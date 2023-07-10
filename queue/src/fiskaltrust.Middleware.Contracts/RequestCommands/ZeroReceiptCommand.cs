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
using fiskaltrust.ifPOS.v1.errors;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Contracts.Interfaces;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public abstract class ZeroReceiptCommand : RequestCommand
    {
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;
        private readonly bool _resendFailedReceipts;
        private readonly long _countryBaseState;
        private readonly IRequestCommandFactory _requestCommandFactory;
        private readonly IActionJournalRepository _actionJournalRepository;
        private readonly IMiddlewareQueueItemRepository _queueItemRepository;
        private readonly ILogger<RequestCommand> _logger;
        private readonly ISigningDevice _signingDevice;

        public ZeroReceiptCommand(ISigningDevice signingDevice, ICountrySpecificSettings countryspecificSettings,IMiddlewareQueueItemRepository queueItemRepository, IRequestCommandFactory requestCommandFactory, ILogger<RequestCommand> logger, IActionJournalRepository actionJournalRepository)
        {
            _requestCommandFactory = requestCommandFactory;
            _logger = logger;
            _queueItemRepository = queueItemRepository;
            _actionJournalRepository = actionJournalRepository;
            _countrySpecificQueueRepository = countryspecificSettings.CountrySpecificQueueRepository;
            _resendFailedReceipts = countryspecificSettings.ResendFailedReceipts;
            _countryBaseState = countryspecificSettings.CountryBaseState;
            _signingDevice = signingDevice;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, bool isBeingResent = false)
        {
            var iQueue = await _countrySpecificQueueRepository.GetQueueAsync(queue.ftQueueId).ConfigureAwait(false);
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, iQueue.CashBoxIdentification, _countryBaseState);
            var signingAvail = await _signingDevice.IsSigningDeviceAvailable().ConfigureAwait(false);
            if (iQueue.SSCDFailCount == 0)
            {
                var log = "Queue has no failed receipts.";
                if (!signingAvail)
                {
                    receiptResponse.ftState = _countryBaseState | 2;
                    log = $"Signing not available. {log}";
                }
                else
                {
                    log = $"Signing available. {log}";
                }
                _logger.LogInformation(log);
                receiptResponse.SetFtStateData(new StateDetail() { FailedReceiptCount = iQueue.SSCDFailCount, FailMoment = iQueue.SSCDFailMoment, SigningDeviceAvailable = signingAvail });
                return new RequestCommandResponse()
                {
                    ReceiptResponse = receiptResponse
                };
            }
            var sentReceipts = new List<string>();
            var signatures = new List<SignaturItem>();

            var succeeded = true;
            if (_resendFailedReceipts)
            {
                succeeded = await ResendFailedReceiptsAsync(iQueue, queue, sentReceipts, signatures).ConfigureAwait(false);
            }

            var resent = $"Resent {sentReceipts.Count()} receipts that have been stored between {iQueue.SSCDFailMoment:G} and {DateTime.UtcNow:G}.";

            if (succeeded && signingAvail)
            {
                _logger.LogInformation($"Successfully closed failed-mode. {resent} ");
                iQueue.SSCDFailCount = 0;
                iQueue.SSCDFailMoment = null;
                iQueue.SSCDFailQueueItemId = null;
            }
            else
            {
                receiptResponse.ftState = _countryBaseState | 2;
            }
            receiptResponse.ftStateData = JsonConvert.SerializeObject(new { SentReceipts = sentReceipts });

            _logger.LogInformation($"{sentReceipts.Count()} receipts from the timeframe between {iQueue.SSCDFailMoment:G} and {DateTime.UtcNow:G} have been re-processed at the fiscalization service.");

            var stateDetail = JsonConvert.SerializeObject(new StateDetail() { FailedReceiptCount = iQueue.SSCDFailCount, FailMoment = iQueue.SSCDFailMoment, SigningDeviceAvailable = signingAvail });

            signatures.Add(new()
            {
                ftSignatureType = _countryBaseState | 2,
                ftSignatureFormat = (long) ifPOS.v0.SignaturItem.Formats.Text,
                Caption = stateDetail,
                Data = JsonConvert.SerializeObject(new { SentReceipts = sentReceipts })
            });
            receiptResponse.ftSignatures = signatures.ToArray();
            await _countrySpecificQueueRepository.InsertOrUpdateQueueAsync(iQueue).ConfigureAwait(false);

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
                            Message = stateDetail,
                            Type = $"{ _countryBaseState | 2:X}",
                            DataJson =  JsonConvert.SerializeObject(new { SentReceipts = sentReceipts })
        }
                    }
            };
        }

        private async Task<bool> ResendFailedReceiptsAsync(ICountrySpecificQueue iQueue, ftQueue queue, List<string> sentReceipts, List<SignaturItem> signatures)
        {
            var failedQueueItem = await _queueItemRepository.GetAsync(iQueue.SSCDFailQueueItemId.Value).ConfigureAwait(false);
            var queueItemsAfterFailure = _queueItemRepository.GetQueueItemsAfterQueueItem(failedQueueItem);
            await foreach (var failqueueItem in queueItemsAfterFailure.ConfigureAwait(false))
            {
                var failRequest = JsonConvert.DeserializeObject<ReceiptRequest>(failqueueItem.request);
                if ((failRequest.ftReceiptCase & 0xFFFF) == 0x0002)
                {
                    continue;
                }
                var command = _requestCommandFactory.Create(failRequest);
                if (await command.ReceiptNeedsReprocessing(queue, failRequest, failqueueItem).ConfigureAwait(false))
                {
                    try
                    {
                        var requestCommandResponse = await command.ExecuteAsync(queue, failRequest, failqueueItem, true).ConfigureAwait(false);
                        signatures.AddRange(requestCommandResponse.Signatures);
                        sentReceipts.Add(failqueueItem.cbReceiptReference);
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
                        if (ex is SSCDErrorException exception && !(exception.Type == SSCDErrorType.Device))
                        {
                            _logger.LogError(ex, "Error on Reprocessing");
                            if (iQueue.SSCDFailQueueItemId != failqueueItem.ftQueueItemId)
                            {
                                iQueue.SSCDFailQueueItemId = failqueueItem.ftQueueItemId;
                                iQueue.SSCDFailMoment = DateTime.UtcNow;
                            }
                            return false;
                        }
                        _logger.LogError(ex, $"The receipt {failRequest.cbReceiptReference} could not be proccessed! \n {ex.Message}");
                    }
                }
                iQueue.SSCDFailCount--;
            }
            return true;
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);
    }
}
