using System.Threading.Tasks;
using System;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.RequestCommands;

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

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);
        public override Task<RequestCommandResponse> ProcessFailedReceiptRequest(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request) => throw new NotImplementedException();
    }
}
