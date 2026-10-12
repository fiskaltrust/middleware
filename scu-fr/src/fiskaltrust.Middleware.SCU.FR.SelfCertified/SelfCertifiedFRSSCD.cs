using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;
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
        SignatureCreationDataAvailable = true,
    }.ToFRSSCDInfo());

    public Task<(ProcessResponse response, string signature)> ProcessReceiptAsync(ProcessRequest request, string? previousSignature)
    {
        var receiptRequest = request.ReceiptRequest;
        var response = request.ReceiptResponse;

        var dataSet = SelfCertifiedDataSetBuilder.Build(receiptRequest, response, _configuration.Siret, _configuration.CertificateSerialNumber, request.ChainCounter, previousSignature, request.PeriodTotals);

        string signature;
        try
        {
            signature = _signatureCreator.Sign(dataSet);
        }
        catch (Exception ex) when (ex is not FRSSCDException)
        {
            throw new FRSigningUnavailableException($"The self-certified signature could not be created for receipt {receiptRequest.cbReceiptReference}. The NF525 chain must not continue with an unsigned entry.", ex);
        }

        response.AddSignatureItem(SignatureTypeFR.ReceiptSignature, "Signature", signature);
        response.AddSignatureItem(SignatureTypeFR.Information, "Donnees signees", dataSet);
        response.AddSignatureItem(SignatureTypeFR.CertificateSerialNumber, "Certificat", _configuration.CertificateSerialNumber);
        response.AddSignatureItem(SignatureTypeFR.Siret, "SIRET", _configuration.Siret);

        return Task.FromResult((new ProcessResponse { ReceiptResponse = response }, signature));
    }

    public void Dispose() => _signatureCreator.Dispose();
}
