using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class SignProcessor : IMarketSpecificSignProcessor
    {
        protected readonly IConfigurationRepository _configurationRepository;
        private readonly SignProcessorIT _signProcessorIT;

        public SignProcessor(IConfigurationRepository configurationRepository, SignProcessorIT signProcessorIT)
        {
            _configurationRepository = configurationRepository;
            _signProcessorIT = signProcessorIT;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {

            var receiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
            var receiptResponse = new ReceiptResponse
            {
                ftCashBoxID = request.ftCashBoxID,
                ftQueueID = queueItem.ftQueueId.ToString(),
                ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                ftQueueRow = queueItem.ftQueueRow,
                cbTerminalID = request.cbTerminalID,
                cbReceiptReference = request.cbReceiptReference,
                ftReceiptMoment = DateTime.UtcNow,
                ftState = Cases.BASE_STATE,
                ftReceiptIdentification = receiptIdentification
            };
            if (queue.IsDeactivated())
            {
                return ReturnWithQueueIsDisabled(queue, receiptResponse, queueItem);
            }

            if (request.IsInitialOperation() && !queue.IsNew())
            {
                receiptResponse.SetReceiptResponseError("The queue is already operational. It is not allowed to send another InitOperation Receipt");
                return (receiptResponse, new List<ftActionJournal>());
            }

            if (!request.IsInitialOperation() && queue.IsNew())
            {
                return ReturnWithQueueIsNotActive(queue, receiptResponse, queueItem);
            }
            return await _signProcessorIT.ProcessAsync(request, receiptResponse, queue, queueItem).ConfigureAwait(false);
        }

        public (ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals) ReturnWithQueueIsNotActive(ftQueue queue, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            var actionJournals = new List<ftActionJournal>
            {
                new ftActionJournal
                {
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueId = queueItem.ftQueueId,
                    ftQueueItemId = queueItem.ftQueueItemId,
                    Moment = DateTime.UtcNow,
                    Message = $"QueueId {queueItem.ftQueueId} has not been activated yet."
                }
            };
            receiptResponse.ftState += ftStatesFlags.SECURITY_MECHAMISN_DEACTIVATED;
            receiptResponse.ftReceiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
            return (receiptResponse, actionJournals);
        }

        public (ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals) ReturnWithQueueIsDisabled(ftQueue queue, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            var actionJournals = new List<ftActionJournal>
            {
                new ftActionJournal
                {
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueId = queueItem.ftQueueId,
                    ftQueueItemId = queueItem.ftQueueItemId,
                    Moment = DateTime.UtcNow,
                    Message = $"QueueId {queueItem.ftQueueId} has been disabled."
                }
            };
            receiptResponse.ftState += ftStatesFlags.SECURITY_MECHAMISN_DEACTIVATED;
            receiptResponse.ftReceiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
            return (receiptResponse, actionJournals);
        }

        public async Task<string> GetFtCashBoxIdentificationAsync(ftQueue queue) => (await _configurationRepository.GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false)).CashBoxIdentification;
        public Task FinalTaskAsync(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request, IMiddlewareActionJournalRepository actionJournalRepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareReceiptJournalRepository receiptJournalRepositor) { return Task.CompletedTask; }
        public Task FirstTaskAsync() { return Task.CompletedTask; }
    }
}
