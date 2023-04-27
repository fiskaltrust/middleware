using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.Factories;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class ZeroReceiptCommand : RequestCommandIT
    {
        private readonly IRequestCommandFactory _requestCommandFactory;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IActionJournalRepository _actionJournalRepository;
        private readonly IMiddlewareQueueItemRepository _queueItemRepository;
        private readonly ILogger _logger;

        public ZeroReceiptCommand(IMiddlewareQueueItemRepository queueItemRepository, IRequestCommandFactory requestCommandFactory, IConfigurationRepository configurationRepository, ILogger logger, IActionJournalRepository actionJournalRepository) : base(configurationRepository, logger)
        {
            _requestCommandFactory = requestCommandFactory;
            _configurationRepository = configurationRepository;
            _logger = logger;
            _queueItemRepository = queueItemRepository;
            _actionJournalRepository = actionJournalRepository;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            try
            {
                var queueIt = await _configurationRepository.GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false);
                if (queueIt.SSCDFailCount == 0)
                {
                    _logger.LogInformation("Queue has no failed receipts.");
                    return new RequestCommandResponse()
                    {
                        ReceiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIt.CashBoxIdentification)
                    };
                }
                var failedQueueItem = await _queueItemRepository.GetAsync(queueIt.SSCDFailQueueItemId.Value).ConfigureAwait(false);
                var queueItemsAfterFailure = _queueItemRepository.GetQueueItemsAfterQueueItem(failedQueueItem);
                await foreach (var fqueueItem in queueItemsAfterFailure.ConfigureAwait(false))
                {
                    var frequest = JsonConvert.DeserializeObject<ReceiptRequest>(fqueueItem.request);
                    var command = _requestCommandFactory.Create(frequest, queueIt);
                    if (await command.ReceiptNeedsReprocessing(queue, request, queueItem).ConfigureAwait(false))
                    {
                        try
                        {
                            var requestCommandResponse = await command.ExecuteAsync(queue, request, queueItem).ConfigureAwait(false);
                            if (requestCommandResponse.ActionJournals != null)
                            {
                                foreach (var journal in requestCommandResponse.ActionJournals)
                                {
                                    await _actionJournalRepository.InsertAsync(journal).ConfigureAwait(false);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"The receipt {frequest.cbReceiptReference} could not be proccessed!");
                        }
                    }
                }
                _logger.LogInformation($"Successfully closed failed-mode, re-sent {queueIt.SSCDFailCount} receipts that have been stored between {queueIt.SSCDFailMoment:G} and {DateTime.UtcNow:G}.");

                var caption = $"Restored connection to fiscalization service at {DateTime.UtcNow:G}.";
                var data = $"{queueIt.SSCDFailCount} receipts from the timeframe between {queueIt.SSCDFailMoment:G} and {DateTime.UtcNow:G} have been re-processed at the fiscalization service.";
                var receiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIt.CashBoxIdentification);
                receiptResponse.ftSignatures = receiptResponse.ftSignatures.Concat(new List<SignaturItem>
                {
                    new()
                    {
                        ftSignatureType = CountryBaseState & 2,
                        ftSignatureFormat = (long) ifPOS.v0.SignaturItem.Formats.Text,
                        Caption = caption,
                        Data = data
                    }
                }).ToArray();

                queueIt.SSCDFailCount = 0;
                queueIt.SSCDFailMoment = null;
                queueIt.SSCDFailQueueItemId = null;
                await _configurationRepository.InsertOrUpdateQueueITAsync(queueIt).ConfigureAwait(false);

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
                _logger.LogDebug(ex, "TSE not reachable.");
                throw;
            }
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);
    }
}
