using System;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.FR.InfoCert.Models;
using fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;

namespace fiskaltrust.Middleware.SCU.FR.InfoCert;

/// <summary>
/// The Infocert flavour of the NF525 signature. It serializes the receipt into a json data set,
/// signs it as an ES256 JWS and returns the JWS signature segment, which the next entry of the
/// chain carries (ADR 0001). The signature travels as one self-describing token a verifier can
/// check against the certificate without knowing the middleware.
/// </summary>
/// <remarks>
/// Deliberately independent of the LNE SCU: the two certification bodies audit against their own
/// referential, so neither implementation may drift because the other one changed. Only the
/// contract, the signature type numbering and the ftState mapping are shared.
/// </remarks>
public class InfoCertFRSSCD : IFRSSCD, IDisposable
{
    public const string CertificationBody = "Infocert";

    private readonly InfoCertConfiguration _configuration;
    private readonly InfoCertJwsSigner _signer;

    public InfoCertFRSSCD(InfoCertConfiguration configuration)
    {
        _configuration = configuration;
        _signer = new InfoCertJwsSigner(configuration.PrivateKey);
    }

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest)
        => Task.FromResult(new EchoResponse { Message = echoRequest.Message });

    public Task<FRSSCDInfo> GetInfoAsync() => Task.FromResult(new InfoCertScuInfo
    {
        Siret = _configuration.Siret,
        CertificateSerialNumber = _configuration.CertificateSerialNumber,
        AttestationNumber = _configuration.AttestationNumber,
        SoftwareName = _configuration.SoftwareName,
        SoftwareVersion = _configuration.SoftwareVersion,
        SoftwareCategory = _configuration.SoftwareCategory,
        Rcs = _configuration.Rcs,
        Naf = _configuration.Naf,
        LegalForm = _configuration.LegalForm,
        VatOption = _configuration.VatOption,
        ElectronicAddress = _configuration.ElectronicAddress,
        SignatureCreationDataAvailable = true,
    }.ToFRSSCDInfo());

    public Task<(ProcessResponse response, string signature)> ProcessReceiptAsync(ProcessRequest request, string? previousSignature)
    {
        try
        {
            return Task.FromResult(SignReceipt(request, previousSignature));
        }
        catch (Exception ex)
        {
            return Task.FromResult(request.ReceiptResponse.ToSigningFailure($"The Infocert signature could not be created for receipt {request.ReceiptRequest?.cbReceiptReference}. The NF525 chain must not continue with an unsigned entry. {ex.Message}"));
        }
    }

    private (ProcessResponse response, string signature) SignReceipt(ProcessRequest request, string? previousSignature)
    {
        var receiptRequest = request.ReceiptRequest;
        var response = request.ReceiptResponse;

        // A technical event is an entry of the JET (R19 v4.2): an event without a valid R19 or
        // editor code and a label cannot be signed as one, so it is refused before it takes a
        // number of chain L (market-fr#200).
        if (FRJetEvent.IsJetEvent(receiptRequest) && !FRJetEvent.TryRead(receiptRequest, out _, out var jetError))
        {
            return response.ToSigningFailure($"The technical event cannot enter the NF525 technical event journal: {jetError}", StateFR.Error);
        }

        // market-fr#108, #221: the JWS payload is the string R13/R12/R19 define for this kind of entry (the 25-field data
        // set on chain E), not a JSON object. The protected header names the certificate serial as kid.
        var (jws, signature) = _signer.Sign(InfoCertSignedString.Build(request, previousSignature, _configuration), _configuration.CertificateSerialNumber);

        // The QR code is the compact JWS; the certificate serial is its header's kid.
        response.AddReceiptQrCode(jws);
        response.AddSignatureItem(SignatureTypeFR.Siret, "SIRET", _configuration.Siret);
        // market-fr#131: the NF525 mention (NF525 V2.3 §6.1.6.1) is the whole line, so the caption
        // stays empty. It is left out rather than emitted incomplete. A sandbox is not certified, so
        // "en cours de certif." stands where the attestation number goes.
        var productNumber = _configuration.Sandbox ? FRSandbox.PendingCertification : _configuration.AttestationNumber;
        var mention = InfoCertMention.Build(_configuration.SoftwareCategory, productNumber, signature, _configuration.SoftwareName, _configuration.SoftwareVersion);
        if (mention is not null)
        {
            response.AddSignatureItem(SignatureTypeFR.CertificationBody, "", mention);
        }
        response.AddPeriodTotalsItems(request.PeriodTotals);

        // The VAT summary is printed on the receipt (R13 v4.2 Récapitulatif), so it travels as
        // signature items (ADR 0011), one per rate. R13 forbids it (I) on a note, a pro forma and an
        // order, which are not the sale's receipt (NF525 V2.3 §6.2.2.3); what is signed is unchanged.
        if (!PrintsNoVatSummary(receiptRequest))
        {
            foreach (var (rate, line) in InfoCertVatSummary.From(receiptRequest) ?? new Dictionary<string, InfoCertVatRate>())
            {
                response.AddSignatureItem(SignatureTypeFR.VatSummary, $"TVA {rate} %", InfoCertVatSummary.Format(line));
            }
        }

        return (new ProcessResponse { ReceiptResponse = response }, signature);
    }

    /// <summary>A note (0x0006), a pro forma (0x0007) or an order (0x3004), training or not.</summary>
    private static bool PrintsNoVatSummary(ReceiptRequest request) => ((long) request.ftReceiptCase & 0xFFFF) is 0x0006 or 0x0007 or 0x3004;

    public Task<FRSigningCertificate> GetSigningCertificateAsync()
        => Task.FromResult(FRHashSigning.SigningCertificate(_signer.Key, _configuration.CertificateBase64, _configuration.CertificateSerialNumber, CertificationBody));

    public Task<FRHashSignature> SignHashAsync(byte[] sha256) => Task.FromResult(FRHashSigning.SignHash(_signer.Key, sha256, CertificationBody));

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    public void Dispose() => _signer.Dispose();
}
