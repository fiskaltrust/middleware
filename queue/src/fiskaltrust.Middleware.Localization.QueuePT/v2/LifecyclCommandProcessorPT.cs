using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueuePT.Interface.Models;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePT.v2
{
    public class LifecyclCommandProcessorPT
    {
        public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
        {
            var receiptCase = request.ReceiptRequest.ftReceiptCase & 0xFFFF;
            switch (receiptCase)
            {
                case (int) ReceiptCases.InitialOperationReceipt0x4001:
                    return await InitialOperationReceipt0x4001Async(request);
                case (int) ReceiptCases.OutOfOperationReceipt0x4002:
                    return await OutOfOperationReceipt0x4002Async(request);
                case (int) ReceiptCases.InitSCUSwitch0x4011:
                    return await InitSCUSwitch0x4011Async(request);
                case (int) ReceiptCases.FinishSCUSwitch0x4012:
                    return await FinishSCUSwitch0x4012Async(request);
            }
            request.ReceiptResponse.SetReceiptResponseError($"The given ReceiptCase 0x{request.ReceiptRequest.ftReceiptCase:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.");
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        public async Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> OutOfOperationReceipt0x4002Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }
}