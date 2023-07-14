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
        
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;
        private readonly SignatureItemFactoryDEFAULT _signatureItemFactoryDefault;

        public OutOfOperationReceiptCommand(SignatureItemFactoryDEFAULT signatureItemFactoryDefault, ICountrySpecificSettings countrySpecificSettings)
        {
            _signatureItemFactoryDefault = signatureItemFactoryDefault;
            _countrySpecificQueueRepository = countrySpecificSettings.CountrySpecificQueueRepository;
            _countryBaseState = countrySpecificSettings.CountryBaseState;
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);

        protected override Task<(ftActionJournal, SignaturItem)> DeactivateSCUAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            return Task.FromResult((new ftActionJournal {}, new SignaturItem {}));
        }

        protected override async Task<string> GetCashboxIdentificationAsync(Guid ftQueueId)
        {
            var queueDefault = await _countrySpecificQueueRepository.GetQueueAsync(ftQueueId).ConfigureAwait(false);
            return queueDefault.CashBoxIdentification;
        }
    }
}
