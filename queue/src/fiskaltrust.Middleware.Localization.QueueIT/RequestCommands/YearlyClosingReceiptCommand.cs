using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class YearlyClosingReceiptCommand : Contracts.RequestCommands.YearlyClosingReceiptCommand
    {
        protected override ICountrySpecificQueueRepository CountrySpecificQueueRepository => _countrySpecificQueueRepository;
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;

        public override long CountryBaseState => Constants.Cases.BASE_STATE;

        public YearlyClosingReceiptCommand(ICountrySpecificQueueRepository countrySpecificQueueRepository)
        {
            _countrySpecificQueueRepository = countrySpecificQueueRepository;
        }

        protected override async Task<string> GetCashboxIdentificationAsync(Guid ftQueueId)
        {
            var queueIt = await _countrySpecificQueueRepository.GetQueueAsync(ftQueueId).ConfigureAwait(false);
            return queueIt.CashBoxIdentification;
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);
    }
}
