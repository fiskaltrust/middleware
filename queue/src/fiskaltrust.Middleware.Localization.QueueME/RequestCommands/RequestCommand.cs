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
        protected readonly SignatureFactoryME _signatureFactory;
        protected readonly IConfigurationRepository _configurationRepository;
        protected readonly IJournalMERepository _journalMERepository;
        protected readonly IQueueItemRepository _queueItemRepository;
        protected readonly IActionJournalRepository _actionJournalRepository;
        public RequestCommand(ILogger<RequestCommand> logger, SignatureFactoryME signatureFactory, IConfigurationRepository configurationRepository, 
            IJournalMERepository journalMERepository, IQueueItemRepository queueItemRepository, IActionJournalRepository actionJournalRepository)
        {
            _logger = logger;
            _signatureFactory = signatureFactory;
            _configurationRepository = configurationRepository;
            _journalMERepository = journalMERepository;
            _queueItemRepository = queueItemRepository;
            _actionJournalRepository = actionJournalRepository;
        }

        public abstract Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem);

        protected static ReceiptResponse CreateReceiptResponse(ReceiptRequest request, ftQueueItem queueItem)
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
                ftState = 0x44D5000000000000
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
    }
}