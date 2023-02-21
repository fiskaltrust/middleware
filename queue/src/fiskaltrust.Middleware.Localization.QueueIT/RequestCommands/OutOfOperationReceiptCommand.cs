using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.serialization.DE.V0;
using Newtonsoft.Json;
using System.Collections.Generic;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.ifPOS.v1.it;
using Microsoft.Extensions.DependencyInjection;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class OutOfOperationReceiptCommand : RequestCommandIT
    {
        private readonly SignatureItemFactoryIT _signatureItemFactoryIT;
        public OutOfOperationReceiptCommand(IServiceProvider services)
        {
            _signatureItemFactoryIT = services.GetRequiredService<SignatureItemFactoryIT>();

        }
        public override Task<RequestCommandResponse> ExecuteAsync(IITSSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueIT queueIt)
       {
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem);
            receiptResponse.ftSignatures = new SignaturItem[] { _signatureItemFactoryIT.CreateOutOfOperationSignature($"Queue-ID: {queue.ftQueueId}") };

            var notification = new DeactivateQueueSCU
            {
                CashBoxId = Guid.Parse(request.ftCashBoxID),
                QueueId = queueItem.ftQueueId,
                Moment = DateTime.UtcNow,
                SCUId = queueIt.ftSignaturCreationUnitITId.GetValueOrDefault(),
                IsStopReceipt = true,
                Version = "V0"
            };

            var actionJournal = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-{nameof(DeactivateQueueSCU)}",
                queueItem.ftQueueItemId, $"Out-of-Operation receipt. Queue-ID: {queue.ftQueueId}", JsonConvert.SerializeObject(notification));

            queue.StopMoment = DateTime.UtcNow;

            return Task.FromResult(new RequestCommandResponse
            {
                ReceiptResponse = receiptResponse,
                ActionJournals = new List<ftActionJournal> { actionJournal }
            });
        }
    }
}
