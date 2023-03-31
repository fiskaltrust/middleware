using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.serialization.DE.V0;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class InitialOperationReceiptCommand : Contracts.RequestCommands.InitialOperationReceiptCommand
    {
        public override long CountryBaseState => Constants.Cases.BASE_STATE;

        private readonly IConfigurationRepository _configurationRepository;
        private readonly SignatureItemFactoryIT _signatureItemFactoryIT;

        public InitialOperationReceiptCommand(ILogger<InitialOperationReceiptCommand> logger, IConfigurationRepository configurationRepository, SignatureItemFactoryIT signatureItemFactoryIT) : base(logger, configurationRepository)
        {
            _configurationRepository = configurationRepository;
            _signatureItemFactoryIT = signatureItemFactoryIT;
        }

        protected override async Task<(ftActionJournal, SignaturItem)> InitializeSCUAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            var queueIt = await _configurationRepository.GetQueueITAsync(queue.ftQueueId);
            var signatureItem = _signatureItemFactoryIT.CreateInitialOperationSignature($"Queue-ID: {queue.ftQueueId}");
            var notification = new ActivateQueueSCU
            {
                CashBoxId = Guid.Parse(request.ftCashBoxID),
                QueueId = queueItem.ftQueueId,
                Moment = DateTime.UtcNow,
                SCUId = queueIt.ftSignaturCreationUnitITId.GetValueOrDefault(),
                IsStartReceipt = true,
                Version = "V0",
            };
            var actionJournal = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-{nameof(ActivateQueueSCU)}",
                queueItem.ftQueueItemId, $"Initial-Operation receipt. Queue-ID: {queue.ftQueueId}", JsonConvert.SerializeObject(notification));

            return (actionJournal, signatureItem);
        }
    }
}
