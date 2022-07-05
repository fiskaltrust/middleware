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
using fiskaltrust.Middleware.Contracts.Constants;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public abstract class RequestCommand
    {
        private const string QueueInFailedMode = "Queue in failed mode, use Zeroreceipt to process failed requests. SSCDFailCount: {0}";
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
        public static ReceiptResponse CreateReceiptResponse(ReceiptRequest request, ftQueueItem queueItem,string[] receiptHeader = null, long state = 0x4D45000000000000)
        {
            return new ReceiptResponse
            {
                ftCashBoxID = request.ftCashBoxID,
                ftQueueID = queueItem.ftQueueId.ToString(),
                ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                ftQueueRow = queueItem.ftQueueRow,
                cbTerminalID = request.cbTerminalID,
                cbReceiptReference = request.cbReceiptReference,
                ftReceiptMoment = DateTime.UtcNow,
                ftReceiptHeader = receiptHeader,
                ftState = state,
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
            var receiptResponse = CreateReceiptResponse(request, queueItem);
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

        public async Task<RequestCommandResponse> ProcessFailedReceiptRequest(ftQueueItem queueItem, ReceiptRequest request, ftQueueME queueMe)
        {
            if (queueMe.SSCDFailCount == 0)
            {
                queueMe.SSCDFailMoment = DateTime.UtcNow;
                queueMe.SSCDFailQueueItemId = queueItem.ftQueueItemId;
            }
            queueMe.SSCDFailCount++;
            await ConfigurationRepository.InsertOrUpdateQueueMEAsync(queueMe).ConfigureAwait(false);
            var receiptResponse = CreateReceiptResponse(request, queueItem,
                receiptHeader: new[] { string.Format(QueueInFailedMode, queueMe.SSCDFailCount) }, 0x4D45000000000002);
            Logger.LogInformation(string.Format(QueueInFailedMode, queueMe.SSCDFailCount));

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