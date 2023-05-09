using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.RequestCommands.Factories;
using fiskaltrust.Middleware.Contracts.Models;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class ZeroReceiptCommandIT : ZeroReceiptCommand
    {
        public override long CountryBaseState => Constants.Cases.BASE_STATE;

        protected override IQueueRepository IQueueRepository => _iQueueRepository;

        private readonly IQueueRepository _iQueueRepository;

        public ZeroReceiptCommandIT(IQueueRepository iqueueRepository, MiddlewareConfiguration middlewareConfiguration, IMiddlewareQueueItemRepository queueItemRepository, IRequestCommandFactory requestCommandFactory, ILogger < RequestCommand > logger, IActionJournalRepository actionJournalRepository):
            base(middlewareConfiguration, queueItemRepository, requestCommandFactory, logger, actionJournalRepository)
            {
            _iQueueRepository = iqueueRepository;
        }

    }
}
