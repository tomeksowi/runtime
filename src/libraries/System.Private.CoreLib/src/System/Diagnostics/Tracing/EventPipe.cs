// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Diagnostics.Tracing
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct EventPipeEventInstanceData
    {
        internal IntPtr ProviderID;
        internal uint EventID;
        internal uint ThreadID;
        internal long TimeStamp;
        internal Guid ActivityId;
        internal Guid ChildActivityId;
        internal IntPtr Payload;
        internal uint PayloadLength;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EventPipeSessionInfo
    {
        internal long StartTimeAsUTCFileTime;
        internal long StartTimeStamp;
        internal long TimeStampFrequency;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct EventPipeProviderConfiguration
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        private readonly string m_providerName;
        private readonly ulong m_keywords;
        private readonly uint m_loggingLevel;

        [MarshalAs(UnmanagedType.LPWStr)]
        private readonly string? m_filterData;

        internal EventPipeProviderConfiguration(
            string providerName,
            ulong keywords,
            uint loggingLevel,
            string? filterData)
        {
            ArgumentException.ThrowIfNullOrEmpty(providerName);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(loggingLevel, 5u); // 5 == Verbose, the highest value in EventPipeLoggingLevel.
            m_providerName = providerName;
            m_keywords = keywords;
            m_loggingLevel = loggingLevel;
            m_filterData = filterData;
        }

        internal string ProviderName
        {
            get { return m_providerName; }
        }

        internal ulong Keywords
        {
            get { return m_keywords; }
        }

        internal uint LoggingLevel
        {
            get { return m_loggingLevel; }
        }

        internal string? FilterData => m_filterData;
    }

    internal enum EventPipeSerializationFormat
    {
        NetPerf,
        NetTrace
    }

    internal sealed class EventPipeWaitHandle : WaitHandle
    {
    }

    internal static partial class EventPipeInternal
    {
#if FEATURE_PERFTRACING
        private unsafe struct EventPipeProviderConfigurationNative
        {
            private char* m_pProviderName;
            private ulong m_keywords;
            private uint m_loggingLevel;
            private char* m_pFilterData;

            internal static void MarshalToNative(EventPipeProviderConfiguration managed, ref EventPipeProviderConfigurationNative native)
            {
                native.m_pProviderName = (char*)Marshal.StringToCoTaskMemUni(managed.ProviderName);
                native.m_keywords = managed.Keywords;
                native.m_loggingLevel = managed.LoggingLevel;
                native.m_pFilterData = (char*)Marshal.StringToCoTaskMemUni(managed.FilterData);
            }

            internal void Release()
            {
                if (m_pProviderName != null)
                {
                    Marshal.FreeCoTaskMem((IntPtr)m_pProviderName);
                }
                if (m_pFilterData != null)
                {
                    Marshal.FreeCoTaskMem((IntPtr)m_pFilterData);
                }
            }
        }

        internal static unsafe ulong Enable(
            string? outputFile,
            EventPipeSerializationFormat format,
            uint circularBufferSizeInMB,
            EventPipeProviderConfiguration[] providers)
        {
            Span<EventPipeProviderConfigurationNative> providersNative = new Span<EventPipeProviderConfigurationNative>((void*)Marshal.AllocCoTaskMem(sizeof(EventPipeProviderConfigurationNative) * providers.Length), providers.Length);
            providersNative.Clear();

            try
            {
                for (int i = 0; i < providers.Length; i++)
                {
                    EventPipeProviderConfigurationNative.MarshalToNative(providers[i], ref providersNative[i]);
                }

                fixed (char* outputFilePath = outputFile)
                fixed (EventPipeProviderConfigurationNative* providersNativePointer = providersNative)
                {
                    return Enable(outputFilePath, format, circularBufferSizeInMB, providersNativePointer, (uint)providersNative.Length);
                }
            }
            finally
            {
                for (int i = 0; i < providers.Length; i++)
                {
                    providersNative[i].Release();
                }

                fixed (EventPipeProviderConfigurationNative* providersNativePointer = providersNative)
                {
                    Marshal.FreeCoTaskMem((IntPtr)providersNativePointer);
                }
            }
        }
#else
#pragma warning disable IDE0060
        private unsafe struct EventPipeProviderConfigurationNative
        {
        }

        internal static unsafe ulong Enable(
            string? outputFile,
            EventPipeSerializationFormat format,
            uint circularBufferSizeInMB,
            EventPipeProviderConfiguration[] providers)
        {
            return 0;
        }
#pragma warning restore IDE0060
#endif //FEATURE_PERFTRACING
    }
}
