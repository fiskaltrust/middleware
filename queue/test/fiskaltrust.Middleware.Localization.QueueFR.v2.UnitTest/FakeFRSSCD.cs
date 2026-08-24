using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// A recording IFRSSCD. It hands back a hash derived from the call count so a test can tell the
/// chain entries apart, and can be told to fail so the queue's error handling is exercised.
/// </summary>
public class FakeFRSSCD : IFRSSCD
{
    private int _calls;

    public List<(string? lastHash, string? receiptIdentification, FRPeriodTotals? periodTotals)> Calls { get; } = new();

    public Func<Exception>? ThrowOnProcess { get; set; }

    public bool HasSignatureCreationData { get; set; } = true;

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => Task.FromResult(new EchoResponse { Message = echoRequest.Message });

    public Task<FRSSCDInfo> GetInfoAsync() => Task.FromResult(new FRSSCDInfo
    {
        InfoData = $"{{\"CertificationBody\":\"Fake\",\"SignatureCreationDataAvailable\":{(HasSignatureCreationData ? "true" : "false")}}}",
    });

    public Task<(ProcessResponse response, string hash)> ProcessReceiptAsync(ProcessRequest request, string? lastHash)
    {
        Calls.Add((lastHash, request.ReceiptResponse.ftReceiptIdentification, request.PeriodTotals));

        if (ThrowOnProcess is not null)
        {
            throw ThrowOnProcess();
        }

        var hash = $"hash-{++_calls}";
        return Task.FromResult((new ProcessResponse { ReceiptResponse = request.ReceiptResponse }, hash));
    }
}
