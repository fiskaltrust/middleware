using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.Factories;
using fiskaltrust.storage.serialization.DE.V0;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands
{
    public class OutOfOperationReceiptCommand : Contracts.RequestCommands.OutOfOperationReceiptCommand
    {
        protected override long CountryBaseState => _countryBaseState;
        private readonly long _countryBaseState;
        private readonly SignatureItemFactoryDEFAULT _signatureItemFactoryDefault;
        private readonly ftQueueIT _queueIt;
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;

        public OutOfOperationReceiptCommand(SignatureItemFactoryDEFAULT signatureItemFactoryDefault, ftQueueIT queueIt, ICountrySpecificSettings countrySpecificSettings)
        {
            _signatureItemFactoryDefault = signatureItemFactoryDefault;
            _queueIt = queueIt;
            _countrySpecificQueueRepository = countrySpecificSettings.CountrySpecificQueueRepository;
            _countryBaseState = countrySpecificSettings.CountryBaseState;
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);

        protected override Task<(ftActionJournal, SignaturItem)> DeactivateSCUAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            var signatureItem = _signatureItemFactoryDefault.CreateOutOfOperationSignature($"Queue-ID: {queue.ftQueueId}");
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
            var queueIt = await _countrySpecificQueueRepository.GetQueueAsync(ftQueueId).ConfigureAwait(false);
            return queueIt.CashBoxIdentification;
        }
    }
}
