using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
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

    public Task<(ProcessResponse response, string hash)> ProcessReceiptAsync(ProcessRequest request, string? lastHash)
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
            (request.ReceiptRequest.cbChargeItems ?? new List<ChargeItem>()).Sum(x => x.Amount).ToString("0.00", CultureInfo.InvariantCulture),
            PeriodTotals(request.PeriodTotals),
            lastHash ?? "");

        var dataSetBytes = Encoding.UTF8.GetBytes(dataSet);
        var signature = Convert.ToBase64String(_key.SignData(dataSetBytes, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence));
        var chainHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes($"{dataSet}|{signature}")));

        if (request.PeriodTotals is not null)
        {
            response.AddSignatureItem(SignatureTypeFR.PerpetualTotals, "Total perpetuel", request.PeriodTotals.Perpetual.Totalizer.ToString("0.00", CultureInfo.InvariantCulture));
            response.AddSignatureItem(PeriodSignatureType(request.PeriodTotals.Period), $"Total {request.PeriodTotals.Period}", request.PeriodTotals.Current.Totalizer.ToString("0.00", CultureInfo.InvariantCulture));
        }

        response.AddSignatureItem(SignatureTypeFR.ReceiptSignature, "Signature", signature);
        response.AddSignatureItem(SignatureTypeFR.ChainHash, "Empreinte", chainHash);
        response.AddSignatureItem(SignatureTypeFR.CertificateSerialNumber, "Certificat", _certificateSerialNumber);
        response.AddSignatureItem(SignatureTypeFR.Siret, "SIRET", _siret);

        return Task.FromResult((new ProcessResponse { ReceiptResponse = response }, chainHash));
    }

    /// <summary>The accumulated totals a closing attests, folded into the signed data set.</summary>
    private static string PeriodTotals(FRPeriodTotals? periodTotals) => periodTotals is null
        ? ""
        : $"{periodTotals.Period}:{periodTotals.Current.Totalizer.ToString("0.00", CultureInfo.InvariantCulture)}:{periodTotals.Perpetual.Totalizer.ToString("0.00", CultureInfo.InvariantCulture)}";

    private static SignatureTypeFR PeriodSignatureType(FRTotalsPeriod period) => period switch
    {
        FRTotalsPeriod.Month => SignatureTypeFR.MonthTotals,
        FRTotalsPeriod.Year => SignatureTypeFR.YearTotals,
        _ => SignatureTypeFR.DayTotals,
    };

    public void Dispose() => _key.Dispose();
}
