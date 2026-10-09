using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Test.Launcher.v2.Extensions;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Helpers.IT;

/// <summary>
/// Sends the receipt through a JSON round trip on its way to and from the SCU, as the launcher's gRPC/HTTP
/// client would. The Italian SCUs still implement the ifPOS.v1 contract, which is why this warps v1 objects.
/// </summary>
public class ITSSCDJsonWarper : IITSSCD
{
    private readonly IITSSCD _itsscd;

    public ITSSCDJsonWarper(IITSSCD itsscd)
    {
        _itsscd = itsscd;
    }

    public Task<DeviceInfo> GetDeviceInfoAsync() => _itsscd.GetDeviceInfoAsync();
    public Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => _itsscd.EchoAsync(request.JsonWarp()!);
    public Task<FiscalReceiptResponse> FiscalReceiptInvoiceAsync(FiscalReceiptInvoice request) => _itsscd.FiscalReceiptInvoiceAsync(request);
    public Task<FiscalReceiptResponse> FiscalReceiptRefundAsync(FiscalReceiptRefund request) => _itsscd.FiscalReceiptRefundAsync(request);
    public Task<DailyClosingResponse> ExecuteDailyClosingAsync(DailyClosingRequest request) => _itsscd.ExecuteDailyClosingAsync(request);
    public Task<Response> NonFiscalReceiptAsync(NonFiscalRequest request) => _itsscd.NonFiscalReceiptAsync(request);
    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request) => (await _itsscd.ProcessReceiptAsync(request.JsonWarp()!)).JsonWarp()!;
    public Task<RTInfo> GetRTInfoAsync() => _itsscd.GetRTInfoAsync();
}
