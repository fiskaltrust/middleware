using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.FR.SelfCertified.Models;
using fiskaltrust.Middleware.SCU.FR.SelfCertified.Signing;

namespace fiskaltrust.Middleware.SCU.FR.SelfCertified;

/// <summary>
/// The NF525 signature of a self-certified installation: the publisher's individual attestation
/// stands in for a certificate from a certification body. It builds the same ordered data set as
/// the LNE SCU, signs it with SHA-256withECDSA and chains on the previous signature (ADR 0001).
/// The signed data set is attached to the response so an auditor can re-verify it field by field.
/// </summary>
/// <remarks>
/// The one visible difference from the LNE SCU is that no <see cref="SignatureTypeFR.CertificationBody"/>
/// item is emitted: there is no body whose certificate the receipt could mention. The queue and the
/// SCU switch still tell this SCU apart through <see cref="GetInfoAsync"/>, which reports
/// <see cref="CertificationBody"/>. Deliberately independent of the other SCUs (market-fr#176).
/// </remarks>
public class SelfCertifiedFRSSCD : IFRSSCD, IDisposable
{
    /// <summary>The value reported as <c>CertificationBody</c> in <see cref="FRSSCDInfo.InfoData"/>.</summary>
    public const string CertificationBody = "SelfCertified";

    private readonly SelfCertifiedConfiguration _configuration;
    private readonly SelfCertifiedSignatureCreator _signatureCreator;

    public SelfCertifiedFRSSCD(SelfCertifiedConfiguration configuration)
    {
        _configuration = configuration;
        _signatureCreator = new SelfCertifiedSignatureCreator(configuration.PrivateKey);
    }

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest)
        => Task.FromResult(new EchoResponse { Message = echoRequest.Message });

    public Task<FRSSCDInfo> GetInfoAsync() => Task.FromResult(new SelfCertifiedScuInfo
    {
        Siret = _configuration.Siret,
        CertificateSerialNumber = _configuration.CertificateSerialNumber,
        SoftwareName = _configuration.SoftwareName,
        SoftwareVersion = _configuration.SoftwareVersion,
        Rcs = _configuration.Rcs,
        Naf = _configuration.Naf,
        LegalForm = _configuration.LegalForm,
        VatOption = _configuration.VatOption,
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
            return Task.FromResult(request.ReceiptResponse.ToSigningFailure($"The self-certified SCU refuses receipt {request.ReceiptRequest?.cbReceiptReference}: {ex.Message}", StateFR.Error));
        }
        catch (Exception ex)
        {
            return Task.FromResult(request.ReceiptResponse.ToSigningFailure($"The self-certified signature could not be created for receipt {request.ReceiptRequest?.cbReceiptReference}. The NF525 chain must not continue with an unsigned entry. {ex.Message}"));
        }
    }

    private (ProcessResponse response, string signature) SignReceipt(ProcessRequest request, string? previousSignature)
    {
        var receiptRequest = request.ReceiptRequest;
        var response = request.ReceiptResponse;

        var dataSet = SelfCertifiedDataSetBuilder.Build(receiptRequest, response, _configuration, request.ChainCounter, previousSignature, request.PeriodTotals, FRStateData.CopyNumber(response));

        var signature = _signatureCreator.Sign(dataSet);

        // The QR code carries the data set and its signature; the certificate serial is a field of the data set.
        response.AddReceiptQrCode(FRReceiptQrCode.OfDataSet(dataSet, signature));
        response.AddSignatureItem(SignatureTypeFR.Siret, "SIRET", _configuration.Siret);
        response.AddPeriodTotalsItems(request.PeriodTotals);

        return (new ProcessResponse { ReceiptResponse = response }, signature);
    }

    /// <summary>
    /// The configured certificate of the publisher's key. A self-certified queue signs its
    /// exported archive with the same key as its receipts, like the certified SCUs (ADR 0007
    /// amendment); without it the queue refuses the local archive export.
    /// </summary>
    public Task<FRSigningCertificate> GetSigningCertificateAsync()
        => Task.FromResult(FRHashSigning.SigningCertificate(_signatureCreator.Key, _configuration.CertificateBase64, _configuration.CertificateSerialNumber, CertificationBody));

    public Task<FRHashSignature> SignHashAsync(byte[] sha256) => Task.FromResult(FRHashSigning.SignHash(_signatureCreator.Key, sha256, CertificationBody));

    public void Dispose() => _signatureCreator.Dispose();
}
