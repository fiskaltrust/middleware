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
using System.Linq;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class ZeroReceiptCommand : RequestCommand
    {
        protected readonly IRequestCommandFactory _requestCommandFactory;

        public ZeroReceiptCommand(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository, IMiddlewareJournalMERepository journalMERepository,
            IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareActionJournalRepository actionJournalRepository, IRequestCommandFactory requestCommandFactory, QueueMEConfiguration queueMeConfiguration) :
            base(logger, configurationRepository, journalMERepository, queueItemRepository, actionJournalRepository, queueMeConfiguration)
        {
            _requestCommandFactory = requestCommandFactory;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueME, bool subsequent = false)
        {
            try
            {
                if (queueME.SSCDFailCount == 0)
                {
                    Logger.LogInformation("Queue has no failed receipts.");
                    return new RequestCommandResponse()
                    {
                        ReceiptResponse = CreateReceiptResponse(queue, request, queueItem)
                    };
                }
                var failedQueueItem = await QueueItemRepository.GetAsync(queueME.SSCDFailQueueItemId.Value).ConfigureAwait(false);
                var queueItemsAfterFailure = QueueItemRepository.GetQueueItemsAfterQueueItem(failedQueueItem);
                await foreach (var fqueueItem in queueItemsAfterFailure.ConfigureAwait(false))
                {
                    var frequest = JsonConvert.DeserializeObject<ReceiptRequest>(fqueueItem.request);
                    var command = _requestCommandFactory.Create(frequest);
                    if (await command.ReceiptNeedsReprocessing(queueME, fqueueItem, frequest).ConfigureAwait(false))
                    {
                        try
                        {
                            var requestCommandResponse = await command.ExecuteAsync(client, queue, frequest, fqueueItem, queueME,true).ConfigureAwait(false);
                            if (requestCommandResponse.ActionJournals != null)
                            {
                                foreach (var journal in requestCommandResponse.ActionJournals)
                                {
                                    await ActionJournalRepository.InsertAsync(journal).ConfigureAwait(false);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Request could not be resolved: " + fqueueItem.request);
                        }
                    }
                }
                Logger.LogInformation($"Successfully closed failed-mode, re-sent {queueME.SSCDFailCount} receipts that have been stored between {queueME.SSCDFailMoment:G} and {DateTime.UtcNow:G}.");


                var caption = $"Restored connection to fiscalization service at {DateTime.UtcNow:G}.";
                var data = $"{queueME.SSCDFailCount} receipts from the timeframe between {queueME.SSCDFailMoment:G} and {DateTime.UtcNow:G} have been re-processed at the fiscalization service.";

                var receiptResponse = CreateReceiptResponse(queue, request, queueItem);
                receiptResponse.ftSignatures = receiptResponse.ftSignatures.Concat(new List<SignaturItem>
                {
                    new()
                    {
                        ftSignatureType = 0x4D45_0000_0000_0002,
                        ftSignatureFormat = (long) ifPOS.v0.SignaturItem.Formats.Text,
                        Caption = caption,
                        Data = data
                    }
                }).ToArray();

                queueME.SSCDFailCount = 0;
                queueME.SSCDFailMoment = null;
                queueME.SSCDFailQueueItemId = null;
                await ConfigurationRepository.InsertOrUpdateQueueMEAsync(queueME).ConfigureAwait(false);

                return await Task.FromResult(new RequestCommandResponse
                {
                    ReceiptResponse = receiptResponse,
                    ActionJournals = new List<ftActionJournal>
                    {
                        new ftActionJournal
                        {
                            ftActionJournalId = Guid.NewGuid(),
                            ftQueueId = queueItem.ftQueueId,
                            ftQueueItemId = queueItem.ftQueueItemId,
                            Moment = DateTime.UtcNow,
                            Priority = -1,
                            TimeStamp = 0,
                            Message = caption + data,
                            Type = $"{0x4D45_0000_0000_0002:X}-{nameof(String)}",
                            DataJson = JsonConvert.SerializeObject(caption + " " + data)
                        }
                    }
                });
            }
            catch (EntryPointNotFoundException ex)
            {
                Logger.LogDebug(ex, "TSE not reachable.");
                throw;
            }
        }
        public override Task<bool> ReceiptNeedsReprocessing(ftQueueME queueME, ftQueueItem queueItem, ReceiptRequest request) => Task.FromResult(false);
    }
}
