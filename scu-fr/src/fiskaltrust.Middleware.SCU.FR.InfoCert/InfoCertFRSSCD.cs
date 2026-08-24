using System;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.FR.InfoCert.Models;
using fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;

namespace fiskaltrust.Middleware.SCU.FR.InfoCert;

/// <summary>
/// The Infocert flavour of the NF525 signature. It serializes the receipt into a json data set,
/// signs it as an ES256 JWS and returns the SHA-256 of the signing input as the chain hash, so the
/// signature travels as one self-describing token a verifier can check against the certificate
/// without knowing the middleware.
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
        SignatureCreationDataAvailable = true,
    }.ToFRSSCDInfo());

    public Task<(ProcessResponse response, string hash)> ProcessReceiptAsync(ProcessRequest request, string? lastHash)
    {
        var receiptRequest = request.ReceiptRequest;
        var response = request.ReceiptResponse;

        var totals = InfoCertTotals.From(receiptRequest);
        var payload = new InfoCertReceiptPayload
        {
            QueueId = response.ftQueueID,
            QueueItemId = response.ftQueueItemID,
            CashBoxIdentification = response.ftCashBoxIdentification,
            Siret = _configuration.Siret,
            ReceiptId = response.ftReceiptIdentification,
            ReceiptMoment = response.ftReceiptMoment,
            ReceiptCase = (long) receiptRequest.ftReceiptCase,
            Currency = receiptRequest.Currency.ToString(),
            Totalizer = totals.Totalizer,
            CINormal = totals.CINormal,
            CIReduced1 = totals.CIReduced1,
            CIReduced2 = totals.CIReduced2,
            CIReducedS = totals.CIReducedS,
            CIZero = totals.CIZero,
            CIUnknown = totals.CIUnknown,
            PICash = totals.PICash,
            PINonCash = totals.PINonCash,
            PIInternal = totals.PIInternal,
            PIUnknown = totals.PIUnknown,
            LastHash = lastHash ?? "",
            CertificateSerialNumber = _configuration.CertificateSerialNumber,
            AttestationNumber = _configuration.AttestationNumber,
        };

        string jws;
        string chainHash;
        try
        {
            (jws, chainHash) = _signer.Sign(JsonSerializer.Serialize(payload));
        }
        catch (Exception ex) when (ex is not FRSSCDException)
        {
            throw new FRSigningUnavailableException($"The Infocert signature could not be created for receipt {receiptRequest.cbReceiptReference}. The NF525 chain must not continue with an unsigned entry.", ex);
        }

        response.AddSignatureItem(SignatureTypeFR.ReceiptSignature, "www.fiskaltrust.fr", jws, SignatureFormat.QRCode);
        response.AddSignatureItem(SignatureTypeFR.ChainHash, "Empreinte", chainHash);
        response.AddSignatureItem(SignatureTypeFR.CertificateSerialNumber, "Certificat", _configuration.CertificateSerialNumber);
        response.AddSignatureItem(SignatureTypeFR.Siret, "SIRET", _configuration.Siret);
        response.AddSignatureItem(SignatureTypeFR.CertificationBody, "Attestation", $"{CertificationBody} {_configuration.AttestationNumber}".TrimEnd());

        return Task.FromResult((new ProcessResponse { ReceiptResponse = response }, chainHash));
    }

    public void Dispose() => _signer.Dispose();
}
