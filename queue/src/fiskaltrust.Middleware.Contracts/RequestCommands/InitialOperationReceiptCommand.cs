using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v1;
using System.Collections.Generic;
using fiskaltrust.Middleware.Contracts.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public abstract class InitialOperationReceiptCommand : RequestCommand
    {
        private readonly ILogger _logger;
        public InitialOperationReceiptCommand(ILogger logger)
        {
            _logger = logger;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            if (queue.IsNew())
            {
                queue.StartMoment = DateTime.UtcNow;
                var receiptResponse = CreateReceiptResponse(queue, request, queueItem);

                var (actionJournal, signature) = await InitializeSCUAsync(queue, request, queueItem);

                receiptResponse.ftSignatures = new SignaturItem[] { signature };
                return new RequestCommandResponse
                {
                    ReceiptResponse = receiptResponse,
                    ActionJournals = new List<ftActionJournal> { actionJournal }
                };
            }

            var actionJournalEntry = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}",
                queueItem.ftQueueItemId, queue.IsDeactivated()
                        ? $"Queue {queue.ftQueueId} is de-activated, initial-operations-receipt can not be executed."
                        : $"Queue {queue.ftQueueId} is already activated, initial-operations-receipt can not be executed.", "");

            _logger.LogInformation(actionJournalEntry.Message);
            return new RequestCommandResponse
            {
                ActionJournals = new List<ftActionJournal> { actionJournalEntry },
                ReceiptResponse = CreateReceiptResponse(queue, request, queueItem)
            };
        }

        protected abstract Task<(ftActionJournal, SignaturItem)> InitializeSCUAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem);
    }
}
