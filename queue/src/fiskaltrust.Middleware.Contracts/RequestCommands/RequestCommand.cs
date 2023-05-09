using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public abstract class RequestCommand
    {
        public abstract long CountryBaseState { get;}

        protected abstract IQueueRepository IQueueRepository { get;}

        public abstract Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, bool isResend = false);

        public abstract Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem);

        protected ReceiptResponse CreateReceiptResponse(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, string ftCashBoxIdentification, long? ftState = null)
        {
            var receiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
            return new ReceiptResponse
            {
                ftCashBoxID = request.ftCashBoxID,
                ftQueueID = queueItem.ftQueueId.ToString(),
                ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                ftQueueRow = queueItem.ftQueueRow,
                cbTerminalID = request.cbTerminalID,
                cbReceiptReference = request.cbReceiptReference,
                ftReceiptMoment = DateTime.UtcNow,
                ftState = ftState ?? CountryBaseState,
                ftReceiptIdentification = receiptIdentification,
                ftCashBoxIdentification = ftCashBoxIdentification
            };
        }

        protected ftActionJournal CreateActionJournal(Guid queueId, string type, Guid queueItemId, string message, string data, int priority = -1)
        {
            return new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queueId,
                ftQueueItemId = queueItemId,
                Type = type,
                Moment = DateTime.UtcNow,
                Message = message,
                Priority = priority,
                DataJson = data
            };
        }

        public async Task<RequestCommandResponse> ProcessFailedReceiptRequest(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request)
        {
            var queueIt = await IQueueRepository.GetQueueAsync(queue.ftQueueId).ConfigureAwait(false);
            if (queueIt.SSCDFailCount == 0)
            {
                queueIt.SSCDFailMoment = DateTime.UtcNow;
                queueIt.SSCDFailQueueItemId = queueItem.ftQueueItemId;
            }
            queueIt.SSCDFailCount++;
            await IQueueRepository.InsertOrUpdateQueueAsync(queueIt).ConfigureAwait(false);
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIt.CashBoxIdentification);
            receiptResponse.ftState = CountryBaseState | 0x2;
            receiptResponse.ftStateData = $"Queue is in failed mode. SSCDFailMoment: {queueIt.SSCDFailMoment}, SSCDFailCount: {queueIt.SSCDFailCount}. When connection is established use zeroreceipt for subsequent booking!";
            return new RequestCommandResponse { ReceiptResponse = receiptResponse };
        }
    }
}