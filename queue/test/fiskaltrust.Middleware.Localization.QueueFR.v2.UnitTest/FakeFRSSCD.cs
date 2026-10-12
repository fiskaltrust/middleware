using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// A recording IFRSSCD. It hands back a signature derived from the call count so a test can tell
/// the chain entries apart. It can fail the way a real SCU does, in its response
/// (<see cref="FailWith"/>), or the call itself can fail as a transport would
/// (<see cref="ThrowOnProcess"/>).
/// </summary>
public class FakeFRSSCD : IFRSSCD
{
    private int _calls;

    public List<(string? previousSignature, string? receiptIdentification, FRPeriodTotals? periodTotals, long chainCounter)> Calls { get; } = new();

    public string? FailWith { get; set; }

    /// <summary>
    /// Answers a failure with a json copy of the response, as an SCU behind REST does, so the queue
    /// cannot rely on the SCU having changed its own response object.
    /// </summary>
    public bool FailsOverJson { get; set; }

    public Func<Exception>? ThrowOnProcess { get; set; }

    public ProcessRequest? LastRequest { get; private set; }

    public bool HasSignatureCreationData { get; set; } = true;

    /// <summary>The certificate the fake claims to sign with; a switch needs it to tell two SCUs apart.</summary>
    public string CertificateSerialNumber { get; set; } = "FAKE-1";

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => Task.FromResult(new EchoResponse { Message = echoRequest.Message });

    public Task<FRSSCDInfo> GetInfoAsync() => Task.FromResult(new FRSSCDInfo
    {
        InfoData = $"{{\"CertificationBody\":\"Fake\",\"CertificateSerialNumber\":\"{CertificateSerialNumber}\",\"SignatureCreationDataAvailable\":{(HasSignatureCreationData ? "true" : "false")}}}",
    });

    public Task<(ProcessResponse response, string signature)> ProcessReceiptAsync(ProcessRequest request, string? previousSignature)
    {
        Calls.Add((previousSignature, request.ReceiptResponse.ftReceiptIdentification, request.PeriodTotals, request.ChainCounter));
        LastRequest = request;

        if (ThrowOnProcess is not null)
        {
            throw ThrowOnProcess();
        }

        if (FailWith is not null)
        {
            var response = FailsOverJson ? JsonSerializer.Deserialize<ReceiptResponse>(JsonSerializer.Serialize(request.ReceiptResponse))! : request.ReceiptResponse;
            return Task.FromResult(response.ToSigningFailure(FailWith));
        }

        var signature = $"sig-{++_calls}";
        return Task.FromResult((new ProcessResponse { ReceiptResponse = request.ReceiptResponse }, signature));
    }
}
