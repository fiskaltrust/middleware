using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.storage.V0;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueIT.v2
{
    public class ProtocolCommandProcessorIT
    {
        public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
        {
            var receiptCase = (request.ReceiptRequest.ftReceiptCase & 0xFFFF);
            if (receiptCase == (int) ReceiptCases.ProtocolUnspecified0x3000)
                return await ProtocolUnspecified0x3000Async(request);

            if (receiptCase == (int) ReceiptCases.ProtocolTechnicalEvent0x3001)
                return await ProtocolTechnicalEvent0x3001Async(request);

            if (receiptCase == (int) ReceiptCases.ProtocolAccountingEvent0x3002)
                return await ProtocolAccountingEvent0x3002Async(request);

            if (receiptCase == (int) ReceiptCases.InternalUsageMaterialConsumption0x3003)
                return await InternalUsageMaterialConsumption0x3003Async(request);

            if (receiptCase == (int) ReceiptCases.Order0x3004)
                return await Order0x3004Async(request);

            request.ReceiptResponse.SetReceiptResponseErrored($"The given ReceiptCase 0x{request.ReceiptRequest.ftReceiptCase:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.");
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        public async Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }
}