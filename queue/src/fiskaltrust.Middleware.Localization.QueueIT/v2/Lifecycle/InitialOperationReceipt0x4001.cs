﻿using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.ifPOS.v1.it;
using System.Collections.Generic;
using fiskaltrust.Middleware.Contracts.Extensions;
using System;
using System.Linq;
using Newtonsoft.Json;
using fiskaltrust.storage.serialization.DE.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2.Lifecycle
{
    public class InitialOperationReceipt0x4001 : IReceiptTypeProcessor
    {
        private readonly IITSSCDProvider _itSSCDProvider;
        private readonly IConfigurationRepository _configurationRepository;

        public ITReceiptCases ReceiptCase => ITReceiptCases.InitialOperationReceipt0x4001;

        public bool FailureModeAllowed => true;

        public bool GenerateJournalIT => true;

        public InitialOperationReceipt0x4001(IITSSCDProvider itSSCDProvider, IConfigurationRepository configurationRepository)
        {
            _itSSCDProvider = itSSCDProvider;
            _configurationRepository = configurationRepository;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            if (queue.IsNew())
            {
                var scu = await _configurationRepository.GetSignaturCreationUnitITAsync(queueIt.ftSignaturCreationUnitITId.Value).ConfigureAwait(false);
                var deviceInfo = await _itSSCDProvider.GetRTInfoAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(scu.InfoJson))
                {
                    scu.InfoJson = JsonConvert.SerializeObject(deviceInfo);
                    await _configurationRepository.InsertOrUpdateSignaturCreationUnitITAsync(scu).ConfigureAwait(false);
                }

                var signature = SignaturItemFactory.CreateInitialOperationSignature(queueIt, deviceInfo);
                var actionJournal = ActionJournalFactory.CreateInitialOperationActionJournal(queue, queueItem, queueIt, request);
                queue.StartMoment = DateTime.UtcNow;

                var result = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
                {
                    ReceiptRequest = request,
                    ReceiptResponse = receiptResponse,
                });
                if (!result.ReceiptResponse.ftSignatures.Any())
                {
                    result.ReceiptResponse.ftSignatures = new SignaturItem[]
                    {
                        signature
                    };
                }
                return (result.ReceiptResponse, new List<ftActionJournal>
                {
                    actionJournal
                });
            }
            else
            {
                return (receiptResponse, new List<ftActionJournal>
                {
                    ActionJournalFactory.CreateWrongStateForInitialOperationActionJournal(queue, queueItem, request)
                });
            }
        }
    }
}