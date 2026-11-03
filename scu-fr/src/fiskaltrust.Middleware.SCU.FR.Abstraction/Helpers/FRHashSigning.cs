using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// The archive-signing half of an SCU (<see cref="IFRSSCD.SignHashAsync"/>). Not part of either
/// referential, so the certified SCUs share it.
/// </summary>
public static class FRHashSigning
{
    /// <summary>
    /// The configured certificate, checked against the SCU's key: an archive signed with a key the
    /// embedded certificate does not hold would never verify.
    /// </summary>
    public static FRSigningCertificate SigningCertificate(ECDsa key, string? certificateBase64, string certificateSerialNumber, string scuName)
    {
        if (string.IsNullOrWhiteSpace(certificateBase64))
        {
            throw new FRCertificateException($"The {scuName} SCU has no CertificateBase64 in its configuration, so it cannot sign an archive.");
        }

        byte[] rawData;
        try
        {
            rawData = Convert.FromBase64String(certificateBase64);
        }
        catch (FormatException ex)
        {
            throw new FRCertificateException($"The configured CertificateBase64 of the {scuName} SCU is not valid base64.", ex);
        }

        X509Certificate2 certificate;
        try
        {
            certificate = new X509Certificate2(rawData);
        }
        catch (CryptographicException ex)
        {
            throw new FRCertificateException($"The configured CertificateBase64 of the {scuName} SCU is not an X.509 certificate.", ex);
        }

        using (certificate)
        using (var publicKey = certificate.GetECDsaPublicKey())
        {
            if (publicKey is null || !publicKey.ExportSubjectPublicKeyInfo().SequenceEqual(key.ExportSubjectPublicKeyInfo()))
            {
                throw new FRCertificateException($"The configured certificate of the {scuName} SCU does not hold the public key of its PrivateKey.");
            }
        }

        return new FRSigningCertificate
        {
            CertificateBase64 = certificateBase64,
            CertificateSerialNumber = certificateSerialNumber,
            SignatureAlgorithm = FRSigningCertificate.ES256,
        };
    }

    /// <summary>ES256 over a SHA-256 hash, in IEEE P1363 (r||s) form.</summary>
    public static byte[] SignHash(ECDsa key, byte[] sha256)
    {
        if (sha256 is null || sha256.Length != 32)
        {
            throw new FRValidationException("A SHA-256 hash is 32 bytes.");
        }

        return key.SignHash(sha256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }
}
