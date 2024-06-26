// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    [ConditionalClass(typeof(TestConfiguration), nameof(TestConfiguration.IsBinaryFormatterEnabled))]
    public static class SerializationGuardTests
    {
        [Fact]
        public static void BlockAssemblyLoads()
        {
            TryPayload(new AssemblyLoader());
        }

        [Fact]
        public static void BlockProcessStarts()
        {
            TryPayload(new ProcessStarter());
        }

        [Fact]
        public static void BlockFileWrites()
        {
            TryPayload(new FileWriter());
        }

        [Fact]
        public static void BlockAsyncDodging()
        {
            TryPayload(new AsyncDodger());
        }

        private static void TryPayload(object payload)
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter writer = new BinaryFormatter();
            writer.Serialize(ms, payload);
            ms.Position = 0;

            BinaryFormatter reader = new BinaryFormatter();
            SerializationException se = Assert.Throws<SerializationException>(() => reader.Deserialize(ms));
            Assert.IsAssignableFrom<TargetInvocationException>(se.InnerException);
        }
    }

    [Serializable]
    internal class AssemblyLoader : ISerializable
    {
        public AssemblyLoader() { }

        public AssemblyLoader(SerializationInfo info, StreamingContext context)
        {
            Assembly.Load(new byte[1000]);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }
    }

    [Serializable]
    internal class ProcessStarter : ISerializable
    {
        public ProcessStarter() { }

        private ProcessStarter(SerializationInfo info, StreamingContext context)
        {
            Process.Start("calc.exe");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }
    }

    [Serializable]
    internal class FileWriter : ISerializable
    {
        public FileWriter() { }

        private FileWriter(SerializationInfo info, StreamingContext context)
        {
            string tempPath = Path.GetTempFileName();
            File.WriteAllText(tempPath, "This better not be written...");
            throw new UnreachableException("Unreachable code (SerializationGuard should have kicked in)");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }
    }

    [Serializable]
    internal class AsyncDodger : ISerializable
    {
        public AsyncDodger() { }

        private AsyncDodger(SerializationInfo info, StreamingContext context)
        {
            try
            {
                Task t = Task.Factory.StartNew(LoadAssemblyOnBackgroundThread, TaskCreationOptions.LongRunning);
                t.Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }

        private void LoadAssemblyOnBackgroundThread()
        {
            Assembly.Load(new byte[1000]);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }
    }
}
