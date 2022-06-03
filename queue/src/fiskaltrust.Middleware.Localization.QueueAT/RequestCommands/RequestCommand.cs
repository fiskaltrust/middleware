using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueAT.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueAT.RequestCommands
{
    public abstract class RequestCommand
    {
        public abstract string ReceiptName { get; }

        public abstract Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueAT queueDE, ReceiptRequest request, ftQueueItem queueItem);

        protected static ReceiptResponse CreateReceiptResponse(ReceiptRequest request, ftQueueItem queueItem, ftQueueAT queueAT)
        {
            return new ReceiptResponse
            {
                ftCashBoxID = request.ftCashBoxID,
                ftCashBoxIdentification = queueAT.CashBoxIdentification,
                ftQueueID = queueItem.ftQueueId.ToString(),
                ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                ftQueueRow = queueItem.ftQueueRow,
                cbTerminalID = request.cbTerminalID,
                cbReceiptReference = request.cbReceiptReference,
                ftReceiptMoment = DateTime.UtcNow,
                ftState = 0x4445000000000000
            };
        }
    }
}