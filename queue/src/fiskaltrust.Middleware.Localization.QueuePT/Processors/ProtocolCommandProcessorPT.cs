using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors
{
    public class ProtocolCommandProcessorPT : IProtocolCommandProcessor
    {
        public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
        {
            var receiptCase = request.ReceiptRequest.ftReceiptCase & 0xFFFF;
            switch (receiptCase)
            {
                case (int) ReceiptCases.ProtocolUnspecified0x3000:
                    return await ProtocolUnspecified0x3000Async(request);
                case (int) ReceiptCases.ProtocolTechnicalEvent0x3001:
                    return await ProtocolTechnicalEvent0x3001Async(request);
                case (int) ReceiptCases.ProtocolAccountingEvent0x3002:
                    return await ProtocolAccountingEvent0x3002Async(request);
                case (int) ReceiptCases.InternalUsageMaterialConsumption0x3003:
                    return await InternalUsageMaterialConsumption0x3003Async(request);
                case (int) ReceiptCases.Order0x3004:
                    return await Order0x3004Async(request);
                case (int) ReceiptCases.CopyReceiptPrintExistingReceipt0x3010:
                    return await CopyReceiptPrintExistingReceipt0x3010Async(request);
            }
            request.ReceiptResponse.SetReceiptResponseError($"The given ftReceiptCase 0x{request.ReceiptRequest.ftReceiptCase:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.");
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        public async Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }
}