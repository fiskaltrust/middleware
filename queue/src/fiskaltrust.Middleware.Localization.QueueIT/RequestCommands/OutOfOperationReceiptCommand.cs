using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.serialization.DE.V0;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class OutOfOperationReceiptCommand : Contracts.RequestCommands.OutOfOperationReceiptCommand
    {
        private readonly SignatureItemFactoryIT _signatureItemFactoryIT;
        private readonly ftQueueIT _queueIt;
        private readonly IReadOnlyConfigurationRepository _configurationRepository;

        public OutOfOperationReceiptCommand(SignatureItemFactoryIT signatureItemFactoryIT, ftQueueIT queueIt, IReadOnlyConfigurationRepository configurationRepository)
        {
            _signatureItemFactoryIT = signatureItemFactoryIT;
            _queueIt = queueIt;
            _configurationRepository = configurationRepository;
        }

        public override long CountryBaseState => Constants.Cases.BASE_STATE;

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);

        protected override Task<(ftActionJournal, SignaturItem)> DeactivateSCUAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            var signatureItem = _signatureItemFactoryIT.CreateOutOfOperationSignature($"Queue-ID: {queue.ftQueueId}");

            var notification = new DeactivateQueueSCU
            {
                CashBoxId = Guid.Parse(request.ftCashBoxID),
                QueueId = queueItem.ftQueueId,
                Moment = DateTime.UtcNow,
                SCUId = _queueIt.ftSignaturCreationUnitITId.GetValueOrDefault(),
                IsStopReceipt = true,
                Version = "V0"
            };

            var actionJournal = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-{nameof(DeactivateQueueSCU)}",
                queueItem.ftQueueItemId, $"Out-of-Operation receipt. Queue-ID: {queue.ftQueueId}", JsonConvert.SerializeObject(notification));

            return Task.FromResult((actionJournal, signatureItem));
        }

        protected override async Task<string> GetCashboxIdentificationAsync(Guid ftQueueId)
        {
            var queueIt = await _configurationRepository.GetQueueITAsync(ftQueueId).ConfigureAwait(false);
            return queueIt.CashBoxIdentification;
        }
    }
}
