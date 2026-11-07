using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.FR.LNE.Models;
using fiskaltrust.Middleware.SCU.FR.LNE.Signing;

namespace fiskaltrust.Middleware.SCU.FR.LNE;

/// <summary>
/// The LNE flavour of the NF525 signature. It builds the ordered "jeu de données à signer",
/// signs it with SHA-256withECDSA and chains on the previous signature (ADR 0001). The signed data set is
/// returned in the receipt's QR code item, followed by its signature, because an LNE audit reconstructs the
/// signed string from the archive and re-verifies it field by field.
/// </summary>
/// <remarks>
/// Deliberately independent of the Infocert SCU: the two certification bodies audit against their
/// own referential, so neither implementation may drift because the other one changed. Only the
/// contract, the signature type numbering and the ftState mapping are shared.
/// </remarks>
public class LneFRSSCD : IFRSSCD, IDisposable
{
    public const string CertificationBody = "LNE";

    /// <summary>The certification mention LNE Rev. 1.8 Exigence 9 recommends on every ticket.</summary>
    public const string Mention = "Système de caisse certifié LNE";

    /// <summary>The line a sandbox prints instead: a sandbox SCU is not certified (market-fr#221, 2026-11-07).</summary>
    public const string SandboxMention = FRSandbox.PendingCertification + " LNE";

    private readonly LneConfiguration _configuration;
    private readonly LneSignatureCreator _signatureCreator;

    public LneFRSSCD(LneConfiguration configuration)
    {
        _configuration = configuration;
        _signatureCreator = new LneSignatureCreator(configuration.PrivateKey);
    }

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest)
        => Task.FromResult(new EchoResponse { Message = echoRequest.Message });

    public Task<FRSSCDInfo> GetInfoAsync() => Task.FromResult(new LneScuInfo
    {
        Siret = _configuration.Siret,
        CertificateSerialNumber = _configuration.CertificateSerialNumber,
        LneCertificateNumber = _configuration.LneCertificateNumber,
        SoftwareName = _configuration.SoftwareName,
        SoftwareVersion = _configuration.SoftwareVersion,
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
        catch (FRValidationException ex)
        {
            // A value the data set cannot hold: this receipt is refused, the SCU is not unavailable.
            return Task.FromResult(request.ReceiptResponse.ToSigningFailure($"The LNE SCU refuses receipt {request.ReceiptRequest?.cbReceiptReference}: {ex.Message}", StateFR.Error));
        }
        catch (Exception ex)
        {
            return Task.FromResult(request.ReceiptResponse.ToSigningFailure($"The LNE signature could not be created for receipt {request.ReceiptRequest?.cbReceiptReference}. The NF525 chain must not continue with an unsigned entry. {ex.Message}"));
        }
    }

    private (ProcessResponse response, string signature) SignReceipt(ProcessRequest request, string? previousSignature)
    {
        var receiptRequest = request.ReceiptRequest;
        var response = request.ReceiptResponse;

        var dataSet = LneDataSetBuilder.Build(receiptRequest, response, _configuration, request.ChainCounter, previousSignature, request.PeriodTotals, FRStateData.CopyNumber(response), FRStateData.Archive(response));

        var signature = _signatureCreator.Sign(dataSet);

        // The QR code carries the data set and its signature; the certificate serial is a field of the data set.
        response.AddReceiptQrCode(FRReceiptQrCode.OfDataSet(dataSet, signature));
        response.AddSignatureItem(SignatureTypeFR.Siret, "SIRET", _configuration.Siret);
        // market-fr#131: the mention LNE Rev. 1.8 Exigence 9 recommends, with the LNE certificate
        // number (an acceptable solution of the same exigence). Printed on every ticket, so it stays
        // short: the full signature the exigence recommends is in the QR code. Not signed.
        // A sandbox is not certified (LNE Rev. 1.8 §III.10): it claims no certification and prints no number.
        if (_configuration.Sandbox)
        {
            response.AddSignatureItem(SignatureTypeFR.CertificationBody, SandboxMention, "");
        }
        else
        {
            response.AddSignatureItem(SignatureTypeFR.CertificationBody, Mention, _configuration.LneCertificateNumber);
        }
        response.AddPeriodTotalsItems(request.PeriodTotals);

        return (new ProcessResponse { ReceiptResponse = response }, signature);
    }

    public Task<FRSigningCertificate> GetSigningCertificateAsync()
        => Task.FromResult(FRHashSigning.SigningCertificate(_signatureCreator.Key, _configuration.CertificateBase64, _configuration.CertificateSerialNumber, CertificationBody));

    public Task<FRHashSignature> SignHashAsync(byte[] sha256) => Task.FromResult(FRHashSigning.SignHash(_signatureCreator.Key, sha256, CertificationBody));

    public void Dispose() => _signatureCreator.Dispose();
}
