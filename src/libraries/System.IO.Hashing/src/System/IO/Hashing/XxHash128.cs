// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Based on the XXH128 implementation from https://github.com/Cyan4973/xxHash.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.IO.Hashing.XxHashShared;

namespace System.IO.Hashing
{
    /// <summary>Provides an implementation of the XXH128 hash algorithm for generating a 128-bit hash.</summary>
    /// <remarks>
    /// For methods that persist the computed numerical hash value as bytes,
    /// the value is written in the Big Endian byte order.
    /// </remarks>
#if NET
    [SkipLocalsInit]
#endif
    public sealed unsafe class XxHash128 : NonCryptographicHashAlgorithm
    {
        /// <summary>XXH128 produces 16-byte hashes.</summary>
        private new const int HashLengthInBytes = 16;

        private State _state;

        /// <summary>Initializes a new instance of the <see cref="XxHash128"/> class using the default seed value 0.</summary>
        public XxHash128() : this(0)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="XxHash128"/> class using the specified seed.</summary>
        public XxHash128(long seed) : base(HashLengthInBytes)
        {
            Initialize(ref _state, (ulong)seed);
        }

        /// <summary>Initializes a new instance of the <see cref="XxHash128"/> class using the state from another instance.</summary>
        private XxHash128(State state) : base(HashLengthInBytes)
        {
            _state = state;
        }

        /// <summary>Returns a clone of the current instance, with a copy of the current instance's internal state.</summary>
        /// <returns>A new instance that will produce the same sequence of values as the current instance.</returns>
        public XxHash128 Clone() => new(_state);

        /// <summary>Computes the XXH128 hash of the provided <paramref name="source"/> data.</summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>The XXH128 128-bit hash code of the provided data.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static byte[] Hash(byte[] source) => Hash(source, seed: 0);

        /// <summary>Computes the XXH128 hash of the provided data using the provided seed.</summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="seed">The seed value for this hash computation.</param>
        /// <returns>The XXH128 128-bit hash code of the provided data.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static byte[] Hash(byte[] source, long seed)
        {
            ArgumentNullException.ThrowIfNull(source);

            return Hash(new ReadOnlySpan<byte>(source), seed);
        }

        /// <summary>Computes the XXH128 hash of the provided <paramref name="source"/> data using the optionally provided <paramref name="seed"/>.</summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="seed">The seed value for this hash computation. The default is zero.</param>
        /// <returns>The XXH128 128-bit hash code of the provided data.</returns>
        public static byte[] Hash(ReadOnlySpan<byte> source, long seed = 0)
        {
            byte[] result = new byte[HashLengthInBytes];
            Hash(source, result, seed);
            return result;
        }

        /// <summary>Computes the XXH128 hash of the provided <paramref name="source"/> data into the provided <paramref name="destination"/> using the optionally provided <paramref name="seed"/>.</summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer that receives the computed 128-bit hash code.</param>
        /// <param name="seed">The seed value for this hash computation. The default is zero.</param>
        /// <returns>The number of bytes written to <paramref name="destination"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is shorter than <see cref="HashLengthInBytes"/> (16 bytes).</exception>
        public static int Hash(ReadOnlySpan<byte> source, Span<byte> destination, long seed = 0)
        {
            if (!TryHash(source, destination, out int bytesWritten, seed))
            {
                ThrowDestinationTooShort();
            }

            return bytesWritten;
        }

        /// <summary>Attempts to compute the XXH128 hash of the provided <paramref name="source"/> data into the provided <paramref name="destination"/> using the optionally provided <paramref name="seed"/>.</summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer that receives the computed 128-bit hash code.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written to <paramref name="destination"/>.</param>
        /// <param name="seed">The seed value for this hash computation. The default is zero.</param>
        /// <returns><see langword="true"/> if <paramref name="destination"/> is long enough to receive the computed hash value (16 bytes); otherwise, <see langword="false"/>.</returns>
        public static bool TryHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, long seed = 0)
        {
            if (destination.Length >= sizeof(ulong) * 2)
            {
                Hash128 hash = HashToHash128(source, seed);
                WriteBigEndian128(hash, destination);
                bytesWritten = HashLengthInBytes;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

#if NET
        /// <summary>Computes the XXH128 hash of the provided data.</summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="seed">The seed value for this hash computation. The default is zero.</param>
        /// <returns>The computed XXH128 hash.</returns>
        [CLSCompliant(false)]
        public static UInt128 HashToUInt128(ReadOnlySpan<byte> source, long seed = 0)
        {
            Hash128 hash = HashToHash128(source, seed);
            return new UInt128(hash.High64, hash.Low64);
        }
#endif

        private static Hash128 HashToHash128(ReadOnlySpan<byte> source, long seed = 0)
        {
            uint length = (uint)source.Length;
            fixed (byte* sourcePtr = &MemoryMarshal.GetReference(source))
            {
                if (length <= 16)
                {
                    return HashLength0To16(sourcePtr, length, (ulong)seed);
                }

                if (length <= 128)
                {
                    return HashLength17To128(sourcePtr, length, (ulong)seed);
                }

                if (length <= MidSizeMaxBytes)
                {
                    return HashLength129To240(sourcePtr, length, (ulong)seed);
                }

                return HashLengthOver240(sourcePtr, length, (ulong)seed);
            }
        }

        /// <summary>Resets the hash computation to the initial state.</summary>
        public override void Reset()
        {
            XxHashShared.Reset(ref _state);
        }

        /// <summary>Appends the contents of <paramref name="source"/> to the data already processed for the current hash computation.</summary>
        /// <param name="source">The data to process.</param>
        public override void Append(ReadOnlySpan<byte> source)
        {
            XxHashShared.Append(ref _state, source);
        }

        /// <summary>Writes the computed 128-bit hash value to <paramref name="destination"/> without modifying accumulated state.</summary>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        protected override void GetCurrentHashCore(Span<byte> destination)
        {
            Hash128 current = GetCurrentHashAsHash128();
            WriteBigEndian128(current, destination);
        }

        private Hash128 GetCurrentHashAsHash128()
        {
            Hash128 current;

            if (_state.TotalLength > MidSizeMaxBytes)
            {
                // Digest on a local copy to ensure the accumulators remain unaltered.
                ulong* accumulators = stackalloc ulong[AccumulatorCount];
                CopyAccumulators(ref _state, accumulators);

                fixed (byte* secret = _state.Secret)
                {
                    DigestLong(ref _state, accumulators, secret);
                    current = new Hash128(
                        low64: MergeAccumulators(accumulators, secret + SecretMergeAccsStartBytes, _state.TotalLength * Prime64_1),
                        high64: MergeAccumulators(accumulators, secret + SecretLengthBytes - AccumulatorCount * sizeof(ulong) - SecretMergeAccsStartBytes, ~(_state.TotalLength * Prime64_2)));
                }
            }
            else
            {
                fixed (byte* buffer = _state.Buffer)
                {
                    current = HashToHash128(new ReadOnlySpan<byte>(buffer, (int)_state.TotalLength), (long)_state.Seed);
                }
            }

            return current;
        }

#if NET
        /// <summary>Gets the current computed hash value without modifying accumulated state.</summary>
        /// <returns>The hash value for the data already provided.</returns>
        [CLSCompliant(false)]
        public UInt128 GetCurrentHashAsUInt128()
        {
            Hash128 current = GetCurrentHashAsHash128();
            return new UInt128(current.High64, current.Low64);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteBigEndian128(in Hash128 hash, Span<byte> destination)
        {
            ulong low = hash.Low64;
            ulong high = hash.High64;
            if (BitConverter.IsLittleEndian)
            {
                low = BinaryPrimitives.ReverseEndianness(low);
                high = BinaryPrimitives.ReverseEndianness(high);
            }

            ref byte dest0 = ref MemoryMarshal.GetReference(destination);
            Unsafe.WriteUnaligned(ref dest0, high);
            Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref dest0, new IntPtr(sizeof(ulong))), low);
        }

        private static Hash128 HashLength0To16(byte* source, uint length, ulong seed)
        {
            if (length > 8)
            {
                return HashLength9To16(source, length, seed);
            }

            if (length >= 4)
            {
                return HashLength4To8(source, length, seed);
            }

            if (length != 0)
            {
                return HashLength1To3(source, length, seed);
            }

            const ulong BitFlipL = DefaultSecretUInt64_8 ^ DefaultSecretUInt64_9;
            const ulong BitFlipH = DefaultSecretUInt64_10 ^ DefaultSecretUInt64_11;
            return new Hash128(XxHash64.Avalanche(seed ^ BitFlipL), XxHash64.Avalanche(seed ^ BitFlipH));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Hash128 HashLength1To3(byte* source, uint length, ulong seed)
        {
            Debug.Assert(length >= 1 && length <= 3);

            // When source.Length == 1, c1 == source[0], c2 == source[0], c3 == source[0]
            // When source.Length == 2, c1 == source[0], c2 == source[1], c3 == source[1]
            // When source.Length == 3, c1 == source[0], c2 == source[1], c3 == source[2]
            byte c1 = *source;
            byte c2 = source[length >> 1];
            byte c3 = source[length - 1];

            uint combinedl = ((uint)c1 << 16) | ((uint)c2 << 24) | c3 | (length << 8);

            uint combinedh = BitOperations.RotateLeft(BinaryPrimitives.ReverseEndianness(combinedl), 13);
            const uint SecretXorL = (unchecked((uint)DefaultSecretUInt64_0) ^ (uint)(DefaultSecretUInt64_0 >> 32));
            const uint SecretXorH = (unchecked((uint)DefaultSecretUInt64_1) ^ (uint)(DefaultSecretUInt64_1 >> 32));
            ulong bitflipl = SecretXorL + seed;
            ulong bitfliph = SecretXorH - seed;
            ulong keyedLo = combinedl ^ bitflipl;
            ulong keyedHi = combinedh ^ bitfliph;

            return new Hash128(XxHash64.Avalanche(keyedLo), XxHash64.Avalanche(keyedHi));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Hash128 HashLength4To8(byte* source, uint length, ulong seed)
        {
            Debug.Assert(length >= 4 && length <= 8);

            seed ^= (ulong)BinaryPrimitives.ReverseEndianness((uint)seed) << 32;

            uint inputLo = ReadUInt32LE(source);
            uint inputHi = ReadUInt32LE(source + length - 4);
            ulong input64 = inputLo + ((ulong)inputHi << 32);
            ulong bitflip = (DefaultSecretUInt64_2 ^ DefaultSecretUInt64_3) + seed;
            ulong keyed = input64 ^ bitflip;

            ulong m128High = Multiply64To128(keyed, Prime64_1 + (length << 2), out ulong m128Low);

            m128High += (m128Low << 1);
            m128Low ^= (m128High >> 3);

            m128Low = XorShift(m128Low, 35);
            m128Low *= 0x9FB21C651E98DF25UL;
            m128Low = XorShift(m128Low, 28);
            m128High = Avalanche(m128High);

            return new Hash128(m128Low, m128High);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Hash128 HashLength9To16(byte* source, uint length, ulong seed)
        {
            Debug.Assert(length >= 9 && length <= 16);
            ulong bitflipl = (DefaultSecretUInt64_4 ^ DefaultSecretUInt64_5) - seed;
            ulong bitfliph = (DefaultSecretUInt64_6 ^ DefaultSecretUInt64_7) + seed;
            ulong inputLo = ReadUInt64LE(source);
            ulong inputHi = ReadUInt64LE(source + length - 8);
            ulong m128High = Multiply64To128(inputLo ^ inputHi ^ bitflipl, Prime64_1, out ulong m128Low);

            m128Low += (ulong)(length - 1) << 54;
            inputHi ^= bitfliph;

            m128High += sizeof(void*) < sizeof(ulong) ?
                (inputHi & 0xFFFFFFFF00000000UL) + Multiply32To64((uint)inputHi, Prime32_2) :
                inputHi + Multiply32To64((uint)inputHi, Prime32_2 - 1);

            m128Low ^= BinaryPrimitives.ReverseEndianness(m128High);

            ulong h128High = Multiply64To128(m128Low, Prime64_2, out ulong h128Low);
            h128High += m128High * (ulong)Prime64_2;

            h128Low = Avalanche(h128Low);
            h128High = Avalanche(h128High);
            return new Hash128(h128Low, h128High);
        }

        private static Hash128 HashLength17To128(byte* source, uint length, ulong seed)
        {
            Debug.Assert(length >= 17 && length <= 128);

            ulong accLow = length * Prime64_1;
            ulong accHigh = 0;

            switch ((length - 1) / 32)
            {
                default: // case 3
                    Mix32Bytes(ref accLow, ref accHigh, source + 48, source + length - 64, DefaultSecretUInt64_12, DefaultSecretUInt64_13, DefaultSecretUInt64_14, DefaultSecretUInt64_15, seed);
                    goto case 2;
                case 2:
                    Mix32Bytes(ref accLow, ref accHigh, source + 32, source + length - 48, DefaultSecretUInt64_8, DefaultSecretUInt64_9, DefaultSecretUInt64_10, DefaultSecretUInt64_11, seed);
                    goto case 1;
                case 1:
                    Mix32Bytes(ref accLow, ref accHigh, source + 16, source + length - 32, DefaultSecretUInt64_4, DefaultSecretUInt64_5, DefaultSecretUInt64_6, DefaultSecretUInt64_7, seed);
                    goto case 0;
                case 0:
                    Mix32Bytes(ref accLow, ref accHigh, source, source + length - 16, DefaultSecretUInt64_0, DefaultSecretUInt64_1, DefaultSecretUInt64_2, DefaultSecretUInt64_3, seed);
                    break;
            }

            return AvalancheHash(accLow, accHigh, length, seed);
        }

        private static Hash128 HashLength129To240(byte* source, uint length, ulong seed)
        {
            Debug.Assert(length >= 129 && length <= 240);

            ulong accLow = length * Prime64_1;
            ulong accHigh = 0;

            Mix32Bytes(ref accLow, ref accHigh, source + (32 * 0), source + (32 * 0) + 16, DefaultSecretUInt64_0, DefaultSecretUInt64_1, DefaultSecretUInt64_2, DefaultSecretUInt64_3, seed);
            Mix32Bytes(ref accLow, ref accHigh, source + (32 * 1), source + (32 * 1) + 16, DefaultSecretUInt64_4, DefaultSecretUInt64_5, DefaultSecretUInt64_6, DefaultSecretUInt64_7, seed);
            Mix32Bytes(ref accLow, ref accHigh, source + (32 * 2), source + (32 * 2) + 16, DefaultSecretUInt64_8, DefaultSecretUInt64_9, DefaultSecretUInt64_10, DefaultSecretUInt64_11, seed);
            Mix32Bytes(ref accLow, ref accHigh, source + (32 * 3), source + (32 * 3) + 16, DefaultSecretUInt64_12, DefaultSecretUInt64_13, DefaultSecretUInt64_14, DefaultSecretUInt64_15, seed);

            accLow = Avalanche(accLow);
            accHigh = Avalanche(accHigh);

            uint bound = ((length - (32 * 4)) / 32);
            if (bound != 0)
            {
                Mix32Bytes(ref accLow, ref accHigh, source + (32 * 4), source + (32 * 4) + 16, DefaultSecret3UInt64_0, DefaultSecret3UInt64_1, DefaultSecret3UInt64_2, DefaultSecret3UInt64_3, seed);
                if (bound >= 2)
                {
                    Mix32Bytes(ref accLow, ref accHigh, source + (32 * 5), source + (32 * 5) + 16, DefaultSecret3UInt64_4, DefaultSecret3UInt64_5, DefaultSecret3UInt64_6, DefaultSecret3UInt64_7, seed);
                    if (bound == 3)
                    {
                        Mix32Bytes(ref accLow, ref accHigh, source + (32 * 6), source + (32 * 6) + 16, DefaultSecret3UInt64_8, DefaultSecret3UInt64_9, DefaultSecret3UInt64_10, DefaultSecret3UInt64_11, seed);
                    }
                }
            }
            Mix32Bytes(ref accLow, ref accHigh, source + length - 16, source + length - 32, 0x4F0BC7C7BBDCF93F, 0x59B4CD4BE0518A1D, 0x7378D9C97E9FC831, 0xEBD33483ACC5EA64, 0 - seed);

            return AvalancheHash(accLow, accHigh, length, seed);
        }

        private static Hash128 HashLengthOver240(byte* source, uint length, ulong seed)
        {
            Debug.Assert(length > 240);

            fixed (byte* defaultSecret = &MemoryMarshal.GetReference(DefaultSecret))
            {
                byte* secret = defaultSecret;
                if (seed != 0)
                {
                    byte* customSecret = stackalloc byte[SecretLengthBytes];
                    DeriveSecretFromSeed(customSecret, seed);
                    secret = customSecret;
                }

                ulong* accumulators = stackalloc ulong[AccumulatorCount];
                InitializeAccumulators(accumulators);

                HashInternalLoop(accumulators, source, length, secret);

                return new Hash128(
                    low64: MergeAccumulators(accumulators, secret + SecretMergeAccsStartBytes, length * Prime64_1),
                    high64: MergeAccumulators(accumulators, secret + SecretLengthBytes - AccumulatorCount * sizeof(ulong) - SecretMergeAccsStartBytes, ~(length * Prime64_2)));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Hash128 AvalancheHash(ulong accLow, ulong accHigh, uint length, ulong seed)
        {
            ulong h128Low = accLow + accHigh;
            ulong h128High = (accLow * Prime64_1)
                          + (accHigh * Prime64_4)
                          + ((length - seed) * Prime64_2);
            h128Low = Avalanche(h128Low);
            h128High = 0ul - Avalanche(h128High);
            return new Hash128(h128Low, h128High);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Mix32Bytes(ref ulong accLow, ref ulong accHigh, byte* input1, byte* input2, ulong secret1, ulong secret2, ulong secret3, ulong secret4, ulong seed)
        {
            accLow += Mix16Bytes(input1, secret1, secret2, seed);
            accLow ^= ReadUInt64LE(input2) + ReadUInt64LE(input2 + 8);
            accHigh += Mix16Bytes(input2, secret3, secret4, seed);
            accHigh ^= ReadUInt64LE(input1) + ReadUInt64LE(input1 + 8);
        }

        [DebuggerDisplay("Low64 = {" + nameof(Low64) + "}, High64 = {" + nameof(High64) + "}")]
        private readonly struct Hash128
        {
            public readonly ulong Low64;
            public readonly ulong High64;

            public Hash128(ulong low64, ulong high64)
            {
                Low64 = low64;
                High64 = high64;
            }
        }
    }
}
