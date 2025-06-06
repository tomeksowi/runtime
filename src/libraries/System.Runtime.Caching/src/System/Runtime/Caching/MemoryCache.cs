// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Caching.Configuration;
using System.Runtime.Caching.Resources;
using System.Runtime.Versioning;
using System.Security;
using System.Threading;

namespace System.Runtime.Caching
{
    public class MemoryCache : ObjectCache, IEnumerable, IDisposable
    {
        private const DefaultCacheCapabilities CAPABILITIES = DefaultCacheCapabilities.InMemoryProvider
                                                              | DefaultCacheCapabilities.CacheEntryChangeMonitors
                                                              | DefaultCacheCapabilities.AbsoluteExpirations
                                                              | DefaultCacheCapabilities.SlidingExpirations
                                                              | DefaultCacheCapabilities.CacheEntryUpdateCallback
                                                              | DefaultCacheCapabilities.CacheEntryRemovedCallback;
        private static readonly TimeSpan s_oneYear = new TimeSpan(365, 0, 0, 0);
        private static readonly object s_initLock = new object();
        private static MemoryCache s_defaultCache;
        private static readonly CacheEntryRemovedCallback s_sentinelRemovedCallback = new CacheEntryRemovedCallback(SentinelEntry.OnCacheEntryRemovedCallback);
        private GCHandleRef<MemoryCacheStore>[] _storeRefs;
        private int _storeCount;
        private int _disposed;
        private MemoryCacheStatistics _stats;
        private readonly string _name;
        private Counters _perfCounters;
        private readonly bool _configLess;
        private bool _useMemoryCacheManager = true;
        private bool _throwOnDisposed;
#if NET
        [UnsupportedOSPlatformGuard("wasi")]
        [UnsupportedOSPlatformGuard("browser")]
        private static bool _countersSupported => !OperatingSystem.IsBrowser() && !OperatingSystem.IsWasi();
#else
        private static bool _countersSupported => true;
#endif

        private bool IsDisposed { get { return (_disposed == 1); } }
        internal bool ConfigLess { get { return _configLess; } }

        private sealed class SentinelEntry
        {
            private readonly string _key;
            private readonly ChangeMonitor _expensiveObjectDependency;
            private readonly CacheEntryUpdateCallback _updateCallback;

            internal SentinelEntry(string key, ChangeMonitor expensiveObjectDependency, CacheEntryUpdateCallback callback)
            {
                _key = key;
                _expensiveObjectDependency = expensiveObjectDependency;
                _updateCallback = callback;
            }

            internal string Key
            {
                get { return _key; }
            }

            internal ChangeMonitor ExpensiveObjectDependency
            {
                get { return _expensiveObjectDependency; }
            }

            internal CacheEntryUpdateCallback CacheEntryUpdateCallback
            {
                get { return _updateCallback; }
            }

            private static bool IsPolicyValid(CacheItemPolicy policy)
            {
                if (policy == null)
                {
                    return false;
                }
                // see if any change monitors have changed
                bool hasChanged = false;
                Collection<ChangeMonitor> changeMonitors = policy.ChangeMonitors;
                if (changeMonitors != null)
                {
                    foreach (ChangeMonitor monitor in changeMonitors)
                    {
                        if (monitor != null && monitor.HasChanged)
                        {
                            hasChanged = true;
                            break;
                        }
                    }
                }
                // if the monitors haven't changed yet and we have an update callback
                // then the policy is valid
                if (!hasChanged && policy.UpdateCallback != null)
                {
                    return true;
                }
                // if the monitors have changed we need to dispose them
                if (hasChanged)
                {
                    foreach (ChangeMonitor monitor in changeMonitors)
                    {
                        monitor?.Dispose();
                    }
                }
                return false;
            }

            internal static void OnCacheEntryRemovedCallback(CacheEntryRemovedArguments arguments)
            {
                MemoryCache cache = arguments.Source as MemoryCache;
                SentinelEntry entry = arguments.CacheItem.Value as SentinelEntry;
                CacheEntryRemovedReason reason = arguments.RemovedReason;
                switch (reason)
                {
                    case CacheEntryRemovedReason.Expired:
                        break;
                    case CacheEntryRemovedReason.ChangeMonitorChanged:
                        if (entry.ExpensiveObjectDependency.HasChanged)
                        {
                            // If the expensiveObject has been removed explicitly by Cache.Remove,
                            // return from the SentinelEntry removed callback
                            // thus effectively removing the SentinelEntry from the cache.
                            return;
                        }
                        break;
                    case CacheEntryRemovedReason.Evicted:
                        Debug.Fail("Reason should never be CacheEntryRemovedReason.Evicted since the entry was inserted as NotRemovable.");
                        return;
                    default:
                        // do nothing if reason is Removed or CacheSpecificEviction
                        return;
                }

                // invoke update callback
                try
                {
                    CacheEntryUpdateArguments args = new CacheEntryUpdateArguments(cache, reason, entry.Key, null);
                    entry.CacheEntryUpdateCallback(args);
                    object expensiveObject = args.UpdatedCacheItem?.Value;
                    CacheItemPolicy policy = args.UpdatedCacheItemPolicy;
                    // Only update the "expensive" object if the user returns a new object,
                    // a policy with update callback, and the change monitors haven't changed.  (Inserting
                    // with change monitors that have already changed will cause recursion.)
                    if (expensiveObject != null && IsPolicyValid(policy))
                    {
                        cache.Set(entry.Key, expensiveObject, policy);
                    }
                    else
                    {
                        cache.Remove(entry.Key);
                    }
                }
                catch
                {
                    cache.Remove(entry.Key);
                    // Review: What should we do with this exception?
                }
            }
        }

        // private and internal

        internal MemoryCacheStore GetStore(MemoryCacheKey cacheKey)
        {
            int hashCode = cacheKey.Hash;
            if (hashCode < 0)
            {
                hashCode = (hashCode == int.MinValue) ? 0 : -hashCode;
            }
            int idx = hashCode % _storeCount;
            return _storeRefs[idx].Target;
        }

        internal object[] AllSRefTargets
        {
            get
            {
                var allStores = new MemoryCacheStore[_storeCount];
                for (int i = 0; i < _storeCount; i++)
                {
                    allStores[i] = _storeRefs[i].Target;
                }
                return allStores;
            }
        }

        private void InitDisposableMembers(NameValueCollection config)
        {
            bool dispose = true;
            try
            {
                try
                {
                    if (_countersSupported)
                    {
                        _perfCounters = new Counters(_name);
                    }
                }
                catch
                {
                    // ignore exceptions from perf counters
                }
                for (int i = 0; i < _storeCount; i++)
                {
                    _storeRefs[i] = new GCHandleRef<MemoryCacheStore>(new MemoryCacheStore(this, _perfCounters));
                }
                _stats = new MemoryCacheStatistics(this, config);
                dispose = false;
            }
            finally
            {
                if (dispose)
                {
                    Dispose();
                }
            }
        }

        private static void ValidatePolicy(CacheItemPolicy policy)
        {
            if (policy.AbsoluteExpiration != ObjectCache.InfiniteAbsoluteExpiration
                && policy.SlidingExpiration != ObjectCache.NoSlidingExpiration)
            {
                throw new ArgumentException(SR.Invalid_expiration_combination, nameof(policy));
            }
            if (policy.SlidingExpiration < ObjectCache.NoSlidingExpiration || s_oneYear < policy.SlidingExpiration)
            {
                throw new ArgumentOutOfRangeException(nameof(policy), RH.Format(SR.Argument_out_of_range, "SlidingExpiration", ObjectCache.NoSlidingExpiration, s_oneYear));
            }
            if (policy.RemovedCallback != null
                && policy.UpdateCallback != null)
            {
                throw new ArgumentException(SR.Invalid_callback_combination, nameof(policy));
            }
            if (policy.Priority != CacheItemPriority.Default && policy.Priority != CacheItemPriority.NotRemovable)
            {
                throw new ArgumentOutOfRangeException(nameof(policy), RH.Format(SR.Argument_out_of_range, "Priority", CacheItemPriority.Default, CacheItemPriority.NotRemovable));
            }
        }

        // public

        // Amount of memory that can be used before
        // the cache begins to forcibly remove items.
        public long CacheMemoryLimit
        {
            get
            {
                return _stats.CacheMemoryLimit;
            }
        }

        public static MemoryCache Default
        {
            get
            {
                if (s_defaultCache == null)
                {
                    lock (s_initLock)
                    {
                        s_defaultCache ??= new MemoryCache();
                    }
                }
                return s_defaultCache;
            }
        }

        public override DefaultCacheCapabilities DefaultCacheCapabilities
        {
            get
            {
                return CAPABILITIES;
            }
        }

        public override string Name
        {
            get { return _name; }
        }

        internal bool UseMemoryCacheManager
        {
            get { return _useMemoryCacheManager; }
        }

        // Percentage of physical memory that can be used before
        // the cache begins to forcibly remove items.
        public long PhysicalMemoryLimit
        {
            get
            {
                return _stats.PhysicalMemoryLimit;
            }
        }

        // The maximum interval of time after which the cache
        // will update its memory statistics.
        public TimeSpan PollingInterval
        {
            get
            {
                return _stats.PollingInterval;
            }
        }

        // Only used for Default MemoryCache
        private MemoryCache()
        {
            _name = "Default";
            Init(null);
        }

        public MemoryCache(string name, NameValueCollection config = null)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Empty_string_invalid, nameof(name));
            }
            if (string.Equals(name, "default", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(SR.Default_is_reserved, nameof(name));
            }
            _name = name;
            Init(config);
        }

        // ignoreConfigSection is used when redirecting ASP.NET cache into the MemoryCache.  This avoids infinite recursion
        // due to the fact that the (ASP.NET) config system uses the cache, and the cache uses the config system.
        public MemoryCache(string name, NameValueCollection config, bool ignoreConfigSection)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Empty_string_invalid, nameof(name));
            }
            if (string.Equals(name, "default", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(SR.Default_is_reserved, nameof(name));
            }
            _name = name;
            _configLess = ignoreConfigSection;
            Init(config);
        }

        private void Init(NameValueCollection config)
        {
            _storeCount = Environment.ProcessorCount;
            _storeRefs = new GCHandleRef<MemoryCacheStore>[_storeCount];
            if (config != null)
            {
                _useMemoryCacheManager = ConfigUtil.GetBooleanValue(config, ConfigUtil.UseMemoryCacheManager, true);
                _throwOnDisposed = ConfigUtil.GetBooleanValue(config, ConfigUtil.ThrowOnDisposed, false);
            }
            InitDisposableMembers(config);
        }

        private object AddOrGetExistingInternal(string key, object value, CacheItemPolicy policy)
        {
            ArgumentNullException.ThrowIfNull(key);

            DateTimeOffset absExp = ObjectCache.InfiniteAbsoluteExpiration;
            TimeSpan slidingExp = ObjectCache.NoSlidingExpiration;
            CacheItemPriority priority = CacheItemPriority.Default;
            Collection<ChangeMonitor> changeMonitors = null;
            CacheEntryRemovedCallback removedCallback = null;
            if (policy != null)
            {
                ValidatePolicy(policy);
                if (policy.UpdateCallback != null)
                {
                    throw new ArgumentException(SR.Update_callback_must_be_null, nameof(policy));
                }
                absExp = policy.AbsoluteExpiration;
                slidingExp = policy.SlidingExpiration;
                priority = policy.Priority;
                changeMonitors = policy.ChangeMonitors;
                removedCallback = policy.RemovedCallback;
            }
            if (IsDisposed)
            {
                if (changeMonitors != null)
                {
                    foreach (ChangeMonitor monitor in changeMonitors)
                    {
                        monitor?.Dispose();
                    }
                }

                IsDisposedOrThrow();

                return null;
            }
            MemoryCacheKey cacheKey = new MemoryCacheKey(key);
            MemoryCacheStore store = GetStore(cacheKey);
            MemoryCacheEntry entry = store.AddOrGetExisting(cacheKey, new MemoryCacheEntry(key, value, absExp, slidingExp, priority, changeMonitors, removedCallback, this));
            return entry?.Value;
        }

        public override CacheEntryChangeMonitor CreateCacheEntryChangeMonitor(IEnumerable<string> keys, string regionName = null)
        {
            if (regionName != null)
            {
                throw new NotSupportedException(SR.RegionName_not_supported);
            }
            ArgumentNullException.ThrowIfNull(keys);
            List<string> keysClone = new List<string>(keys);
            if (keysClone.Count == 0)
            {
                throw new ArgumentException(RH.Format(SR.Empty_collection, nameof(keys)));
            }

            foreach (string key in keysClone)
            {
                if (key == null)
                {
                    throw new ArgumentException(RH.Format(SR.Collection_contains_null_element, nameof(keys)));
                }
            }

            return new MemoryCacheEntryChangeMonitor(keysClone.AsReadOnly(), regionName, this);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                // stats must be disposed prior to disposing the stores.
                _stats?.Dispose();
                if (_storeRefs != null)
                {
                    foreach (var storeRef in _storeRefs)
                    {
                        storeRef?.Dispose();
                    }
                }
                if (_perfCounters != null)
                {
                    if (_countersSupported)
                    {
                        _perfCounters.Dispose();
                    }
                }
                GC.SuppressFinalize(this);
            }
        }

        private object GetInternal(string key, string regionName)
        {
            if (regionName != null)
            {
                throw new NotSupportedException(SR.RegionName_not_supported);
            }
            ArgumentNullException.ThrowIfNull(key);
            MemoryCacheEntry entry = GetEntry(key);
            return entry?.Value;
        }

        internal MemoryCacheEntry GetEntry(string key)
        {
            if (IsDisposedOrThrow())
            {
                return null;
            }
            MemoryCacheKey cacheKey = new MemoryCacheKey(key);
            MemoryCacheStore store = GetStore(cacheKey);
            return store.Get(cacheKey);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            Hashtable h = new Hashtable();

            if (!IsDisposedOrThrow())
            {
                foreach (var storeRef in _storeRefs)
                {
                    storeRef.Target.CopyTo(h);
                }
            }
            return h.GetEnumerator();
        }

        protected override IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            Dictionary<string, object> h = new Dictionary<string, object>();

            if (!IsDisposedOrThrow())
            {
                foreach (var storeRef in _storeRefs)
                {
                    storeRef.Target.CopyTo(h);
                }
            }
            return h.GetEnumerator();
        }

        internal MemoryCacheEntry RemoveEntry(string key, MemoryCacheEntry entry, CacheEntryRemovedReason reason)
        {
            MemoryCacheKey cacheKey = new MemoryCacheKey(key);
            MemoryCacheStore store = GetStore(cacheKey);
            return store.Remove(cacheKey, entry, reason);
        }

        public long Trim(int percent)
        {
            long trimmed = 0;
            if (percent > 100)
            {
                percent = 100;
            }

            if (!IsDisposedOrThrow())
            {
                foreach (var storeRef in _storeRefs)
                {
                    trimmed += storeRef.Target.TrimInternal(percent);
                }
            }
            return trimmed;
        }

        //Default indexer property
        public override object this[string key]
        {
            get
            {
                return GetInternal(key, null);
            }
            set
            {
                Set(key, value, ObjectCache.InfiniteAbsoluteExpiration);
            }
        }

        //Existence check for a single item
        public override bool Contains(string key, string regionName = null)
        {
            return (GetInternal(key, regionName) != null);
        }

        public override bool Add(CacheItem item, CacheItemPolicy policy)
        {
            CacheItem existingEntry = AddOrGetExisting(item, policy);
            return (existingEntry == null || existingEntry.Value == null);
        }

        public override object AddOrGetExisting(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            if (regionName != null)
            {
                throw new NotSupportedException(SR.RegionName_not_supported);
            }
            CacheItemPolicy policy = new CacheItemPolicy();
            policy.AbsoluteExpiration = absoluteExpiration;
            return AddOrGetExistingInternal(key, value, policy);
        }

        public override CacheItem AddOrGetExisting(CacheItem item, CacheItemPolicy policy)
        {
            ArgumentNullException.ThrowIfNull(item);

            return new CacheItem(item.Key, AddOrGetExistingInternal(item.Key, item.Value, policy));
        }

        public override object AddOrGetExisting(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            if (regionName != null)
            {
                throw new NotSupportedException(SR.RegionName_not_supported);
            }
            return AddOrGetExistingInternal(key, value, policy);
        }

        public override object Get(string key, string regionName = null)
        {
            return GetInternal(key, regionName);
        }

        public override CacheItem GetCacheItem(string key, string regionName = null)
        {
            object value = GetInternal(key, regionName);
            return (value != null) ? new CacheItem(key, value) : null;
        }

        public override void Set(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            if (regionName != null)
            {
                throw new NotSupportedException(SR.RegionName_not_supported);
            }
            CacheItemPolicy policy = new CacheItemPolicy();
            policy.AbsoluteExpiration = absoluteExpiration;
            Set(key, value, policy);
        }

        public override void Set(CacheItem item, CacheItemPolicy policy)
        {
            ArgumentNullException.ThrowIfNull(item);

            Set(item.Key, item.Value, policy);
        }

        public override void Set(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            if (regionName != null)
            {
                throw new NotSupportedException(SR.RegionName_not_supported);
            }
            ArgumentNullException.ThrowIfNull(key);
            DateTimeOffset absExp = ObjectCache.InfiniteAbsoluteExpiration;
            TimeSpan slidingExp = ObjectCache.NoSlidingExpiration;
            CacheItemPriority priority = CacheItemPriority.Default;
            Collection<ChangeMonitor> changeMonitors = null;
            CacheEntryRemovedCallback removedCallback = null;
            if (policy != null)
            {
                ValidatePolicy(policy);
                if (policy.UpdateCallback != null)
                {
                    Set(key, value, policy.ChangeMonitors, policy.AbsoluteExpiration, policy.SlidingExpiration, policy.UpdateCallback);
                    return;
                }
                absExp = policy.AbsoluteExpiration;
                slidingExp = policy.SlidingExpiration;
                priority = policy.Priority;
                changeMonitors = policy.ChangeMonitors;
                removedCallback = policy.RemovedCallback;
            }
            if (IsDisposed)
            {
                if (changeMonitors != null)
                {
                    foreach (ChangeMonitor monitor in changeMonitors)
                    {
                        monitor?.Dispose();
                    }
                }

                IsDisposedOrThrow();

                return;
            }
            MemoryCacheKey cacheKey = new MemoryCacheKey(key);
            MemoryCacheStore store = GetStore(cacheKey);
            store.Set(cacheKey, new MemoryCacheEntry(key, value, absExp, slidingExp, priority, changeMonitors, removedCallback, this));
        }

        internal void Set(string key,
                          object value,
                          Collection<ChangeMonitor> changeMonitors,
                          DateTimeOffset absoluteExpiration,
                          TimeSpan slidingExpiration,
                          CacheEntryUpdateCallback onUpdateCallback)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (changeMonitors == null
                && absoluteExpiration == ObjectCache.InfiniteAbsoluteExpiration
                && slidingExpiration == ObjectCache.NoSlidingExpiration)
            {
                throw new ArgumentException(SR.Invalid_argument_combination);
            }
            ArgumentNullException.ThrowIfNull(onUpdateCallback);
            if (IsDisposed)
            {
                if (changeMonitors != null)
                {
                    foreach (ChangeMonitor monitor in changeMonitors)
                    {
                        monitor?.Dispose();
                    }
                }

                IsDisposedOrThrow();

                return;
            }
            // Insert updatable cache entry
            MemoryCacheKey cacheKey = new MemoryCacheKey(key);
            MemoryCacheStore store = GetStore(cacheKey);
            MemoryCacheEntry cacheEntry = new MemoryCacheEntry(key,
                                                               value,
                                                               ObjectCache.InfiniteAbsoluteExpiration,
                                                               ObjectCache.NoSlidingExpiration,
                                                               CacheItemPriority.NotRemovable,
                                                               null,
                                                               null,
                                                               this);
            store.Set(cacheKey, cacheEntry);

            // Ensure the sentinel depends on its updatable entry
            string[] cacheKeys = { key };
            ChangeMonitor expensiveObjectDep = CreateCacheEntryChangeMonitor(cacheKeys);
            changeMonitors ??= new Collection<ChangeMonitor>();
            changeMonitors.Add(expensiveObjectDep);

            // Insert sentinel entry for the updatable cache entry
            MemoryCacheKey sentinelCacheKey = new MemoryCacheKey("OnUpdateSentinel" + key);
            MemoryCacheStore sentinelStore = GetStore(sentinelCacheKey);
            MemoryCacheEntry sentinelCacheEntry = new MemoryCacheEntry(sentinelCacheKey.Key,
                                                                       new SentinelEntry(key, expensiveObjectDep, onUpdateCallback),
                                                                       absoluteExpiration,
                                                                       slidingExpiration,
                                                                       CacheItemPriority.NotRemovable,
                                                                       changeMonitors,
                                                                       s_sentinelRemovedCallback,
                                                                       this);
            sentinelStore.Set(sentinelCacheKey, sentinelCacheEntry);
            cacheEntry.ConfigureUpdateSentinel(sentinelStore, sentinelCacheEntry);
        }

        public override object Remove(string key, string regionName = null)
        {
            return Remove(key, CacheEntryRemovedReason.Removed, regionName);
        }

        public object Remove(string key, CacheEntryRemovedReason reason, string regionName = null)
        {
            if (regionName != null)
            {
                throw new NotSupportedException(SR.RegionName_not_supported);
            }
            ArgumentNullException.ThrowIfNull(key);
            if (IsDisposedOrThrow())
            {
                return null;
            }
            MemoryCacheEntry entry = RemoveEntry(key, null, reason);
            return entry?.Value;
        }

        public override long GetCount(string regionName = null)
        {
            if (regionName != null)
            {
                throw new NotSupportedException(SR.RegionName_not_supported);
            }

            long count = 0;

            if (!IsDisposedOrThrow())
            {
                foreach (var storeRef in _storeRefs)
                {
                    count += storeRef.Target.Count;
                }
            }
            return count;
        }

        public long GetLastSize(string regionName = null)
        {
            if (regionName != null)
            {
                throw new NotSupportedException(SR.RegionName_not_supported);
            }

            return _stats.GetLastSize();
        }

        public override IDictionary<string, object> GetValues(IEnumerable<string> keys, string regionName = null)
        {
            if (regionName != null)
            {
                throw new NotSupportedException(SR.RegionName_not_supported);
            }
            ArgumentNullException.ThrowIfNull(keys);

            Dictionary<string, object> values = null;

            if (!IsDisposedOrThrow())
            {
                foreach (string key in keys)
                {
                    if (key == null)
                    {
                        throw new ArgumentException(RH.Format(SR.Collection_contains_null_element, nameof(keys)));
                    }
                    object value = GetInternal(key, null);
                    if (value != null)
                    {
                        values ??= new Dictionary<string, object>();
                        values[key] = value;
                    }
                }
            }
            return values;
        }

        // used when redirecting ASP.NET cache into the MemoryCache.  This avoids infinite recursion
        // due to the fact that the (ASP.NET) config system uses the cache, and the cache uses the
        // config system.
        internal void UpdateConfig(NameValueCollection config)
        {
            ArgumentNullException.ThrowIfNull(config);

            if (!IsDisposed)
            {
                _stats.UpdateConfig(config);
            }
        }

        private bool IsDisposedOrThrow()
        {
            if (!IsDisposed)
                return false;

            if (_throwOnDisposed)
            {
                string cacheName = $"{this.GetType().FullName}({_name})";
                throw new ObjectDisposedException(cacheName);
            }

            return true;
        }
    }
}
