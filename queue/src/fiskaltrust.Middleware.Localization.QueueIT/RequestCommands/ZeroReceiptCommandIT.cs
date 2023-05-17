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
        public override bool ResendFailedReceipts => true;

        protected override ICountrySpecificQueueRepository CountrySpecificQueueRepository => _countrySpecificQueueRepository;


        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;

        public ZeroReceiptCommandIT(ICountrySpecificQueueRepository countrySpecificQueueRepository, IMiddlewareQueueItemRepository queueItemRepository, IRequestCommandFactory requestCommandFactory, ILogger<RequestCommand> logger, IActionJournalRepository actionJournalRepository) :
            base(queueItemRepository, requestCommandFactory, logger, actionJournalRepository)
        {
            _countrySpecificQueueRepository = countrySpecificQueueRepository;
        }

    }
}
