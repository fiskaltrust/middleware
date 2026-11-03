using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Journal.FR.Archive.Signing
{
    /// <summary>Signature algorithm identifiers written in the manifest (JOSE names).</summary>
    public static class ArchiveSignatureAlgorithms
    {
        /// <summary>RSASSA-PKCS1-v1_5 with SHA-256.</summary>
        public const string RS256 = "RS256";

        /// <summary>ECDSA P-256 with SHA-256, signature in IEEE P1363 (r||s) form.</summary>
        public const string ES256 = "ES256";
    }

    /// <summary>
    /// Signs the archive files and manifests with a fiskaltrust certificate chaining to the fiskaltrust
    /// root CA (ADR 0007, market-fr#133). Hash-based, so the key can stay in an HSM or Key Vault: the
    /// package computes the SHA-256 and only asks for a signature over it.
    /// </summary>
    public interface IArchiveSigner
    {
        /// <summary>The signing certificate (public part), embedded in every archive.</summary>
        X509Certificate2 Certificate { get; }

        /// <summary>One of <see cref="ArchiveSignatureAlgorithms"/>.</summary>
        string SignatureAlgorithm { get; }

        Task<byte[]> SignHashAsync(byte[] sha256, CancellationToken cancellationToken);
    }

    /// <summary>An <see cref="IArchiveSigner"/> over a certificate holding its private key (RSA or ECDSA).</summary>
    public sealed class X509ArchiveSigner : IArchiveSigner
    {
        private readonly RSA? _rsa;
        private readonly ECDsa? _ecdsa;

        public X509ArchiveSigner(X509Certificate2 certificateWithPrivateKey)
        {
            if (certificateWithPrivateKey is null) throw new ArgumentNullException(nameof(certificateWithPrivateKey));
            if (!certificateWithPrivateKey.HasPrivateKey)
            {
                throw new ArgumentException("The archive signing certificate must hold its private key.", nameof(certificateWithPrivateKey));
            }
            _rsa = certificateWithPrivateKey.GetRSAPrivateKey();
            _ecdsa = _rsa is null ? certificateWithPrivateKey.GetECDsaPrivateKey() : null;
            if (_rsa is null && _ecdsa is null)
            {
                throw new ArgumentException("The archive signing certificate must have an RSA or ECDSA key.", nameof(certificateWithPrivateKey));
            }
            if (_ecdsa is not null && !ArchiveSignatureVerifier.IsP256(_ecdsa))
            {
                throw new ArgumentException("An ECDSA archive signing certificate must use the NIST P-256 curve (ES256).", nameof(certificateWithPrivateKey));
            }
            SignatureAlgorithm = _rsa is not null ? ArchiveSignatureAlgorithms.RS256 : ArchiveSignatureAlgorithms.ES256;
            Certificate = new X509Certificate2(certificateWithPrivateKey.RawData);
        }

        public X509Certificate2 Certificate { get; }

        public string SignatureAlgorithm { get; }

        public Task<byte[]> SignHashAsync(byte[] sha256, CancellationToken cancellationToken)
        {
            ArchiveSignatureVerifier.CheckHash(sha256);
            var signature = _rsa is not null
                ? _rsa.SignHash(sha256, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
                : _ecdsa!.SignHash(sha256);
            return Task.FromResult(signature);
        }
    }

    public static class ArchiveSignatureVerifier
    {
        private const string P256Oid = "1.2.840.10045.3.1.7";

        /// <summary>The ES256 signature length: r and s of P-256, 32 bytes each (IEEE P1363).</summary>
        private const int ES256SignatureLength = 64;

        public static bool Verify(X509Certificate2 certificate, string signatureAlgorithm, byte[] sha256, byte[] signature)
        {
            if (certificate is null) throw new ArgumentNullException(nameof(certificate));
            CheckHash(sha256);
            if (signature is null) return false;
            switch (signatureAlgorithm)
            {
                case ArchiveSignatureAlgorithms.RS256:
                    using (var rsa = certificate.GetRSAPublicKey())
                    {
                        return rsa is not null && rsa.VerifyHash(sha256, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    }
                case ArchiveSignatureAlgorithms.ES256:
                    using (var ecdsa = certificate.GetECDsaPublicKey())
                    {
                        return ecdsa is not null && signature.Length == ES256SignatureLength && IsP256(ecdsa) && ecdsa.VerifyHash(sha256, signature);
                    }
                default:
                    return false;
            }
        }

        /// <summary>ES256 is ECDSA on NIST P-256 only; a P-384 or P-521 key would be mislabelled.</summary>
        internal static bool IsP256(ECDsa key)
        {
            var curve = key.ExportParameters(false).Curve;
            if (curve.Oid?.Value == P256Oid)
            {
                return true;
            }
            // Windows CNG may name the curve without its OID.
            var name = curve.Oid?.FriendlyName;
            return name == "nistP256" || name == "ECDSA_P256" || name == "secp256r1";
        }

        internal static void CheckHash(byte[] sha256)
        {
            if (sha256 is null || sha256.Length != 32)
            {
                throw new ArgumentException("A SHA-256 hash is 32 bytes.", nameof(sha256));
            }
        }
    }
}
