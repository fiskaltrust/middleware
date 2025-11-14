
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.Test.Launcher.v2.Extensions;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Helpers.ES;

public class ESSSCDJsonWarper : IESSSCD
{
    private readonly IESSSCD _essscd;
    public ESSSCDJsonWarper(IESSSCD essscd)
    {
        _essscd = essscd;
    }

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => _essscd.EchoAsync(echoRequest.JsonWarp());
    public Task<ESSSCDInfo> GetInfoAsync() => _essscd.GetInfoAsync();
    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request) => (await _essscd.ProcessReceiptAsync(request.JsonWarp())).JsonWarp()!;
}