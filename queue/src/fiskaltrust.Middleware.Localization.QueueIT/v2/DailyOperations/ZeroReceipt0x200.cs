using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Extensions;
using System.Linq;
using Newtonsoft.Json;
using System;
using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2.DailyOperations
{
    public class ZeroReceipt0x200 : IReceiptTypeProcessor
    {
        private readonly IITSSCDProvider _itSSCDProvider;
        private readonly ILogger<ZeroReceipt0x200> _logger;
        private readonly IConfigurationRepository _configurationRepository;

        public ITReceiptCases ReceiptCase => ITReceiptCases.ZeroReceipt0x200;

        public ZeroReceipt0x200(IITSSCDProvider itSSCDProvider, ILogger<ZeroReceipt0x200> logger, IConfigurationRepository configurationRepository)
        {
            _itSSCDProvider = itSSCDProvider;
            _logger = logger;
            _configurationRepository = configurationRepository;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIT, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            var signingAvailable = await _itSSCDProvider.IsSSCDAvailable().ConfigureAwait(false);
            if (queueIT.SSCDFailCount == 0)
            {
                var log = "Queue has no failed receipts.";
                if (!signingAvailable)
                {
                    receiptResponse.ftState |= 2;
                    log = $"Signing not available. {log}";
                }
                else
                {
                    log = $"Signing available. {log}";
                }
                _logger.LogInformation(log);
                receiptResponse.SetFtStateData(new StateDetail() { FailedReceiptCount = queueIT.SSCDFailCount, FailMoment = queueIT.SSCDFailMoment, SigningDeviceAvailable = signingAvailable });
                return (receiptResponse, new List<ftActionJournal>());
            }
            try
            {

                var establishConnection = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
                {
                    ReceiptRequest = request,
                    ReceiptResponse = receiptResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to re-establish connection to SCU.");
                receiptResponse.SetFtStateData(new StateDetail() { FailedReceiptCount = queueIT.SSCDFailCount, FailMoment = queueIT.SSCDFailMoment, SigningDeviceAvailable = false });
                return (receiptResponse, new List<ftActionJournal>());
            }

            var sentReceipts = new List<string>();
            var signatures = new List<SignaturItem>();

            var succeeded = true;
            // TBD resend receipts
            //if (_resendFailedReceipts)
            //{
            //    succeeded = await ResendFailedReceiptsAsync(queueIT, queue, sentReceipts, signatures).ConfigureAwait(false);
            //}

            var resent = $"Resent {sentReceipts.Count()} receipts that have been stored between {queueIT.SSCDFailMoment:G} and {DateTime.UtcNow:G}.";

            if (succeeded && signingAvailable)
            {
                _logger.LogInformation($"Successfully closed failed-mode. {resent} ");
                queueIT.SSCDFailCount = 0;
                queueIT.SSCDFailMoment = null;
                queueIT.SSCDFailQueueItemId = null;
            }
            else
            {
                receiptResponse.ftState |= 2;
            }
            receiptResponse.ftStateData = JsonConvert.SerializeObject(new { SentReceipts = sentReceipts });

            _logger.LogInformation($"{sentReceipts.Count()} receipts from the timeframe between {queueIT.SSCDFailMoment:G} and {DateTime.UtcNow:G} have been re-processed at the fiscalization service.");

            var stateDetail = JsonConvert.SerializeObject(new StateDetail() { FailedReceiptCount = queueIT.SSCDFailCount, FailMoment = queueIT.SSCDFailMoment, SigningDeviceAvailable = signingAvailable });

            receiptResponse.ftSignatures = signatures.ToArray();
            await _configurationRepository.InsertOrUpdateQueueITAsync(queueIT).ConfigureAwait(false);

            return (receiptResponse, new List<ftActionJournal>
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
                            Type = $"{ Cases.BASE_STATE | 2:X}",
                            DataJson =  JsonConvert.SerializeObject(new { SentReceipts = sentReceipts })
                        }
                    });
        }

        //private async Task<bool> ResendFailedReceiptsAsync(ICountrySpecificQueue iQueue, ftQueue queue, List<string> sentReceipts, List<SignaturItem> signatures)
        //{
        //    var failedQueueItem = await _queueItemRepository.GetAsync(iQueue.SSCDFailQueueItemId.Value).ConfigureAwait(false);
        //    var queueItemsAfterFailure = _queueItemRepository.GetQueueItemsAfterQueueItem(failedQueueItem);
        //    await foreach (var failqueueItem in queueItemsAfterFailure.ConfigureAwait(false))
        //    {
        //        var failRequest = JsonConvert.DeserializeObject<ReceiptRequest>(failqueueItem.request);
        //        if ((failRequest.ftReceiptCase & 0xFFFF) == 0x0002)
        //        {
        //            continue;
        //        }
        //        var command = _requestCommandFactory.Create(failRequest);
        //        if (await command.ReceiptNeedsReprocessing(queue, failRequest, failqueueItem).ConfigureAwait(false))
        //        {
        //            try
        //            {
        //                var requestCommandResponse = await command.ExecuteAsync(queue, failRequest, failqueueItem, true).ConfigureAwait(false);
        //                signatures.AddRange(requestCommandResponse.ReceiptResponse.ftSignatures);
        //                sentReceipts.Add(failqueueItem.cbReceiptReference);
        //                if (requestCommandResponse.ActionJournals != null)
        //                {
        //                    foreach (var journal in requestCommandResponse.ActionJournals)
        //                    {
        //                        await _actionJournalRepository.InsertAsync(journal).ConfigureAwait(false);
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                if (ex is SSCDErrorException exception && !(exception.Type == SSCDErrorType.Device))
        //                {
        //                    _logger.LogError(ex, "Error on Reprocessing");
        //                    if (iQueue.SSCDFailQueueItemId != failqueueItem.ftQueueItemId)
        //                    {
        //                        iQueue.SSCDFailQueueItemId = failqueueItem.ftQueueItemId;
        //                        iQueue.SSCDFailMoment = DateTime.UtcNow;
        //                    }
        //                    return false;
        //                }
        //                _logger.LogError(ex, $"The receipt {failRequest.cbReceiptReference} could not be proccessed! \n {ex.Message}");
        //            }
        //        }
        //        iQueue.SSCDFailCount--;
        //    }
        //    return true;
        //}

    }
}
