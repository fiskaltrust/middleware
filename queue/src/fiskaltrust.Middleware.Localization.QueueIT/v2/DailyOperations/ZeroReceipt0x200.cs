using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2.DailyOperations
{
    public class ZeroReceipt0x200 : IReceiptTypeProcessor
    {
        private readonly IITSSCDProvider _itSSCDProvider;
        private readonly ILogger<ZeroReceipt0x200> _logger;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IMiddlewareQueueItemRepository _middlewareQueueItemRepository;

        public ITReceiptCases ReceiptCase => ITReceiptCases.ZeroReceipt0x200;

        public ZeroReceipt0x200(IITSSCDProvider itSSCDProvider, ILogger<ZeroReceipt0x200> logger, IConfigurationRepository configurationRepository, IMiddlewareQueueItemRepository middlewareQueueItemRepository)
        {
            _itSSCDProvider = itSSCDProvider;
            _logger = logger;
            _configurationRepository = configurationRepository;
            _middlewareQueueItemRepository = middlewareQueueItemRepository;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIT, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            var signingAvailable = await _itSSCDProvider.IsSSCDAvailable().ConfigureAwait(false);
            if (queueIT.SSCDFailCount == 0)
            {
                var log = "Queue has no failed receipts.";
                if (!signingAvailable)
                {
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


            var succeeded = true;
            if (succeeded && signingAvailable)
            {
                _logger.LogInformation($"Successfully closed failed-mode.");
                queueIT.SSCDFailCount = 0;
                queueIT.SSCDFailMoment = null;
                queueIT.SSCDFailQueueItemId = null;
            }
            else
            {
                receiptResponse.ftState |= 2;
            }


            var fromQueueItem = await _middlewareQueueItemRepository.GetAsync(queueIT.SSCDFailQueueItemId.Value);
            var fromResponse = JsonConvert.DeserializeObject<ReceiptResponse>(fromQueueItem.response);
            var fromReceipt = fromResponse.ftReceiptIdentification;
            receiptResponse.ftSignatures = new List<SignaturItem>().ToArray();
            await _configurationRepository.InsertOrUpdateQueueITAsync(queueIT).ConfigureAwait(false);
            return (receiptResponse, new List<ftActionJournal>
                    {
                        new ftActionJournal
                        {
                            ftActionJournalId = Guid.NewGuid(),
                            ftQueueId = queueItem.ftQueueId,
                            ftQueueItemId = queueItem.ftQueueItemId,
                            Moment = DateTime.UtcNow,
                            Message = $"QueueItem {queueItem.ftQueueItemId} recovered Queue {queueIT.ftQueueITId} from sscd-failed mode. Closing chain of failed receipts from {fromReceipt} to {receiptResponse.ftReceiptIdentification}.",
                            Type = $"{ Cases.BASE_STATE | 2:X}"
                        }
                    });
        }
    }
}
