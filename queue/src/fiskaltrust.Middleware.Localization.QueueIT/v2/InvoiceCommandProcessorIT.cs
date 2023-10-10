using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.storage.V0;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueIT.v2
{
    public class InvoiceCommandProcessorIT
    {
        public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
        {
            var receiptCase = (request.ReceiptRequest.ftReceiptCase & 0xFFFF);
            if (receiptCase == (int)ReceiptCases.InvoiceUnknown0x1000)
                return await InvoiceUnknown0x1000Async(request);

            if (receiptCase == (int)ReceiptCases.InvoiceB2C0x1001)
                return await InvoiceB2C0x1001Async(request);

            if (receiptCase == (int)ReceiptCases.InvoiceB2B0x1002)
                return await InvoiceB2B0x1002Async(request);

            if (receiptCase == (int)ReceiptCases.InvoiceB2G0x1003)
                return await InvoiceB2G0x1003Async(request);

            request.ReceiptResponse.SetReceiptResponseErrored($"The given ReceiptCase 0x{request.ReceiptRequest.ftReceiptCase:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.");
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        public async Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }
}