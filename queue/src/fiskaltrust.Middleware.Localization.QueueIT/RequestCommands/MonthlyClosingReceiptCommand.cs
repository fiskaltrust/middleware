using System.Threading.Tasks;
using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class MonthlyClosingReceiptCommand : Contracts.RequestCommands.MonthlyClosingReceiptCommand
    {
        private readonly IReadOnlyConfigurationRepository _configurationRepository;

        public override long CountryBaseState => Constants.Cases.BASE_STATE;

        public MonthlyClosingReceiptCommand(IReadOnlyConfigurationRepository configurationRepository)
        {
            _configurationRepository = configurationRepository;
        }

        protected override async Task<string> GetCashboxIdentificationAsync(Guid ftQueueId)
        {
            var queueIt = await _configurationRepository.GetQueueITAsync(ftQueueId).ConfigureAwait(false);
            return queueIt.CashBoxIdentification;
        }
    }
}
