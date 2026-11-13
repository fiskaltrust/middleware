using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

namespace fiskaltrust.Middleware.SCU.FR.InMemory;

/// <summary>
/// A deterministic, certificate-free <see cref="IFRSSCD"/> that produces a well-formed chain
/// without real signature creation data — enough to run QueueFR.v2 acceptance tests and the test
/// launcher. It generates an ephemeral secp256r1 key at construction time, so signatures verify
/// against the key this instance reports but carry no legal weight.
/// </summary>
public class InMemorySCU : IFRSSCD, IDisposable
{
    private readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private readonly string _siret;
    private readonly string _certificateSerialNumber;
    private readonly List<Action<ProcessRequest>> _validators;

    public InMemorySCU(string siret = "00000000000000", string certificateSerialNumber = "INMEMORY-0001", IEnumerable<Action<ProcessRequest>>? validators = null)
    {
        _siret = siret;
        _certificateSerialNumber = certificateSerialNumber;
        _validators = validators?.ToList() ?? new List<Action<ProcessRequest>>();
    }

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest)
        => Task.FromResult(new EchoResponse { Message = echoRequest.Message });

    public Task<FRSSCDInfo> GetInfoAsync() => Task.FromResult(new FRSSCDInfo
    {
        Description = $"fiskaltrust Middleware SCU FR (InMemory), SIRET {_siret}",
        Version = "in-memory",
        InfoData = $"{{\"CertificationBody\":\"InMemory\",\"Siret\":\"{_siret}\",\"CertificateSerialNumber\":\"{_certificateSerialNumber}\",\"SignatureCreationDataAvailable\":true}}",
    });

    public Task<(ProcessResponse response, string signature)> ProcessReceiptAsync(ProcessRequest request, string? previousSignature)
    {
        try
        {
            return Task.FromResult(SignReceipt(request, previousSignature));
        }
        catch (Exception ex)
        {
            return Task.FromResult(request.ReceiptResponse.ToSigningFailure($"The in-memory signature could not be created: {ex.Message}"));
        }
    }

    private (ProcessResponse response, string signature) SignReceipt(ProcessRequest request, string? previousSignature)
    {
        foreach (var validator in _validators)
        {
            validator(request);
        }

        var response = request.ReceiptResponse;
        var dataSet = string.Join('|',
            _siret,
            response.ftQueueItemID.ToString(),
            response.ftReceiptIdentification ?? "",
            response.ftReceiptMoment.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            // market-fr#235: the reprint number of a copy (R13 ENC-DUP-PRN-NUM); empty on every other receipt.
            FRStateData.CopyNumber(response)?.ToString(CultureInfo.InvariantCulture) ?? "",
            (request.ReceiptRequest.cbChargeItems ?? new List<ChargeItem>()).Sum(x => x.Amount).ToString("0.00", CultureInfo.InvariantCulture),
            // market-fr#221: fields 20 and 21 as the LNE and self-certified SCUs sign them: the archive boundaries,
            // the migration event or the closed periods by receipt case, then the period and perpetual totals.
            FRSignedRecords.Field20(request.ReceiptRequest, response),
            FRSignedRecords.Field21(request.PeriodTotals),
            _certificateSerialNumber,
            request.ChainCounter.ToString(CultureInfo.InvariantCulture),
            FRChaining.Flag(previousSignature).ToCode(),
            previousSignature ?? "");

        var dataSetBytes = Encoding.UTF8.GetBytes(dataSet);
        var signature = FRChaining.ToBase64Url(_key.SignData(dataSetBytes, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence));

        response.AddReceiptQrCode(FRReceiptQrCode.OfDataSet(dataSet, signature));
        response.AddSignatureItem(SignatureTypeFR.Siret, "SIRET", _siret);
        response.AddPeriodTotalsItems(request.PeriodTotals);

        return (new ProcessResponse { ReceiptResponse = response }, signature);
    }

    /// <summary>A self-signed certificate over the ephemeral key, so a test archive verifies.</summary>
    public Task<FRSigningCertificate> GetSigningCertificateAsync()
    {
        var request = new CertificateRequest($"CN=fiskaltrust InMemory SCU FR, SERIALNUMBER={_certificateSerialNumber}", _key, HashAlgorithmName.SHA256);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        return Task.FromResult(new FRSigningCertificate
        {
            CertificateBase64 = Convert.ToBase64String(certificate.RawData),
            CertificateSerialNumber = _certificateSerialNumber,
        });
    }

    public Task<FRHashSignature> SignHashAsync(byte[] sha256) => Task.FromResult(FRHashSigning.SignHash(_key, sha256, "InMemory"));

    public void Dispose() => _key.Dispose();
}
