using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using System;
using fiskaltrust.ifPOS.v1.me;
using System.Collections.Generic;
using fiskaltrust.Middleware.Contracts.Repositories;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public abstract class RequestCommand
    {
        private const long SSCD_FAILED_MODE_FLAG = 0x0000_0000_0000_0002;

        protected readonly ILogger<RequestCommand> Logger;
        protected readonly IConfigurationRepository ConfigurationRepository;
        protected readonly IMiddlewareJournalMERepository JournalMeRepository;
        protected readonly IMiddlewareQueueItemRepository QueueItemRepository;
        protected readonly IMiddlewareActionJournalRepository ActionJournalRepository;
        protected readonly QueueMEConfiguration QueueMeConfiguration;

        protected RequestCommand(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository, IMiddlewareJournalMERepository journalMeRepository, IMiddlewareQueueItemRepository queueItemRepository,
            IMiddlewareActionJournalRepository actionJournalRepository, QueueMEConfiguration queueMeConfiguration)
        {
            Logger = logger;
            ConfigurationRepository = configurationRepository;
            JournalMeRepository = journalMeRepository;
            QueueItemRepository = queueItemRepository;
            ActionJournalRepository = actionJournalRepository;
            QueueMeConfiguration = queueMeConfiguration;
        }

        public abstract Task<bool> ReceiptNeedsReprocessing(ftQueueME queueMe, ftQueueItem queueItem, ReceiptRequest request);

        public abstract Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueMe);

        public static ReceiptResponse CreateReceiptResponse(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ulong? yearlyOrdinalNumber = null)
        {
            var receiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
            if (yearlyOrdinalNumber.HasValue)
            {
                receiptIdentification += yearlyOrdinalNumber.Value;
            }

            return new ReceiptResponse
            {
                ftCashBoxID = request.ftCashBoxID,
                ftQueueID = queueItem.ftQueueId.ToString(),
                ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                ftQueueRow = queueItem.ftQueueRow,
                cbTerminalID = request.cbTerminalID,
                cbReceiptReference = request.cbReceiptReference,
                ftReceiptMoment = DateTime.UtcNow,
                ftState = 0x4D45000000000000,
                ftReceiptIdentification = receiptIdentification
            };
        }

        protected ftActionJournal CreateActionJournal(ftQueue queue, long journalType, ftQueueItem queueItem)
        {
            var actionJournal = new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                Type = journalType.ToString(),
                Moment = DateTime.UtcNow,
            };
            return actionJournal;
        }

        protected async Task<RequestCommandResponse> CreateClosing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem);
            var actionJournalEntry = CreateActionJournal(queue, request.ftReceiptCase, queueItem);
            var requestCommandResponse = new RequestCommandResponse
            {
                ReceiptResponse = receiptResponse,
                ActionJournals = new List<ftActionJournal>
                {
                    actionJournalEntry
                }
            };
            return await Task.FromResult(requestCommandResponse).ConfigureAwait(false);
        }

        public async Task<RequestCommandResponse> ProcessFailedReceiptRequest(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request, ftQueueME queueMe)
        {
            if (queueMe.SSCDFailCount == 0)
            {
                queueMe.SSCDFailMoment = DateTime.UtcNow;
                queueMe.SSCDFailQueueItemId = queueItem.ftQueueItemId;
            }
            queueMe.SSCDFailCount++;
            await ConfigurationRepository.InsertOrUpdateQueueMEAsync(queueMe).ConfigureAwait(false);
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem);
            receiptResponse.ftState += SSCD_FAILED_MODE_FLAG;

            return new RequestCommandResponse
            {
                ReceiptResponse = receiptResponse
            };
        }

        protected async Task<bool> ActionJournalExists(ftQueueItem queueItem, long type)
        {
            var actionJournal = await ActionJournalRepository.GetByQueueItemId(queueItem.ftQueueItemId).FirstOrDefaultAsync().ConfigureAwait(false);
            return actionJournal != null && actionJournal.Type == type.ToString();
        }
    }
}