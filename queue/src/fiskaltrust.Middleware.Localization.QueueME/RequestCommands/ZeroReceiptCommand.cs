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
        private const string QUEUECONNECTED = "All Receipts have been sent! Queue is in connected mode!";
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
                    return new RequestCommandResponse()
                    {
                        ReceiptResponse = CreateReceiptResponse(request, queueItem)
                    };
                }
                var failedQueueItem = await _queueItemRepository.GetAsync(queueME.SSCDFailQueueItemId.Value).ConfigureAwait(false);
                var queueItemsAfterFailure = _queueItemRepository.GetQueueItemsAfterQueueItem(failedQueueItem);
                await foreach (var fqueueItem in queueItemsAfterFailure.ConfigureAwait(false))
                {
                    var frequest = JsonConvert.DeserializeObject<ReceiptRequest>(fqueueItem.request);
                    var command = _requestCommandFactory.Create(frequest);
                    if (await command.ReceiptNeedsReprocessing(queueME, fqueueItem, frequest).ConfigureAwait(false))
                    {
                        try
                        {
                            var requestCommandResponse = await command.ExecuteAsync(client, queue, frequest, fqueueItem, queueME).ConfigureAwait(false);
                            if (requestCommandResponse.ActionJournals != null)
                            {
                                foreach (var journal in requestCommandResponse.ActionJournals)
                                {
                                    await _actionJournalRepository.InsertAsync(journal).ConfigureAwait(false);
                                }
                            }
                        }catch (Exception ex)
                        {
                            _logger.LogError(ex, "Request could not be resolved : " + fqueueItem.request);
                        }
                    }
                }
                queueME.SSCDFailCount = 0;
                queueME.SSCDFailMoment = null;
                queueME.SSCDFailQueueItemId = null;
                await _configurationRepository.InsertOrUpdateQueueMEAsync(queueME).ConfigureAwait(false);
                _logger.LogInformation(QUEUECONNECTED);
                var receiptResponse = new ReceiptResponse() { ftReceiptHeader = new string[] { QUEUECONNECTED } };
                return await Task.FromResult(new RequestCommandResponse()
                {
                    ReceiptResponse = receiptResponse
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
