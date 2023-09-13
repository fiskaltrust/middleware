using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.SCU.IT.Abstraction;

public abstract class LegacySCU
    : IITSSCD
{
    public Task<DeviceInfo> GetDeviceInfoAsync() => throw new NotImplementedException();
    public Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => throw new NotImplementedException();
    public Task<FiscalReceiptResponse> FiscalReceiptInvoiceAsync(FiscalReceiptInvoice request) => throw new NotImplementedException();
    public Task<FiscalReceiptResponse> FiscalReceiptRefundAsync(FiscalReceiptRefund request) => throw new NotImplementedException();
    public Task<DailyClosingResponse> ExecuteDailyClosingAsync(DailyClosingRequest request) => throw new NotImplementedException();
    public Task<Response> NonFiscalReceiptAsync(NonFiscalRequest request) => throw new NotImplementedException();
    public abstract Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request);
    public abstract Task<RTInfo> GetRTInfoAsync();
}
