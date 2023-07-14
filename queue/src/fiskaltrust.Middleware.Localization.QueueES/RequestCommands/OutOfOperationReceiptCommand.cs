using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.serialization.DE.V0;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Localization.QueueES.Factories;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueES.RequestCommands
{
    public class OutOfOperationReceiptCommand : Contracts.RequestCommands.OutOfOperationReceiptCommand
    {
        protected override long CountryBaseState => _countryBaseState;
        private readonly long _countryBaseState;
        private readonly SignatureItemFactoryES _signatureItemFactory;
        private readonly ftQueueES _queue;
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;

        public OutOfOperationReceiptCommand(SignatureItemFactoryES signatureItemFactory, ftQueueES queue, ICountrySpecificSettings countrySpecificSettings)
        {
            _signatureItemFactory = signatureItemFactory;
            _queue = queue;
            _countrySpecificQueueRepository = countrySpecificSettings.CountrySpecificQueueRepository;
            _countryBaseState = countrySpecificSettings.CountryBaseState;
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);

        protected override Task<(ftActionJournal, SignaturItem)> DeactivateSCUAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => throw new NotImplementedException();

        protected override async Task<string> GetCashboxIdentificationAsync(Guid ftQueueId) => throw new NotImplementedException();
    }
}
