﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueIT.v2
{
    public class LifecycleCommandProcessorIT
    {
        private readonly IITSSCDProvider _itSSCDProvider;
        private readonly IJournalITRepository _journalITRepository;
        private readonly IConfigurationRepository _configurationRepository;

        public LifecycleCommandProcessorIT(IITSSCDProvider itSSCDProvider, IJournalITRepository journalITRepository, IConfigurationRepository configurationRepository)
        {
            _itSSCDProvider = itSSCDProvider;
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

            request.ReceiptResponse.SetReceiptResponseError($"The given ReceiptCase 0x{request.ReceiptRequest.ftReceiptCase:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.");
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        public async Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request)
        {
            // TODO SKE =>  We need to figure a way to retry this functionality in case we fail to do something. There are a few states that 
            //              we need to take care of:
            // - SCU is not rechable => initial operation fails with EEEE_EEEE and needs to be retried by the caller
            // - SCU is reachable but fails internall => initial operation fails with EEEE_EEEE and needs to be retried by the caller
            // - SCU succeeds, but the Queue fails to receive / store the result for whatever reason => initial operation fails with EEEE_EEEE and needs to be retried by the caller but the SCU should be capable of handling that
            // 
            // A few more points regarding the activation
            // - If we fail to receive the result the point of activation doesn't match with the one given in the QueueItem.
            // - 
            var (queue, queueIt, receiptRequest, receiptResponse, queueItem) = request;
            var scu = await _configurationRepository.GetSignaturCreationUnitITAsync(queueIt.ftSignaturCreationUnitITId.Value).ConfigureAwait(false);
            var deviceInfo = await _itSSCDProvider.GetRTInfoAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(scu.InfoJson))
            {
                scu.InfoJson = JsonConvert.SerializeObject(deviceInfo);
                await _configurationRepository.InsertOrUpdateSignaturCreationUnitITAsync(scu).ConfigureAwait(false);
            }

            var signature = SignaturItemFactory.CreateInitialOperationSignature(queueIt, deviceInfo);
            var actionJournal = ftActionJournalFactory.CreateInitialOperationActionJournal(queue, queueItem, queueIt, receiptRequest);

            var result = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = receiptResponse,
            });
            if (result.ReceiptResponse.HasFailed())
            {
                return new ProcessCommandResponse(result.ReceiptResponse, new List<ftActionJournal>());
            }

            var signatures = new List<SignaturItem>
                {
                    signature
                };
            queue.StartMoment = DateTime.UtcNow;
            await _configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
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

            var result = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = receiptResponse,
            });
            if (result.ReceiptResponse.HasFailed())
            {
                return new ProcessCommandResponse(result.ReceiptResponse, new List<ftActionJournal>());
            }

            queue.StopMoment = DateTime.UtcNow;
            await _configurationRepository.InsertOrUpdateQueueAsync(queue);

            var signatureItem = SignaturItemFactory.CreateOutOfOperationSignature(queueIt);
            var actionJournal = ftActionJournalFactory.CreateOutOfOperationActionJournal(queue, queueItem, queueIt, receiptRequest);
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