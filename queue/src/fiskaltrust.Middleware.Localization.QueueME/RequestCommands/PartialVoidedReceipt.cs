using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueME.Extensions;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class PartialVoidedReceipt : CompleteVoidedReceipt
    {
        public PartialVoidedReceipt(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository,
            IMiddlewareJournalMERepository journalMeRepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareActionJournalRepository actionJournalRepository, QueueMEConfiguration queueMeConfiguration) :
            base(logger, configurationRepository, journalMeRepository, queueItemRepository, actionJournalRepository, queueMeConfiguration)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueMe)
        {
            var (scu, invoice) = await ValidateVoidReceipt(request, queueItem, queueMe);
            var queueItemToCancel = GetReceiptToCancel(request, out var journalMeToCancel, out var receiptRequestToCancel, out var invoiceToCancel);
            if (receiptRequestToCancel.cbPayItems.Any(x => x.IsCashLocalCurrency()) ||
                request.cbPayItems.Any(x => x.IsCashLocalCurrency()))
            {
                await CompleteVoidOfReceipt(client, queue, queueItem, queueMe, receiptRequestToCancel,
                    invoiceToCancel, queueItemToCancel, journalMeToCancel, scu);
                var invoiceDetails = await CreateInvoiceDetail(request, invoice, queueItem).ConfigureAwait(false);
                return await SendInvoiceDetailToCis(client, queue, request, queueItem, queueMe, scu, invoiceDetails, invoice);
            }

            return null; //ToDo partial no cash
        }
    }
}
