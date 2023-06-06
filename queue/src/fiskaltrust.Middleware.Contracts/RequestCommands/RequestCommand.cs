using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public abstract class RequestCommand
    {
        public abstract Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, bool isBeingResent = false);

        public abstract Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem);

        protected ReceiptResponse CreateReceiptResponse(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, string ftCashBoxIdentification, long ftState)
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
                ftState = ftState,
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

        public async Task<RequestCommandResponse> ProcessFailedReceiptRequest(ICountrySpecificSettings countryspecificSettings, ftQueue queue, ftQueueItem queueItem, ReceiptRequest request)
        {
            var queueIt = await countryspecificSettings.CountrySpecificQueueRepository.GetQueueAsync(queue.ftQueueId).ConfigureAwait(false);
            if (queueIt.SSCDFailCount == 0)
            {
                queueIt.SSCDFailMoment = DateTime.UtcNow;
                queueIt.SSCDFailQueueItemId = queueItem.ftQueueItemId;
            }
            queueIt.SSCDFailCount++;
            await countryspecificSettings.CountrySpecificQueueRepository.InsertOrUpdateQueueAsync(queueIt).ConfigureAwait(false);
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIt.CashBoxIdentification, countryspecificSettings.CountryBaseState);
            receiptResponse.ftStateData = $"Queue is in failed mode. SSCDFailMoment: {queueIt.SSCDFailMoment}, SSCDFailCount: {queueIt.SSCDFailCount}.";
            if (countryspecificSettings.ResendFailedReceipts)
            {
                receiptResponse.ftState = countryspecificSettings.CountryBaseState | 0x8;
                receiptResponse.ftStateData += " When connection is established use zeroreceipt for subsequent booking!";
            }
            else
            {
                receiptResponse.ftState = countryspecificSettings.CountryBaseState | 0x2;
            }
            return new RequestCommandResponse { ReceiptResponse = receiptResponse };
        }
    }
}