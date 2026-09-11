using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.Test.Launcher.v2.Extensions;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Helpers.PL;

public class PLSSCDJsonWarper : IPLSSCD
{
    private readonly IPLSSCD _plsscd;
    public PLSSCDJsonWarper(IPLSSCD plsscd)
    {
        _plsscd = plsscd;
    }

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => _plsscd.EchoAsync(echoRequest.JsonWarp());
    public Task<PLSSCDInfo> GetInfoAsync() => _plsscd.GetInfoAsync();
    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request) => (await _plsscd.ProcessReceiptAsync(request.JsonWarp())).JsonWarp()!;
}
