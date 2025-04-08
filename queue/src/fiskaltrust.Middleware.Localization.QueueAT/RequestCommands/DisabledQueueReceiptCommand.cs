using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Services;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueAT.RequestCommands
{
    public class DisabledQueueReceiptCommand : RequestCommand
    {
        public override string ReceiptName => "Disabled-queue receipt";

        private const long SECURITY_MECHAMISN_DEACTIVATED_FLAG = 0x0000_0000_0000_0001;

        private bool _loggedDisabledQueueReceiptRequest = false;

        public DisabledQueueReceiptCommand(IATSSCDProvider sscdProvider, MiddlewareConfiguration middlewareConfiguration, QueueATConfiguration queueATConfiguration, ILogger<RequestCommand> logger)
            : base(sscdProvider, middlewareConfiguration, queueATConfiguration, logger) { }

        public override Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueAT queueAT, ReceiptRequest request, ftQueueItem queueItem, ReceiptResponse response)
        {
            var actionJournals = new List<ftActionJournal>();

            if (!_loggedDisabledQueueReceiptRequest)
            {
                actionJournals.Add(
                        new ftActionJournal
                        {
                            ftActionJournalId = Guid.NewGuid(),
                            ftQueueId = queueItem.ftQueueId,
                            ftQueueItemId = queueItem.ftQueueItemId,
                            Moment = DateTime.UtcNow,
                            Message = $"QueueId {queueItem.ftQueueId} was not activated or already deactivated"
                        }
                    );
                _loggedDisabledQueueReceiptRequest = true;
            }

            response.ftState += SECURITY_MECHAMISN_DEACTIVATED_FLAG;

            return Task.FromResult(new RequestCommandResponse
            {
                ReceiptResponse = response,
                ActionJournals = actionJournals,
            });
        }
    }
}
