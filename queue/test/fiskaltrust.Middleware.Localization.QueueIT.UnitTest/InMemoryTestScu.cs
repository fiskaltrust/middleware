using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest
{
    internal class InMemoryTestScu : IITSSCD
    {
        public Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => throw new NotImplementedException();
        public Task<EndExportSessionResponse> EndExportSessionAsync(EndExportSessionRequest request) => throw new NotImplementedException();
        public Task<FiscalReceiptResponse> FiscalReceiptInvoiceAsync(FiscalReceiptInvoice request)
        {
            return Task.FromResult(new FiscalReceiptResponse()
            {
                Amount = 9809.98m,
                Number = 245,
                TimeStamp = new DateTime(1999, 1, 1, 0, 0, 1),
                Success = true,
            });
        }
        public Task<FiscalReceiptResponse> FiscalReceiptRefundAsync(FiscalReceiptRefund request) => throw new NotImplementedException();
        public Task<PrinterStatus> GetPrinterInfoAsync() => throw new NotImplementedException();
        public Task<StartExportSessionResponse> StartExportSessionAsync(StartExportSessionRequest request) => throw new NotImplementedException();
    }
}
