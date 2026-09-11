using System.Collections.Generic;
using System.Text.Json;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.FR.InfoCert;

/// <summary>
/// Signature creation data of an Infocert-certified installation. The values mirror the fields of
/// <c>ftSignaturCreationUnitFR</c> so a queue configuration can be handed to the SCU unchanged.
/// </summary>
public class InfoCertConfiguration
{
    /// <summary>SIRET of the establishment the software is declared for (14 digits).</summary>
    public string Siret { get; set; } = "";

    /// <summary>
    /// The EC private key (secp256r1 / NIST P-256) as base64, in PKCS#8 or SEC1 encoding.
    /// </summary>
    public string PrivateKey { get; set; } = "";

    /// <summary>The signing certificate as base64 DER, kept for the audit trail.</summary>
    public string? CertificateBase64 { get; set; }

    /// <summary>Serial number of the signing certificate; printed alongside every signature.</summary>
    public string CertificateSerialNumber { get; set; } = "";

    /// <summary>The Infocert attestation number issued for the certified solution.</summary>
    public string AttestationNumber { get; set; } = "";

    public string SoftwareName { get; set; } = "fiskaltrust.Middleware";

    public string SoftwareVersion { get; set; } = "";

    public static InfoCertConfiguration FromConfiguration(Dictionary<string, object> configuration)
    {
        var serialized = JsonSerializer.Serialize(configuration);
        var result = JsonSerializer.Deserialize<InfoCertConfiguration>(serialized, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new InfoCertConfiguration();

        if (string.IsNullOrWhiteSpace(result.Siret))
        {
            throw new FRValidationException("The Infocert SCU requires the Siret of the establishment in its configuration.");
        }

        if (string.IsNullOrWhiteSpace(result.PrivateKey))
        {
            throw new FRValidationException("The Infocert SCU requires a PrivateKey (base64 encoded secp256r1 key) in its configuration.");
        }

        return result;
    }
}
