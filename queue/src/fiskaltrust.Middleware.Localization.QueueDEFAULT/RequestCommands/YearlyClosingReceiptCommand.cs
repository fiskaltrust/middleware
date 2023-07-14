using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands
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

        protected override async Task<string> GetCashboxIdentificationAsync(Guid ftQueueId)
        {
            var queueDefault = await _countrySpecificQueueRepository.GetQueueAsync(ftQueueId).ConfigureAwait(false);
            return queueDefault.CashBoxIdentification;
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);
    }
}
