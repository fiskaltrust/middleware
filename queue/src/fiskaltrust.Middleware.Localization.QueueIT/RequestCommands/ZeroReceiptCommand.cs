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
        private readonly ILogger<RequestCommand> _logger;

        public ZeroReceiptCommand(IMiddlewareQueueItemRepository queueItemRepository, IRequestCommandFactory requestCommandFactory, IConfigurationRepository configurationRepository, ILogger<RequestCommand> logger, IActionJournalRepository actionJournalRepository) : base(configurationRepository, logger)
        {
            _requestCommandFactory = requestCommandFactory;
            _configurationRepository = configurationRepository;
            _logger = logger;
            _queueItemRepository = queueItemRepository;
            _actionJournalRepository = actionJournalRepository;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, bool isRebooking = false)
        {
                var queueIt = await _configurationRepository.GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false);
                var receiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIt.CashBoxIdentification);
                if (queueIt.SSCDFailCount == 0)
                {
                    receiptResponse.ftStateData = "Queue has no failed receipts.";
                    _logger.LogInformation(receiptResponse.ftStateData);
                    return new RequestCommandResponse()
                    {
                        ReceiptResponse = receiptResponse
                    };
                }
                
                var failedQueueItem = await _queueItemRepository.GetAsync(queueIt.SSCDFailQueueItemId.Value).ConfigureAwait(false);
                var queueItemsAfterFailure = _queueItemRepository.GetQueueItemsAfterQueueItem(failedQueueItem);
                var sentReceipts = new List<string>();
                var signatures = new List<SignaturItem>();
                await foreach (var fqueueItem in queueItemsAfterFailure.ConfigureAwait(false))
                {
                    var frequest = JsonConvert.DeserializeObject<ReceiptRequest>(fqueueItem.request);
                    var command = _requestCommandFactory.Create(frequest);
                    if (await command.ReceiptNeedsReprocessing(queue, frequest, fqueueItem).ConfigureAwait(false))
                    {
                        try
                        {
                            var requestCommandResponse = await command.ExecuteAsync(queue, frequest, fqueueItem, true).ConfigureAwait(false);
                            signatures.AddRange(requestCommandResponse.Signatures);
                            sentReceipts.Add(fqueueItem.cbReceiptReference);
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
                            if (ex.Message.StartsWith("[ERR-Connection]"))
                            {
                                throw new Exception(ex.Message);
                            }
                            _logger.LogError(ex, $"The receipt {frequest.cbReceiptReference} could not be proccessed!");
                        }
                    }
                }
                receiptResponse.ftStateData = JsonConvert.SerializeObject(new { SentReceipts = sentReceipts });
                _logger.LogInformation($"Successfully closed failed-mode, resent {queueIt.SSCDFailCount} receipts that have been stored between {queueIt.SSCDFailMoment:G} and {DateTime.UtcNow:G}.");

                var caption = $"Restored connection to fiscalization service at {DateTime.UtcNow:G}.";
                var data = $"{queueIt.SSCDFailCount} receipts from the timeframe between {queueIt.SSCDFailMoment:G} and {DateTime.UtcNow:G} have been re-processed at the fiscalization service.";

                signatures.Add(new()
                {
                    ftSignatureType = CountryBaseState & 2,
                    ftSignatureFormat = (long) ifPOS.v0.SignaturItem.Formats.Text,
                    Caption = caption,
                    Data = data
                });
                receiptResponse.ftSignatures = signatures.ToArray();

                queueIt.SSCDFailCount = 0;
                queueIt.SSCDFailMoment = null;
                queueIt.SSCDFailQueueItemId = null;
                await _configurationRepository.InsertOrUpdateQueueITAsync(queueIt).ConfigureAwait(false);

                return new RequestCommandResponse
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
                            Type = $"{ CountryBaseState | 2:X}",
                            DataJson = JsonConvert.SerializeObject(caption + " " + data)
                        }
                    }
                };
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);
    }
}
