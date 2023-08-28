using fiskaltrust.ifPOS.v1;
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
                var (actionJournal, signature) = await InitializeSCUAsync(queue, queueIt, request, queueItem);
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
                var actionJournalEntry = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}",
                    queueItem.ftQueueItemId, queue.IsDeactivated()
                            ? $"Queue {queue.ftQueueId} is de-activated, initial-operations-receipt can not be executed."
                            : $"Queue {queue.ftQueueId} is already activated, initial-operations-receipt can not be executed.", "");

                return (receiptResponse, new List<ftActionJournal>
                {
                    actionJournalEntry
                });
            }
        }

        private async Task<(ftActionJournal, SignaturItem)> InitializeSCUAsync(ftQueue queue, ftQueueIT queueIT, ReceiptRequest request, ftQueueItem queueItem)
        {
            var scu = await _configurationRepository.GetSignaturCreationUnitITAsync(queueIT.ftSignaturCreationUnitITId.Value).ConfigureAwait(false);
            var deviceInfo = await _itSSCDProvider.GetRTInfoAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(scu.InfoJson))
            {
                scu.InfoJson = JsonConvert.SerializeObject(deviceInfo);
                await _configurationRepository.InsertOrUpdateSignaturCreationUnitITAsync(scu).ConfigureAwait(false);
            }

            var signatureItem = CreateInitialOperationSignature($"Queue-ID: {queue.ftQueueId} Serial-Nr: {deviceInfo.SerialNumber}");
            var notification = new ActivateQueueSCU
            {
                CashBoxId = Guid.Parse(request.ftCashBoxID),
                QueueId = queueItem.ftQueueId,
                Moment = DateTime.UtcNow,
                SCUId = queueIT.ftSignaturCreationUnitITId.GetValueOrDefault(),
                IsStartReceipt = true,
                Version = "V0",
            };
            var actionJournal = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-{nameof(ActivateQueueSCU)}",
                queueItem.ftQueueItemId, $"Initial-Operation receipt. Queue-ID: {queue.ftQueueId}", JsonConvert.SerializeObject(notification));

            return (actionJournal, signatureItem);
        }

        public SignaturItem CreateInitialOperationSignature(string data)
        {
            return new SignaturItem()
            {
                ftSignatureType = Cases.BASE_STATE & 0x3,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                Caption = $"Initial-operation receipt",
                Data = data
            };
        }

        protected ftActionJournal CreateActionJournal(Guid queueId, string type, Guid queueItemId, string message, string data, int priority = -1)
        {
            return new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queueId,
                ftQueueItemId = queueItemId,
                Type = type,
                Moment = DateTime.UtcNow,
                Message = message,
                Priority = priority,
                DataJson = data
            };
        }
    }
}
