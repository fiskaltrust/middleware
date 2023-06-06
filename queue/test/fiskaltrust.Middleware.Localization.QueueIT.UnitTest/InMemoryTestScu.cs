using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest
{
    internal class InMemoryTestScu : IITSSCD
    {
        public Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => throw new NotImplementedException();
        public Task<DailyClosingResponse> ExecuteDailyClosingAsync(DailyClosingRequest request) => throw new NotImplementedException();

        public Task<FiscalReceiptResponse> FiscalReceiptInvoiceAsync(FiscalReceiptInvoice request)
        {
            return Task.FromResult(new FiscalReceiptResponse()
            {
                Amount = 9909.98m,
                ReceiptNumber = 245,
                ReceiptDateTime = new DateTime(1999, 1, 1, 0, 0, 1),
                Success = true,
            });
        }
        public Task<FiscalReceiptResponse> FiscalReceiptRefundAsync(FiscalReceiptRefund request) => throw new NotImplementedException();
        public Task<DeviceInfo> GetDeviceInfoAsync() => throw new NotImplementedException();
    }
}
