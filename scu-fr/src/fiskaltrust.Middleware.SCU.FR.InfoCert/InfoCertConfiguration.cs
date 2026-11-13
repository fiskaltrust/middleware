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

    /// <summary>
    /// No longer used (market-fr#221): the seller's electronic address (BT-34) is eInvoicing data and is neither signed
    /// nor reported. Still read so that a configuration carrying it starts; the SCU logs once that it is ignored.
    /// </summary>
    public string? ElectronicAddress { get; set; }

    // market-fr#221 (ADR 0021): the rest of the seller's identity the receipt header shows (R13 ENC-TIK-SOC-*,
    // R12 SOC-*), printed by the issuer item and not signed. Optional; a value not configured has no line.

    /// <summary>Issuer: company name (ENC-TIK-SOC-ETS, SOC-RSO).</summary>
    public string? IssuerName { get; set; }

    /// <summary>Issuer: what the name is, e.g. "Raison sociale" or "Enseigne" (ENC-TIK-SOC-ID).</summary>
    public string? IssuerNameType { get; set; }

    /// <summary>Issuer: street address (ENC-TIK-SOC-ADR, SOC-ADR).</summary>
    public string? IssuerAddress { get; set; }

    /// <summary>Issuer: postcode (ENC-TIK-SOC-CCP).</summary>
    public string? IssuerPostalCode { get; set; }

    /// <summary>Issuer: city (ENC-TIK-SOC-VIL).</summary>
    public string? IssuerCity { get; set; }

    /// <summary>Issuer: country (ENC-TIK-SOC-PAY).</summary>
    public string? IssuerCountry { get; set; }

    /// <summary>Issuer: intra-EU VAT number (ENC-TIK-SOC-TVA, SOC-TVA-NUM); not the VAT option.</summary>
    public string? IssuerVatNumber { get; set; }

    /// <summary>Issuer: share capital (SOC-CAPITAL), e.g. "500000 EUR".</summary>
    public string? ShareCapital { get; set; }

    /// <summary>The seller's identity the issuer item prints.</summary>
    public FRIssuer Issuer() => new(Siret, IssuerName, IssuerNameType, IssuerAddress, IssuerPostalCode, IssuerCity, IssuerCountry,
        IssuerVatNumber, Rcs, Naf, LegalForm, ShareCapital, VatOption);

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

        // Every justificatif carries the complete NF525 mention (NF525 V2.3 §6.1.6.1), so the SCU refuses to start
        // without what the mention prints. The category and the version are known before certification and are always
        // required; only a sandbox, not certified yet, may lack the attestation number: it prints "en cours de certif."
        // in its place.
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(result.SoftwareCategory))
        {
            missing.Add(nameof(SoftwareCategory));
        }
        if (!result.Sandbox && string.IsNullOrWhiteSpace(result.AttestationNumber))
        {
            missing.Add(nameof(AttestationNumber));
        }
        if (string.IsNullOrWhiteSpace(result.SoftwareVersion))
        {
            missing.Add(nameof(SoftwareVersion));
        }
        if (missing.Count > 0)
        {
            throw new FRValidationException($"The Infocert SCU requires {string.Join(", ", missing)} in its configuration{(result.Sandbox ? "" : " outside a sandbox")}: every justificatif prints the NF525 mention (NF525 V2.3 §6.1.6.1).");
        }

        return result;
    }
}
