using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.RequestCommands.Factories;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Constants;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class ZeroReceiptCommandIT : ZeroReceiptCommand
    {
        public ZeroReceiptCommandIT(ICountrySpecificSettings countryspecificSettings, IMiddlewareQueueItemRepository queueItemRepository, IRequestCommandFactory requestCommandFactory, ILogger<RequestCommand> logger, IActionJournalRepository actionJournalRepository) :
            base(countryspecificSettings, queueItemRepository, requestCommandFactory, logger, actionJournalRepository)
        {
        }
    }
}
