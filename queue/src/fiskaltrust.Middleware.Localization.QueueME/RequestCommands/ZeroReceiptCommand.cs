using System;
using System.Threading.Tasks;

using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Contracts.Repositories;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands.Factories;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class ZeroReceiptCommand : RequestCommand
    {
        protected readonly IRequestCommandFactory _requestCommandFactory;

        public ZeroReceiptCommand(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository, IMiddlewareJournalMERepository journalMERepository, 
            IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareActionJournalRepository actionJournalRepository,IRequestCommandFactory requestCommandFactory) :
            base(logger, configurationRepository, journalMERepository, queueItemRepository, actionJournalRepository)
        {
            _requestCommandFactory = requestCommandFactory;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueME)
        {
            try
            {
                if(queueME.SSCDFailCount == 0)
                {
                    _logger.LogInformation("Queue has no failed receipts!");
                    var receiptResponse = CreateReceiptResponse(request, queueItem);
                    return new RequestCommandResponse()
                    {
                        ReceiptResponse = receiptResponse
                    };
                }
                var failedQueueItem = await _queueItemRepository.GetAsync(queueME.SSCDFailQueueItemId.Value).ConfigureAwait(false);
                var queueItemsAfterFailure = _queueItemRepository.GetQueueItemsAfterQueueItem(failedQueueItem);
                await foreach (var fqueueItem in queueItemsAfterFailure.ConfigureAwait(false))
                {
                    var frequest = JsonConvert.DeserializeObject<ReceiptRequest>(fqueueItem.request);
                    var command = _requestCommandFactory.Create(frequest);
                    if (await command.ReceiptNeedsReprocessing(queueME, queueItem, request).ConfigureAwait(false))
                    {
                        await command.ExecuteAsync(client, queue, request, queueItem, queueME).ConfigureAwait(false);
                    }
                }


                return await Task.FromResult(new RequestCommandResponse()
                {
                });
            }
            catch (Exception ex) when (ex.GetType().Name == ENDPOINTNOTFOUND)
            {
                _logger.LogDebug(ex, "TSE not reachable.");
                throw;
            }
        }
        public override Task<bool> ReceiptNeedsReprocessing(ftQueueME queueME, ftQueueItem queueItem, ReceiptRequest request) => Task.FromResult(false);
    }
}
