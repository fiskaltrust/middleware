using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.pt;

namespace fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Validation;

public class MockPTSSCD : IPTSSCD
{
    private int _counter = 0;

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => throw new NotImplementedException();
    public Task<PTSSCDInfo> GetInfoAsync() => throw new NotImplementedException();

    public Task<(ProcessResponse, string)> ProcessReceiptAsync(ProcessRequest request, string lastHash)
    {
        _counter++;
        var hash = $"HASH-{_counter:D40}".PadRight(40, '0');

        return Task.FromResult((
            new ProcessResponse { ReceiptResponse = request.ReceiptResponse },
            hash
        ));
    }
}
