using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.ifPOS.v1;
using System.Collections.Generic;
using fiskaltrust.storage.serialization.DE.V0;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class InitialOperationReceiptCommand : RequestCommandIT
    {
        private readonly ILogger _logger;
        private readonly SignatureItemFactoryIT _signatureItemFactoryIT;
        public InitialOperationReceiptCommand(IServiceProvider services) {
            _signatureItemFactoryIT = services.GetRequiredService<SignatureItemFactoryIT>();
            _logger = services.GetRequiredService<ILogger>();
        }


        public override Task<RequestCommandResponse> ExecuteAsync(IITSSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueIT queueIt)     
        {
                if (queue.IsNew())
                {                    
                    queue.StartMoment = DateTime.UtcNow;
                    var receiptResponse = CreateReceiptResponse(queue, request, queueItem);
                    receiptResponse.ftSignatures = new SignaturItem[]{ _signatureItemFactoryIT.CreateInitialOperationSignature($"Queue-ID: {queue.ftQueueId}") };
                    
                    var notification = new ActivateQueueSCU
                    {
                        CashBoxId = Guid.Parse(request.ftCashBoxID),
                        QueueId = queueItem.ftQueueId,
                        Moment = DateTime.UtcNow,
                        SCUId = queueIt.ftSignaturCreationUnitITId.GetValueOrDefault(),
                        IsStartReceipt = true,
                        Version = "V0"
                    };

                    var actionJournal = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-{nameof(ActivateQueueSCU)}",
                        queueItem.ftQueueItemId, $"Initial-Operation receipt. Queue-ID: {queue.ftQueueId}", JsonConvert.SerializeObject(notification));

                    return Task.FromResult(new RequestCommandResponse
                    {
                        ReceiptResponse = receiptResponse,
                        ActionJournals = new List<ftActionJournal> { actionJournal }
                    });
                }

                var actionJournalEntry = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}",
                    queueItem.ftQueueItemId, queue.IsDeactivated()
                            ? $"Queue {queue.ftQueueId} is de-activated, initial-operations-receipt can not be executed."
                            : $"Queue {queue.ftQueueId} is already activated, initial-operations-receipt can not be executed.", "");

                _logger.LogInformation(actionJournalEntry.Message);
                return Task.FromResult(new RequestCommandResponse
                {
                    ActionJournals = new List<ftActionJournal> { actionJournalEntry },
                    ReceiptResponse = CreateReceiptResponse(queue, request, queueItem)
                });
        }
 }
}
