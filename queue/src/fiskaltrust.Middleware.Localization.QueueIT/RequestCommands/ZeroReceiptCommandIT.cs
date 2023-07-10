using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.RequestCommands.Factories;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Interfaces;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class ZeroReceiptCommandIT : ZeroReceiptCommand
    {
        public ZeroReceiptCommandIT(ISigningDevice signingDevice, ICountrySpecificSettings countryspecificSettings, IMiddlewareQueueItemRepository queueItemRepository, IRequestCommandFactory requestCommandFactory, ILogger<RequestCommand> logger, IActionJournalRepository actionJournalRepository) :
            base(signingDevice, countryspecificSettings, queueItemRepository, requestCommandFactory, logger, actionJournalRepository)
        {
        }
    }
}
