using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueES.RequestCommands
{
    public class YearlyClosingReceiptCommand : Contracts.RequestCommands.YearlyClosingReceiptCommand
    {
        private readonly long _countryBaseState;
        protected override long CountryBaseState => _countryBaseState;
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;

        public YearlyClosingReceiptCommand(ICountrySpecificSettings countryspecificSettings)
        {
            _countrySpecificQueueRepository = countryspecificSettings.CountrySpecificQueueRepository;
            _countryBaseState = countryspecificSettings.CountryBaseState;
        }

        protected override async Task<string> GetCashboxIdentificationAsync(Guid ftQueueId) => throw new NotImplementedException();

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => throw new NotImplementedException();
    }
}
