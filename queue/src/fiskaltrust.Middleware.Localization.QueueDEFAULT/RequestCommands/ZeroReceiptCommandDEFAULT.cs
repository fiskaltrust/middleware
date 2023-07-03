using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.Middleware.Contracts.RequestCommands.Factories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands
{
    public class ZeroReceiptCommandDEFAULT : ZeroReceiptCommand
    {
        public ZeroReceiptCommandDEFAULT(ICountrySpecificSettings countryspecificSettings, IMiddlewareQueueItemRepository queueItemRepository, IRequestCommandFactory requestCommandFactory, ILogger<RequestCommand> logger, IActionJournalRepository actionJournalRepository) :
            base(countryspecificSettings, queueItemRepository, requestCommandFactory, logger, actionJournalRepository)
        {
        }
    }
}
