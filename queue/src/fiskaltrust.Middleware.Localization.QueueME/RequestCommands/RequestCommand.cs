using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using System;
using fiskaltrust.ifPOS.v1.me;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public abstract class RequestCommand
    {
        protected const string RETRYPOLICYEXCEPTION_NAME = "RetryPolicyException";
        protected readonly ILogger<RequestCommand> _logger;
        protected readonly IConfigurationRepository _configurationRepository;
        protected readonly IJournalMERepository _journalMERepository;
        protected readonly IQueueItemRepository _queueItemRepository;
        protected readonly IActionJournalRepository _actionJournalRepository;
        public RequestCommand(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository, 
            IJournalMERepository journalMERepository, IQueueItemRepository queueItemRepository, IActionJournalRepository actionJournalRepository)
        {
            _logger = logger;
            _configurationRepository = configurationRepository;
            _journalMERepository = journalMERepository;
            _queueItemRepository = queueItemRepository;
            _actionJournalRepository = actionJournalRepository;
        }
        public abstract Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueME);
        protected static ReceiptResponse CreateReceiptResponse(ReceiptRequest request, ftQueueItem queueItem, long state = 0x4D45000000000000)
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
                ftState = state
            };
        }
        protected async Task<ftActionJournal> CreateActionJournal(ftQueue queue, long journalType, ftQueueItem queueItem)
        {
            var actionjounal = new ftActionJournal()
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                Type = journalType.ToString(),
                Moment = DateTime.UtcNow,
            };
            await _actionJournalRepository.InsertAsync(actionjounal).ConfigureAwait(false);
            return actionjounal;
        }
        protected static List<ftActionJournal> CreateClosingActionJournals(ftQueueItem queueItem, ftQueue queue, string message, long type)
        {
            return new List<ftActionJournal>
            {
                new ftActionJournal
                {
                    ftActionJournalId = Guid.NewGuid(),
                    Message = message,
                    Type = $"{type:X}",
                    ftQueueId = queue.ftQueueId,
                    ftQueueItemId = queueItem.ftQueueItemId,
                    Moment = DateTime.UtcNow,
                    TimeStamp = DateTime.UtcNow.Ticks,
                    Priority = -1
                }
            };
        }
        protected async Task<RequestCommandResponse> CreateClosing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            var receiptResponse = CreateReceiptResponse(request, queueItem);
            var actionJournalEntry = await CreateActionJournal(queue, request.ftReceiptCase, queueItem).ConfigureAwait(false);
            return new RequestCommandResponse()
            {
                ReceiptResponse = receiptResponse,
                ActionJournals = new List<ftActionJournal>()
                    {
                        actionJournalEntry
                    }
            };
        }
        protected async Task<RequestCommandResponse> ProcessFailedReceiptRequest(ftQueueItem queueItem, ReceiptRequest request, ftQueueME queueME)
        {
            if (queueME.SSCDFailCount == 0)
            {
                queueME.SSCDFailMoment = DateTime.UtcNow;
                queueME.SSCDFailQueueItemId = queueItem.ftQueueItemId;
            }
            queueME.SSCDFailCount++;
            await _configurationRepository.InsertOrUpdateQueueMEAsync(queueME).ConfigureAwait(false);
            var receiptResponse = CreateReceiptResponse(request, queueItem, 0x4D45000000000002);

            return new RequestCommandResponse()
            {
                ReceiptResponse = receiptResponse
            };
        }
    }
}