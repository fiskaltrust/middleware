using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using System;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using System.Runtime.CompilerServices;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public abstract class RequestCommandIT : RequestCommand
    {
        public override long CountryBaseState => Constants.Cases.BASE_STATE;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly ILogger<RequestCommand> _logger;

        public RequestCommandIT(IConfigurationRepository configurationRepository, ILogger<RequestCommand> logger)
        {
            _configurationRepository = configurationRepository;
            _logger = logger;
        }

        protected async Task<RequestCommandResponse> ProcessFailedReceiptRequest(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request)
        {
            var queueIt = await _configurationRepository.GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false);
            if (queueIt.SSCDFailCount == 0)
            {
                queueIt.SSCDFailMoment = DateTime.UtcNow;
                queueIt.SSCDFailQueueItemId = queueItem.ftQueueItemId;
            }
            queueIt.SSCDFailCount++;
            await _configurationRepository.InsertOrUpdateQueueITAsync(queueIt).ConfigureAwait(false);
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIt.CashBoxIdentification);
            receiptResponse.ftState += CountryBaseState & 2;
            _logger.LogInformation(
                $"Queue is in failed mode. SSCDFailMoment: {queueIt.SSCDFailMoment}, SSCDFailCount: {queueIt.SSCDFailCount}. When connection is established use zeroreceipt for subsequent booking!");

            return new RequestCommandResponse { ReceiptResponse = receiptResponse };
        }

    }
}
