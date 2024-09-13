using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors
{
    public class DailyOperationsCommandProcessorPT : IDailyOperationsCommandProcessor
    {
        public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
        {
            var receiptCase = request.ReceiptRequest.ftReceiptCase & 0xFFFF;
            switch (receiptCase)
            {
                case (int) ReceiptCases.ZeroReceipt0x2000:
                    return await ZeroReceipt0x2000Async(request);
                case (int) ReceiptCases.OneReceipt0x2001:
                    return await OneReceipt0x2001Async(request);
                case (int) ReceiptCases.ShiftClosing0x2010:
                    return await ShiftClosing0x2010Async(request);
                case (int) ReceiptCases.DailyClosing0x2011:
                    return await DailyClosing0x2011Async(request);
                case (int) ReceiptCases.MonthlyClosing0x2012:
                    return await MonthlyClosing0x2012Async(request);
                case (int) ReceiptCases.YearlyClosing0x2013:
                    return await YearlyClosing0x2013Async(request);
            }
            request.ReceiptResponse.SetReceiptResponseError($"The given ReceiptCase 0x{request.ReceiptRequest.ftReceiptCase:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.");
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        public async Task<ProcessCommandResponse> ZeroReceipt0x2000Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

        public async Task<ProcessCommandResponse> OneReceipt0x2001Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

        public async Task<ProcessCommandResponse> ShiftClosing0x2010Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

        public async Task<ProcessCommandResponse> DailyClosing0x2011Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

        public async Task<ProcessCommandResponse> MonthlyClosing0x2012Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

        public async Task<ProcessCommandResponse> YearlyClosing0x2013Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));
    }
}