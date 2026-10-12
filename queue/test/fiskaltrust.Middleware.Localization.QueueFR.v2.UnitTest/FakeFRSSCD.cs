using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// A recording IFRSSCD. It hands back a signature derived from the call count so a test can tell
/// the chain entries apart, and can be told to fail so the queue's error handling is exercised.
/// </summary>
public class FakeFRSSCD : IFRSSCD
{
    private int _calls;

    public List<(string? previousSignature, string? receiptIdentification, FRPeriodTotals? periodTotals, long chainCounter)> Calls { get; } = new();

    public Func<Exception>? ThrowOnProcess { get; set; }

    public ProcessRequest? LastRequest { get; private set; }

    public bool HasSignatureCreationData { get; set; } = true;

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => Task.FromResult(new EchoResponse { Message = echoRequest.Message });

    public Task<FRSSCDInfo> GetInfoAsync() => Task.FromResult(new FRSSCDInfo
    {
        InfoData = $"{{\"CertificationBody\":\"Fake\",\"SignatureCreationDataAvailable\":{(HasSignatureCreationData ? "true" : "false")}}}",
    });

    public Task<(ProcessResponse response, string signature)> ProcessReceiptAsync(ProcessRequest request, string? previousSignature)
    {
        Calls.Add((previousSignature, request.ReceiptResponse.ftReceiptIdentification, request.PeriodTotals, request.ChainCounter));
        LastRequest = request;

        if (ThrowOnProcess is not null)
        {
            throw ThrowOnProcess();
        }

        var signature = $"sig-{++_calls}";
        return Task.FromResult((new ProcessResponse { ReceiptResponse = request.ReceiptResponse }, signature));
    }
}
