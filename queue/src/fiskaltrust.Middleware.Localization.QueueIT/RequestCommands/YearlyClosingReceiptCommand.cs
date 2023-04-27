using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class YearlyClosingReceiptCommand : Contracts.RequestCommands.YearlyClosingReceiptCommand
    {
        private readonly IReadOnlyConfigurationRepository _configurationRepository;

        public override long CountryBaseState => Constants.Cases.BASE_STATE;

        public YearlyClosingReceiptCommand(IReadOnlyConfigurationRepository configurationRepository)
        {
            _configurationRepository = configurationRepository;
        }

        protected override async Task<string> GetCashboxIdentificationAsync(Guid ftQueueId)
        {
            var queueIt = await _configurationRepository.GetQueueITAsync(ftQueueId).ConfigureAwait(false);
            return queueIt.CashBoxIdentification;
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);
    }
}
