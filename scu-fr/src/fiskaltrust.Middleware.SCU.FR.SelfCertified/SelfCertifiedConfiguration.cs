using System.Collections.Generic;
using System.Text.Json;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.FR.SelfCertified;

/// <summary>
/// Signature creation data of a self-certified installation. The values mirror the fields of
/// <c>ftSignaturCreationUnitFR</c> so a queue configuration can be handed to the SCU unchanged.
/// Unlike the LNE and Infocert SCUs there is no certificate or attestation number of a
/// certification body: the publisher attests conformity itself.
/// </summary>
public class SelfCertifiedConfiguration
{
    /// <summary>SIRET of the establishment the software is declared for (14 digits).</summary>
    public string Siret { get; set; } = "";

    /// <summary>
    /// The EC private key (secp256r1 / NIST P-256) as base64, in PKCS#8 or SEC1 encoding.
    /// </summary>
    public string PrivateKey { get; set; } = "";

    /// <summary>The signing certificate as base64 DER, kept for the audit trail.</summary>
    public string? CertificateBase64 { get; set; }

    /// <summary>
    /// Serial number of the signing certificate; printed alongside every signature and the key the
    /// archive is split on when the SCU or its certificate changes (ADR 0005).
    /// </summary>
    public string CertificateSerialNumber { get; set; } = "";

    public string SoftwareName { get; set; } = "fiskaltrust.Middleware";

    public string SoftwareVersion { get; set; } = "";

    public static SelfCertifiedConfiguration FromConfiguration(Dictionary<string, object> configuration)
    {
        var serialized = JsonSerializer.Serialize(configuration);
        var result = JsonSerializer.Deserialize<SelfCertifiedConfiguration>(serialized, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new SelfCertifiedConfiguration();

        if (string.IsNullOrWhiteSpace(result.Siret))
        {
            throw new FRValidationException("The self-certified SCU requires the Siret of the establishment in its configuration.");
        }

        if (string.IsNullOrWhiteSpace(result.PrivateKey))
        {
            throw new FRValidationException("The self-certified SCU requires a PrivateKey (base64 encoded secp256r1 key) in its configuration.");
        }

        return result;
    }
}
