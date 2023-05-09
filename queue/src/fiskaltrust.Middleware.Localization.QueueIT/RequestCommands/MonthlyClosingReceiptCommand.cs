using System.Threading.Tasks;
using System;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class MonthlyClosingReceiptCommand : Contracts.RequestCommands.MonthlyClosingReceiptCommand
    {
        protected override IQueueRepository IQueueRepository => _iQueueRepository;
        private readonly IQueueRepository _iQueueRepository;
        public override long CountryBaseState => Constants.Cases.BASE_STATE;

        public MonthlyClosingReceiptCommand(IQueueRepository iQeueRepository)
        {
            _iQueueRepository = iQeueRepository;
        }

        protected override async Task<string> GetCashboxIdentificationAsync(Guid ftQueueId)
        {
            var queueIt = await _iQueueRepository.GetQueueAsync(ftQueueId).ConfigureAwait(false);
            return queueIt.CashBoxIdentification;
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);
  }
}
