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

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueIT.v2.DailyOperations
{
    public class ZeroReceipt0x200 : IReceiptTypeProcessor
    {
        private readonly IITSSCD _itSSCD;
        private readonly ILogger<ZeroReceipt0x200> _logger;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IMiddlewareQueueItemRepository _middlewareQueueItemRepository;

        public ITReceiptCases ReceiptCase => ITReceiptCases.ZeroReceipt0x200;

        public ZeroReceipt0x200(IITSSCD itSSCD, ILogger<ZeroReceipt0x200> logger, IConfigurationRepository configurationRepository, IMiddlewareQueueItemRepository middlewareQueueItemRepository)
        {
            _itSSCD = itSSCD;
            _logger = logger;
            _configurationRepository = configurationRepository;
            _middlewareQueueItemRepository = middlewareQueueItemRepository;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIT, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            if (queueIT.SSCDFailCount != 0)
            {
                queueIT.SSCDFailCount = 0;
                queueIT.SSCDFailMoment = null;
                queueIT.SSCDFailQueueItemId = null;
                await _configurationRepository.InsertOrUpdateQueueITAsync(queueIT).ConfigureAwait(false);
            }
            try
            {
                var establishConnection = await _itSSCD.ProcessReceiptAsync(new ProcessRequest
                {
                    ReceiptRequest = request,
                    ReceiptResponse = receiptResponse
                });
                if(establishConnection.ReceiptResponse.ftState == 0x4954_2001_0000_0000)
                {
                    return (establishConnection.ReceiptResponse, new List<ftActionJournal>());
                }
                return (establishConnection.ReceiptResponse, new List<ftActionJournal>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to re-establish connection to SCU.");
                receiptResponse.ftState = 0x4954_2000_EEEE_EEEE;
                return (receiptResponse, new List<ftActionJournal>());
            }
        }
    }
}
