using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.serialization.DE.V0;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class InitialOperationReceiptCommand : Contracts.RequestCommands.InitialOperationReceiptCommand
    {
        public override long CountryBaseState => Constants.Cases.BASE_STATE;

        private readonly SignatureItemFactoryIT _signatureItemFactoryIT;
        private readonly ftQueueIT _queueIt;

        public InitialOperationReceiptCommand(ILogger logger, SignatureItemFactoryIT signatureItemFactoryIT, ftQueueIT queueIt) : base(logger)
        {
            _signatureItemFactoryIT = signatureItemFactoryIT;
            _queueIt = queueIt;
        }

        protected override Task<(ftActionJournal, SignaturItem)> InitializeSCUAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            var signatureItem = _signatureItemFactoryIT.CreateInitialOperationSignature($"Queue-ID: {queue.ftQueueId}");
            var notification = new ActivateQueueSCU
            {
                CashBoxId = Guid.Parse(request.ftCashBoxID),
                QueueId = queueItem.ftQueueId,
                Moment = DateTime.UtcNow,
                SCUId = _queueIt.ftSignaturCreationUnitITId.GetValueOrDefault(),
                IsStartReceipt = true,
                Version = "V0"
            };
            var actionJournal = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-{nameof(ActivateQueueSCU)}",
                queueItem.ftQueueItemId, $"Initial-Operation receipt. Queue-ID: {queue.ftQueueId}", JsonConvert.SerializeObject(notification));

            return Task.FromResult((actionJournal, signatureItem));
        }
    }
}
