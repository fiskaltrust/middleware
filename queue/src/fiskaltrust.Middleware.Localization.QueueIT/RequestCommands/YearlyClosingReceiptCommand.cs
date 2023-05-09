using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class YearlyClosingReceiptCommand : Contracts.RequestCommands.YearlyClosingReceiptCommand
    {
        protected override IQueueRepository IQueueRepository => _iQueueRepository;
        private readonly IQueueRepository _iQueueRepository;

        public override long CountryBaseState => Constants.Cases.BASE_STATE;

        public YearlyClosingReceiptCommand(IQueueRepository iQeueRepository)
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
