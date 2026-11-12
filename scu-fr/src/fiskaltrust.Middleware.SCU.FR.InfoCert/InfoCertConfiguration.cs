using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;

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

    /// <summary>
    /// The Infocert attestation number issued for the certified solution: the product number the
    /// NF525 mention prints (NF525 V2.3 §6.1.6.1, market-fr#131).
    /// </summary>
    public string AttestationNumber { get; set; } = "";

    /// <summary>
    /// The NF525 software category, <c>A</c>, <c>B</c> or <c>C</c> (NF525 V2.3 §2.1), printed in the
    /// NF525 mention (market-fr#131). Without it no mention is emitted.
    /// </summary>
    public string? SoftwareCategory { get; set; }

    /// <summary>The certified software's name; optional in the NF525 mention.</summary>
    public string SoftwareName { get; set; } = "fiskaltrust.Middleware";

    /// <summary>The certified software's version, as on the certificate; printed in the NF525 mention.</summary>
    public string SoftwareVersion { get; set; } = "";

    /// <summary>Issuer claim: RCS registration, e.g. "RCS Nanterre B 642036065" (SOC-RCS, market-fr#141).</summary>
    public string? Rcs { get; set; }

    /// <summary>Issuer claim: NAF/APE code (SOC-NAF).</summary>
    public string? Naf { get; set; }

    /// <summary>Issuer claim: legal form and share capital, e.g. "SAS, au capital de 500000 EUR" (SOC-TYPE, SOC-CAPITAL).</summary>
    public string? LegalForm { get; set; }

    /// <summary>Issuer claim: <c>debits</c> or <c>receipts</c> (SOC-REF-REGTVA, BT-8).</summary>
    public string? VatOption { get; set; }

    /// <summary>Issuer claim: electronic address, <c>SIREN</c> or <c>SIREN_XXX</c> under scheme 0225 (BT-34).</summary>
    public string? ElectronicAddress { get; set; }

    /// <summary>
    /// True in a sandbox (the Launcher's <c>sandbox</c> key). The NF525 mention then reads
    /// "en cours de certif." instead of the attestation number (<see cref="FRSandbox"/>).
    /// </summary>
    [JsonIgnore]
    public bool Sandbox { get; set; }

    public static InfoCertConfiguration FromConfiguration(Dictionary<string, object> configuration)
    {
        var result = FRScuConfiguration.Read<InfoCertConfiguration>(configuration);
        result.Sandbox = FRSandbox.IsSet(configuration);

        if (string.IsNullOrWhiteSpace(result.Siret))
        {
            throw new FRValidationException("The Infocert SCU requires the Siret of the establishment in its configuration.");
        }

        if (string.IsNullOrWhiteSpace(result.PrivateKey))
        {
            throw new FRValidationException("The Infocert SCU requires a PrivateKey (base64 encoded secp256r1 key) in its configuration.");
        }

        FRScuConfiguration.CheckSerialMatchesCertificate("Infocert", result.CertificateBase64, result.CertificateSerialNumber);
        FRIssuerClaims.ValidateVatOption("Infocert", result.VatOption);

        if (!string.IsNullOrEmpty(result.SoftwareCategory) && System.Array.IndexOf(InfoCertMention.Categories, result.SoftwareCategory) < 0)
        {
            throw new FRValidationException($"The Infocert SCU SoftwareCategory must be A, B or C (NF525 V2.3 §2.1), not '{result.SoftwareCategory}'.");
        }

        // Outside a sandbox every justificatif must carry the complete NF525 mention (NF525 V2.3 §6.1.6.1), so the SCU
        // refuses to start without what the mention prints. A sandbox is not certified yet: it starts without them
        // and, where it has a category and a version, prints "en cours de certif." in place of the attestation number.
        if (!result.Sandbox)
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(result.SoftwareCategory))
            {
                missing.Add(nameof(SoftwareCategory));
            }
            if (string.IsNullOrWhiteSpace(result.AttestationNumber))
            {
                missing.Add(nameof(AttestationNumber));
            }
            if (string.IsNullOrWhiteSpace(result.SoftwareVersion))
            {
                missing.Add(nameof(SoftwareVersion));
            }
            if (missing.Count > 0)
            {
                throw new FRValidationException($"The Infocert SCU requires {string.Join(", ", missing)} in its configuration outside a sandbox: every justificatif prints the NF525 mention (NF525 V2.3 §6.1.6.1).");
            }
        }

        return result;
    }
}
