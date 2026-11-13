using System.Collections.Generic;
using System.Text.Json;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

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

    /// <summary>Issuer claim: RCS registration, e.g. "RCS Nanterre B 642036065" (SOC-RCS, market-fr#141).</summary>
    public string? Rcs { get; set; }

    /// <summary>Issuer claim: NAF/APE code (SOC-NAF).</summary>
    public string? Naf { get; set; }

    /// <summary>Issuer claim: legal form and share capital, e.g. "SAS, au capital de 500000 EUR" (SOC-TYPE, SOC-CAPITAL).</summary>
    public string? LegalForm { get; set; }

    /// <summary>Issuer claim: <c>debits</c> or <c>receipts</c> (SOC-REF-REGTVA, BT-8).</summary>
    public string? VatOption { get; set; }

    /// <summary>
    /// No longer used (market-fr#221): the seller's electronic address (BT-34) is eInvoicing data and is
    /// neither signed nor reported. Still read so that a configuration carrying it starts; the SCU logs once that it
    /// is ignored.
    /// </summary>
    public string? ElectronicAddress { get; set; }

    public static SelfCertifiedConfiguration FromConfiguration(Dictionary<string, object> configuration)
    {
        var result = FRScuConfiguration.Read<SelfCertifiedConfiguration>(configuration);

        if (string.IsNullOrWhiteSpace(result.Siret))
        {
            throw new FRValidationException("The self-certified SCU requires the Siret of the establishment in its configuration.");
        }

        if (string.IsNullOrWhiteSpace(result.PrivateKey))
        {
            throw new FRValidationException("The self-certified SCU requires a PrivateKey (base64 encoded secp256r1 key) in its configuration.");
        }

        FRScuConfiguration.CheckSerialMatchesCertificate("self-certified", result.CertificateBase64, result.CertificateSerialNumber);
        FRIssuerClaims.RejectCharacter("self-certified", nameof(Siret), result.Siret, Signing.SelfCertifiedDataSetBuilder.FieldSeparator);
        FRIssuerClaims.RejectCharacter("self-certified", nameof(CertificateSerialNumber), result.CertificateSerialNumber, Signing.SelfCertifiedDataSetBuilder.FieldSeparator);
        FRIssuerClaims.ValidateVatOption("self-certified", result.VatOption);
        FRIssuerClaims.RejectCharacter("self-certified", nameof(Rcs), result.Rcs, Signing.SelfCertifiedDataSetBuilder.FieldSeparator);
        FRIssuerClaims.RejectCharacter("self-certified", nameof(Naf), result.Naf, Signing.SelfCertifiedDataSetBuilder.FieldSeparator);
        FRIssuerClaims.RejectCharacter("self-certified", nameof(LegalForm), result.LegalForm, Signing.SelfCertifiedDataSetBuilder.FieldSeparator);

        return result;
    }
}
