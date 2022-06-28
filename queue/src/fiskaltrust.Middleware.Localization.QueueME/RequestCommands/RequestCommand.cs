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
using fiskaltrust.Middleware.Localization.QueueME.Factories;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public abstract class RequestCommand
    {
        private const string QUEUEINFAILEDMODE = "Queue in failed mode, use Zeroreceipt to process failed requests. SSCDFailCount: {0}";
        protected readonly ILogger<RequestCommand> _logger;
        protected readonly IConfigurationRepository _configurationRepository;
        protected readonly IMiddlewareJournalMERepository _journalMERepository;
        protected readonly IMiddlewareQueueItemRepository _queueItemRepository;
        protected readonly IMiddlewareActionJournalRepository _actionJournalRepository;

        public RequestCommand(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository, IMiddlewareJournalMERepository journalMERepository, IMiddlewareQueueItemRepository queueItemRepository, 
            IMiddlewareActionJournalRepository actionJournalRepository)
        {
            _logger = logger;
            _configurationRepository = configurationRepository;
            _journalMERepository = journalMERepository;
            _queueItemRepository = queueItemRepository;
            _actionJournalRepository = actionJournalRepository;
        }

        public abstract Task<bool> ReceiptNeedsReprocessing(ftQueueME queueME, ftQueueItem queueItem, ReceiptRequest request);
        public abstract Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueME);
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
            var actionjounal = new ftActionJournal()
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                Type = journalType.ToString(),
                Moment = DateTime.UtcNow,
            };
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
            var actionJournalEntry = CreateActionJournal(queue, request.ftReceiptCase, queueItem);
            var requestCommandResponse = new RequestCommandResponse()
            {
                ReceiptResponse = receiptResponse,
                ActionJournals = new List<ftActionJournal>()
                    {
                        actionJournalEntry
                    }
            };
            return await Task.FromResult(requestCommandResponse).ConfigureAwait(false);
        }

        public async Task<RequestCommandResponse> ProcessFailedInvoiceRequest(ftQueueItem queueItem, ReceiptRequest request, ComputeIICResponse computeIICResponse, ftQueueME queueME)
        {
            var requestCommandResponse = await ProcessFailedReceiptRequest(queueItem, request, queueME);
            requestCommandResponse.ReceiptResponse.ftSignatures = requestCommandResponse.ReceiptResponse.ftSignatures.Concat(new SignatureItemFactory(queueItem, request, computeIICResponse, queueME).CreateSignatures()).ToArray();
            return requestCommandResponse;
        }

        public async Task<RequestCommandResponse> ProcessFailedReceiptRequest(ftQueueItem queueItem, ReceiptRequest request, ftQueueME queueME)
        {
            if (queueME.SSCDFailCount == 0)
            {
                queueME.SSCDFailMoment = DateTime.UtcNow;
                queueME.SSCDFailQueueItemId = queueItem.ftQueueItemId;
            }
            queueME.SSCDFailCount++;
            await _configurationRepository.InsertOrUpdateQueueMEAsync(queueME).ConfigureAwait(false);
            var receiptResponse = CreateReceiptResponse(request, queueItem, new string[] { string.Format(QUEUEINFAILEDMODE, queueME.SSCDFailCount)}, 0x4D45000000000002);
            _logger.LogInformation(string.Format(QUEUEINFAILEDMODE, queueME.SSCDFailCount));

            return new RequestCommandResponse()
            {
                ReceiptResponse = receiptResponse
            };
        }

        protected async Task<bool> ActionJournalExists(ftQueueItem queueItem, long type)
        {
            var actionJournal = await _actionJournalRepository.GetByQueueItemId(queueItem.ftQueueItemId).FirstOrDefaultAsync().ConfigureAwait(false);
            if (actionJournal != null && actionJournal.Type == type.ToString())
            {
                return true;
            }
            return false;
        }
    }
}