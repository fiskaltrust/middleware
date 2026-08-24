using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.FR.LNE.Models;
using fiskaltrust.Middleware.SCU.FR.LNE.Signing;

namespace fiskaltrust.Middleware.SCU.FR.LNE;

/// <summary>
/// The LNE flavour of the NF525 signature. It builds the ordered "jeu de données à signer",
/// signs it with SHA-256withECDSA and chains over data set plus signature. The signed data set is
/// attached to the response as its own signature item, because an LNE audit reconstructs the
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
        SignatureCreationDataAvailable = true,
    }.ToFRSSCDInfo());

    public Task<(ProcessResponse response, string hash)> ProcessReceiptAsync(ProcessRequest request, string? lastHash)
    {
        var receiptRequest = request.ReceiptRequest;
        var response = request.ReceiptResponse;

        var dataSet = LneDataSetBuilder.Build(receiptRequest, response, _configuration.Siret, _configuration.CertificateSerialNumber, lastHash);

        string signature;
        string chainHash;
        try
        {
            (signature, chainHash) = _signatureCreator.Sign(dataSet);
        }
        catch (Exception ex) when (ex is not FRSSCDException)
        {
            throw new FRSigningUnavailableException($"The LNE signature could not be created for receipt {receiptRequest.cbReceiptReference}. The NF525 chain must not continue with an unsigned entry.", ex);
        }

        response.AddSignatureItem(SignatureTypeFR.ReceiptSignature, "Signature", signature);
        response.AddSignatureItem(SignatureTypeFR.ChainHash, "Empreinte", chainHash);
        response.AddSignatureItem(SignatureTypeFR.Information, "Donnees signees", dataSet);
        response.AddSignatureItem(SignatureTypeFR.CertificateSerialNumber, "Certificat", _configuration.CertificateSerialNumber);
        response.AddSignatureItem(SignatureTypeFR.Siret, "SIRET", _configuration.Siret);
        response.AddSignatureItem(SignatureTypeFR.CertificationBody, "Certification", $"{CertificationBody} {_configuration.LneCertificateNumber}".TrimEnd());

        return Task.FromResult((new ProcessResponse { ReceiptResponse = response }, chainHash));
    }

    public void Dispose() => _signatureCreator.Dispose();
}
