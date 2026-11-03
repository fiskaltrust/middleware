namespace fiskaltrust.ifPOS.v2.fr;

/// <summary>
/// The certificate an SCU signs hashes with (<see cref="IFRSSCD.SignHashAsync"/>), so that a
/// verifier can check a locally exported archive (ADR 0007 amendment, market-fr#218).
/// </summary>
/// <remarks>Temporary local declaration, see <see cref="IFRSSCD"/>.</remarks>
public class FRSigningCertificate
{
    /// <summary>JOSE name of the signature <see cref="IFRSSCD.SignHashAsync"/> returns.</summary>
    public const string ES256 = "ES256";

    /// <summary>The certificate, base64 DER. Its public key is the one the SCU signs with.</summary>
    public string CertificateBase64 { get; set; } = "";

    public string CertificateSerialNumber { get; set; } = "";

    /// <summary><see cref="ES256"/>: ECDSA P-256 over SHA-256, signature in IEEE P1363 (r||s) form.</summary>
    public string SignatureAlgorithm { get; set; } = ES256;
}
