using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Test.Launcher.v2.Extensions;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Helpers.FR;

/// <summary>
/// Round-trips everything crossing the SCU boundary through json, so the launcher exercises the
/// same serialization the real out-of-process SCU hosting does.
/// </summary>
public class FRSSCDJsonWarper : IFRSSCD
{
    private readonly IFRSSCD _frsscd;

    public FRSSCDJsonWarper(IFRSSCD frsscd)
    {
        _frsscd = frsscd;
    }

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => _frsscd.EchoAsync(echoRequest.JsonWarp()!);

    public Task<FRSSCDInfo> GetInfoAsync() => _frsscd.GetInfoAsync();

    public async Task<(ProcessResponse response, string signature)> ProcessReceiptAsync(ProcessRequest request, string? previousSignature)
    {
        var (response, signature) = await _frsscd.ProcessReceiptAsync(request.JsonWarp()!, previousSignature);
        return (response.JsonWarp()!, signature);
    }
}
