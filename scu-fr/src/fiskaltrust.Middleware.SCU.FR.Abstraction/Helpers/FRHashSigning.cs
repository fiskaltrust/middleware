using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using fiskaltrust.ifPOS.v2.fr;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// The archive-signing half of an SCU (<see cref="IFRSSCD.SignHashAsync"/>). Not part of either
/// referential, so the certified SCUs share it. Like receipt signing, it reports a failure in its
/// result and never throws.
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
            return FRSigningCertificate.Failed($"The {scuName} SCU has no CertificateBase64 in its configuration, so it cannot sign an archive.");
        }

        byte[] rawData;
        try
        {
            rawData = Convert.FromBase64String(certificateBase64);
        }
        catch (FormatException)
        {
            return FRSigningCertificate.Failed($"The configured CertificateBase64 of the {scuName} SCU is not valid base64.");
        }

        X509Certificate2 certificate;
        try
        {
            certificate = new X509Certificate2(rawData);
        }
        catch (CryptographicException ex)
        {
            return FRSigningCertificate.Failed($"The configured CertificateBase64 of the {scuName} SCU is not an X.509 certificate. {ex.Message}");
        }

        using (certificate)
        using (var publicKey = certificate.GetECDsaPublicKey())
        {
            if (publicKey is null || !publicKey.ExportSubjectPublicKeyInfo().SequenceEqual(key.ExportSubjectPublicKeyInfo()))
            {
                return FRSigningCertificate.Failed($"The configured certificate of the {scuName} SCU does not hold the public key of its PrivateKey.");
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
    public static FRHashSignature SignHash(ECDsa key, byte[] sha256, string scuName)
    {
        if (sha256 is null || sha256.Length != 32)
        {
            return FRHashSignature.Failed($"The {scuName} SCU signs SHA-256 hashes, which are 32 bytes.");
        }

        try
        {
            return new FRHashSignature { Signature = key.SignHash(sha256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation) };
        }
        catch (Exception ex)
        {
            return FRHashSignature.Failed($"The {scuName} SCU could not sign the hash. {ex.Message}");
        }
    }
}
