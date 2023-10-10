using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueIT.v2
{
    public class LifecyclCommandProcessorIT
    {
        private readonly IJournalITRepository _journalITRepository;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IITSSCD _itSSCD;

        public LifecyclCommandProcessorIT(IITSSCD iTSSCD, IJournalITRepository journalITRepository, IConfigurationRepository configurationRepository)
        {
            _itSSCD = iTSSCD;
            _journalITRepository = journalITRepository;
            _configurationRepository = configurationRepository;
        }

        public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
        {
            var receiptCase = (request.ReceiptRequest.ftReceiptCase & 0xFFFF);
            if (receiptCase == (int) ReceiptCases.InitialOperationReceipt0x4001)
                return await InitialOperationReceipt0x4001Async(request);

            if (receiptCase == (int) ReceiptCases.OutOfOperationReceipt0x4002)
                return await OutOfOperationReceipt0x4002Async(request);

            if (receiptCase == (int) ReceiptCases.InitSCUSwitch0x4011)
                return await InitSCUSwitch0x4011Async(request);

            if (receiptCase == (int) ReceiptCases.FinishSCUSwitch0x4012)
                return await FinishSCUSwitch0x4012Async(request);

            request.ReceiptResponse.SetReceiptResponseErrored($"The given ReceiptCase 0x{request.ReceiptRequest.ftReceiptCase:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.");
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        public async Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request)
        {
            var (queue, queueIt, receiptRequest, receiptResponse, queueItem) = request;
            var scu = await _configurationRepository.GetSignaturCreationUnitITAsync(queueIt.ftSignaturCreationUnitITId.Value).ConfigureAwait(false);
            var deviceInfo = await _itSSCD.GetRTInfoAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(scu.InfoJson))
            {
                scu.InfoJson = JsonConvert.SerializeObject(deviceInfo);
                await _configurationRepository.InsertOrUpdateSignaturCreationUnitITAsync(scu).ConfigureAwait(false);
            }

            var signature = SignaturItemFactory.CreateInitialOperationSignature(queueIt, deviceInfo);
            var actionJournal = ActionJournalFactory.CreateInitialOperationActionJournal(queue, queueItem, queueIt, receiptRequest);
            queue.StartMoment = DateTime.UtcNow;

            await _configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
            var result = await _itSSCD.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = receiptResponse,
            });

            var signatures = new List<SignaturItem>
                {
                    signature
                };
            signatures.AddRange(result.ReceiptResponse.ftSignatures);
            receiptResponse.ftSignatures = signatures.ToArray();

            return new ProcessCommandResponse(receiptResponse, new List<ftActionJournal>
                {
                    actionJournal
                });
        }

        public async Task<ProcessCommandResponse> OutOfOperationReceipt0x4002Async(ProcessCommandRequest request)
        {
            var (queue, queueIt, receiptRequest, receiptResponse, queueItem) = request;
            var result = await _itSSCD.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = receiptResponse,
            });
            queue.StopMoment = DateTime.UtcNow;
            await _configurationRepository.InsertOrUpdateQueueAsync(queue);

            var signatureItem = SignaturItemFactory.CreateOutOfOperationSignature(queueIt);
            var actionJournal = ActionJournalFactory.CreateOutOfOperationActionJournal(queue, queueItem, queueIt, receiptRequest);
            var signatures = new List<SignaturItem>
                {
                    signatureItem
                };
            signatures.AddRange(result.ReceiptResponse.ftSignatures);
            receiptResponse.ftSignatures = signatures.ToArray();
            return new ProcessCommandResponse(receiptResponse, new List<ftActionJournal>
                {
                    actionJournal
                });
        }

        public async Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }
}