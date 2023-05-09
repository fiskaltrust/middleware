using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.RequestCommands.Factories;
using fiskaltrust.Middleware.Contracts.Models;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public abstract class ZeroReceiptCommand : RequestCommand
    {
        private readonly MiddlewareConfiguration _middlewareConfiguration;
        private readonly IRequestCommandFactory _requestCommandFactory;
        private readonly IActionJournalRepository _actionJournalRepository;
        private readonly IMiddlewareQueueItemRepository _queueItemRepository;
        private readonly ILogger<RequestCommand> _logger;

        public ZeroReceiptCommand(MiddlewareConfiguration middlewareConfiguration, IMiddlewareQueueItemRepository queueItemRepository, IRequestCommandFactory requestCommandFactory, ILogger<RequestCommand> logger, IActionJournalRepository actionJournalRepository)
        {
            _middlewareConfiguration = middlewareConfiguration;
            _requestCommandFactory = requestCommandFactory;
            _logger = logger;
            _queueItemRepository = queueItemRepository;
            _actionJournalRepository = actionJournalRepository;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, bool isResend = false)
        {
            var iQueue = await IQueueRepository.GetQueueAsync(queue.ftQueueId).ConfigureAwait(false);
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, iQueue.CashBoxIdentification);
            if (iQueue.SSCDFailCount == 0)
            {
                receiptResponse.ftStateData = "Queue has no failed receipts.";
                _logger.LogInformation(receiptResponse.ftStateData);
                return new RequestCommandResponse()
                {
                    ReceiptResponse = receiptResponse
                };
            }
            var sentReceipts = new List<string>();
            var signatures = new List<SignaturItem>();

            if (_middlewareConfiguration.ResendFailedReceipts)
            {
                await ResendFailedReceipts(iQueue, queue, sentReceipts, signatures).ConfigureAwait(false);
            }
            receiptResponse.ftStateData = JsonConvert.SerializeObject(new { SentReceipts = sentReceipts });
            _logger.LogInformation($"Successfully closed failed-mode, resent {sentReceipts.Count()} receipts that have been stored between {iQueue.SSCDFailMoment:G} and {DateTime.UtcNow:G}.");

            var caption = $"Restored connection to fiscalization service at {DateTime.UtcNow:G}.";
            var data = $"{iQueue.SSCDFailCount} receipts from the timeframe between {iQueue.SSCDFailMoment:G} and {DateTime.UtcNow:G} have been re-processed at the fiscalization service.";

            signatures.Add(new()
            {
                ftSignatureType = CountryBaseState & 2,
                ftSignatureFormat = (long) ifPOS.v0.SignaturItem.Formats.Text,
                Caption = caption,
                Data = data
            });
            receiptResponse.ftSignatures = signatures.ToArray();

            iQueue.SSCDFailCount = 0;
            iQueue.SSCDFailMoment = null;
            iQueue.SSCDFailQueueItemId = null;
            await IQueueRepository.InsertOrUpdateQueueAsync(iQueue).ConfigureAwait(false);

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

        private async Task ResendFailedReceipts(IQueue iQueue, ftQueue queue, List<string> sentReceipts, List<SignaturItem> signatures)
        {
            var failedQueueItem = await _queueItemRepository.GetAsync(iQueue.SSCDFailQueueItemId.Value).ConfigureAwait(false);
            var queueItemsAfterFailure = _queueItemRepository.GetQueueItemsAfterQueueItem(failedQueueItem);
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
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);
    }
}
