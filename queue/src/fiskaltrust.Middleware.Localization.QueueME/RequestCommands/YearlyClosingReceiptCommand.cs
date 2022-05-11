using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class YearlyClosingReceiptCommand : RequestCommand
    {
        public YearlyClosingReceiptCommand(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository,
            IMiddlewareJournalMERepository journalMERepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareActionJournalRepository actionJournalRepository) :
            base(logger, configurationRepository, journalMERepository, queueItemRepository, actionJournalRepository)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueME)
        {
            try
            {
                return await CreateClosing(queue, request, queueItem).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex.GetType().Name == RETRYPOLICYEXCEPTION_NAME)
            {
                _logger.LogDebug(ex, "TSE not reachable.");
                throw;
            }
        }
        public override async Task<bool> ReceiptNeedsReprocessing(ftQueueME queueME, ftQueueItem queueItem, ReceiptRequest request)
        {
            return await ActionJournalExists(queueItem, request.ftReceiptCase).ConfigureAwait(false) == false;
        }
    }
}
