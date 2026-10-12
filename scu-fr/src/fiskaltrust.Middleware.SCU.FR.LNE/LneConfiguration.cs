using System.Collections.Generic;
using System.Text.Json;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

namespace fiskaltrust.Middleware.SCU.FR.LNE;

/// <summary>
/// Signature creation data of an LNE-certified installation. The values mirror the fields of
/// <c>ftSignaturCreationUnitFR</c> so a queue configuration can be handed to the SCU unchanged.
/// </summary>
public class LneConfiguration
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

    /// <summary>The LNE certificate number issued for the certified solution.</summary>
    public string LneCertificateNumber { get; set; } = "";

    public string SoftwareName { get; set; } = "fiskaltrust.Middleware";

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

    public static LneConfiguration FromConfiguration(Dictionary<string, object> configuration)
    {
        var serialized = JsonSerializer.Serialize(configuration);
        var result = JsonSerializer.Deserialize<LneConfiguration>(serialized, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new LneConfiguration();

        if (string.IsNullOrWhiteSpace(result.Siret))
        {
            throw new FRValidationException("The LNE SCU requires the Siret of the establishment in its configuration.");
        }

        if (string.IsNullOrWhiteSpace(result.PrivateKey))
        {
            throw new FRValidationException("The LNE SCU requires a PrivateKey (base64 encoded secp256r1 key) in its configuration.");
        }

        FRIssuerClaims.ValidateVatOption("LNE", result.VatOption);
        FRIssuerClaims.RejectCharacter("LNE", nameof(Rcs), result.Rcs, Signing.LneDataSetBuilder.FieldSeparator);
        FRIssuerClaims.RejectCharacter("LNE", nameof(Naf), result.Naf, Signing.LneDataSetBuilder.FieldSeparator);
        FRIssuerClaims.RejectCharacter("LNE", nameof(LegalForm), result.LegalForm, Signing.LneDataSetBuilder.FieldSeparator);
        FRIssuerClaims.RejectCharacter("LNE", nameof(ElectronicAddress), result.ElectronicAddress, Signing.LneDataSetBuilder.FieldSeparator);

        return result;
    }
}
