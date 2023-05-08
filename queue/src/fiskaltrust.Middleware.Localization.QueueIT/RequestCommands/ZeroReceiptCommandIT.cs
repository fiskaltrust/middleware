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
        private readonly IConfigurationRepository _configurationRepository;

        public ZeroReceiptCommandIT(IConfigurationRepository configurationRepository, MiddlewareConfiguration middlewareConfiguration, IMiddlewareQueueItemRepository queueItemRepository, IRequestCommandFactory requestCommandFactory, ILogger < RequestCommand > logger, IActionJournalRepository actionJournalRepository):
            base(middlewareConfiguration, queueItemRepository, requestCommandFactory, logger, actionJournalRepository)
            {
            _configurationRepository = configurationRepository;
        }

        public override async Task<IQueue> GetIQueue(Guid queueId) => await _configurationRepository.GetQueueITAsync(queueId).ConfigureAwait(false);
        public override async Task SaveIQueue(IQueue iQueue) => await _configurationRepository.InsertOrUpdateQueueITAsync((ftQueueIT) iQueue).ConfigureAwait(false);

    }
}
