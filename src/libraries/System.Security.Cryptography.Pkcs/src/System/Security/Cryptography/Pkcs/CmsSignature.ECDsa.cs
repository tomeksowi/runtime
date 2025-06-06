// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    internal partial class CmsSignature
    {
        static partial void PrepareRegistrationECDsa(Dictionary<string, CmsSignature> lookup)
        {
            lookup.Add(Oids.ECDsaWithSha1, new ECDsaCmsSignature(Oids.ECDsaWithSha1, HashAlgorithmName.SHA1));
            lookup.Add(Oids.ECDsaWithSha256, new ECDsaCmsSignature(Oids.ECDsaWithSha256, HashAlgorithmName.SHA256));
            lookup.Add(Oids.ECDsaWithSha384, new ECDsaCmsSignature(Oids.ECDsaWithSha384, HashAlgorithmName.SHA384));
            lookup.Add(Oids.ECDsaWithSha512, new ECDsaCmsSignature(Oids.ECDsaWithSha512, HashAlgorithmName.SHA512));
#if NET8_0_OR_GREATER
            lookup.Add(Oids.ECDsaWithSha3_256, new ECDsaCmsSignature(Oids.ECDsaWithSha3_256, HashAlgorithmName.SHA3_256));
            lookup.Add(Oids.ECDsaWithSha3_384, new ECDsaCmsSignature(Oids.ECDsaWithSha3_384, HashAlgorithmName.SHA3_384));
            lookup.Add(Oids.ECDsaWithSha3_512, new ECDsaCmsSignature(Oids.ECDsaWithSha3_512, HashAlgorithmName.SHA3_512));
#endif
            lookup.Add(Oids.EcPublicKey, new ECDsaCmsSignature(null, null));
        }

        private sealed partial class ECDsaCmsSignature : CmsSignature
        {
            private readonly HashAlgorithmName? _expectedDigest;
            private readonly string? _signatureAlgorithm;

            internal override RSASignaturePadding? SignaturePadding => null;

            internal ECDsaCmsSignature(string? signatureAlgorithm, HashAlgorithmName? expectedDigest)
            {
                _signatureAlgorithm = signatureAlgorithm;
                _expectedDigest = expectedDigest;
            }

            protected override bool VerifyKeyType(object key) => key is ECDsa;
            internal override bool NeedsHashedMessage => true;

            internal override bool VerifySignature(
#if NET || NETSTANDARD2_1
                ReadOnlySpan<byte> valueHash,
                ReadOnlyMemory<byte> signature,
#else
                byte[] valueHash,
                byte[] signature,
#endif
                string? digestAlgorithmOid,
                ReadOnlyMemory<byte>? signatureParameters,
                X509Certificate2 certificate)
            {
                HashAlgorithmName digestAlgorithmName = PkcsHelpers.GetDigestAlgorithm(digestAlgorithmOid, forVerification: true);

                if (_expectedDigest != null && _expectedDigest != digestAlgorithmName)
                {
                    throw new CryptographicException(
                        SR.Format(
                            SR.Cryptography_Cms_InvalidSignerHashForSignatureAlg,
                            digestAlgorithmOid,
                            _signatureAlgorithm));
                }

                ECDsa? key = certificate.GetECDsaPublicKey();

                if (key == null)
                {
                    return false;
                }

                using (key)
                {
                    int bufSize;
                    checked
                    {
                        // fieldSize = ceil(KeySizeBits / 8);
                        int fieldSize = (key.KeySize + 7) / 8;
                        bufSize = 2 * fieldSize;
                    }

#if NET || NETSTANDARD2_1
                    byte[] rented = CryptoPool.Rent(bufSize);
                    Span<byte> ieee = new Span<byte>(rented, 0, bufSize);

                    try
                    {
#else
                    byte[] ieee = new byte[bufSize];
#endif
                        if (!DsaDerToIeee(signature, ieee))
                        {
                            return false;
                        }

                        return key.VerifyHash(valueHash, ieee);
#if NET || NETSTANDARD2_1
                    }
                    finally
                    {
                        CryptoPool.Return(rented, bufSize);
                    }
#endif
                }
            }

            protected override bool Sign(
#if NET || NETSTANDARD2_1
                ReadOnlySpan<byte> dataHash,
#else
                byte[] dataHash,
#endif
                string? hashAlgorithmOid,
                X509Certificate2 certificate,
                object? privateKey,
                bool silent,
                [NotNullWhen(true)] out string? signatureAlgorithm,
                [NotNullWhen(true)] out byte[]? signatureValue,
                out byte[]? signatureParameters)
            {
                signatureParameters = null;
                using (GetSigningKey(privateKey, certificate, silent, ECDsaCertificateExtensions.GetECDsaPublicKey, out ECDsa? key))
                {
                    if (key == null)
                    {
                        signatureAlgorithm = null;
                        signatureValue = null;
                        return false;
                    }

                    string? oidValue =
                        hashAlgorithmOid switch
                        {
                            Oids.Sha1 => Oids.ECDsaWithSha1,
                            Oids.Sha256 => Oids.ECDsaWithSha256,
                            Oids.Sha384 => Oids.ECDsaWithSha384,
                            Oids.Sha512 => Oids.ECDsaWithSha512,
#if NET8_0_OR_GREATER
                            Oids.Sha3_256 => Oids.ECDsaWithSha3_256,
                            Oids.Sha3_384 => Oids.ECDsaWithSha3_384,
                            Oids.Sha3_512 => Oids.ECDsaWithSha3_512,
#endif
                            _ => null,
                        };

                    if (oidValue == null)
                    {
                        signatureAlgorithm = null;
                        signatureValue = null;
                        return false;
                    }

                    signatureAlgorithm = oidValue;

#if NET || NETSTANDARD2_1
                    int bufSize;
                    checked
                    {
                        // fieldSize = ceil(KeySizeBits / 8);
                        int fieldSize = (key.KeySize + 7) / 8;
                        bufSize = 2 * fieldSize;
                    }

                    byte[] rented = CryptoPool.Rent(bufSize);
                    int bytesWritten = 0;

                    try
                    {
                        if (key.TrySignHash(dataHash, rented, out bytesWritten))
                        {
                            var signedHash = new ReadOnlySpan<byte>(rented, 0, bytesWritten);

                            if (key != null)
                            {
                                using (ECDsa certKey = certificate.GetECDsaPublicKey()!)
                                {
                                    if (!certKey.VerifyHash(dataHash, signedHash))
                                    {
                                        // key did not match certificate
                                        signatureValue = null;
                                        return false;
                                    }
                                }
                            }

                            signatureValue = DsaIeeeToDer(signedHash);
                            return true;
                        }
                    }
                    finally
                    {
                        CryptoPool.Return(rented, bytesWritten);
                    }
#endif

                    signatureValue = DsaIeeeToDer(key.SignHash(
#if NET || NETSTANDARD2_1
                        dataHash.ToArray()
#else
                        dataHash
#endif
                        ));
                    return true;
                }
            }
        }
    }
}
