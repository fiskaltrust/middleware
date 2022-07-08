using System.Collections.Generic;
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
    public class PartialVoidedReceiptCommand : CompleteVoidedReceiptCommand
    {
        public PartialVoidedReceiptCommand(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository,
            IMiddlewareJournalMERepository journalMeRepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareActionJournalRepository actionJournalRepository, QueueMEConfiguration queueMeConfiguration) :
            base(logger, configurationRepository, journalMeRepository, queueItemRepository, actionJournalRepository, queueMeConfiguration)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueMe)
        {
            var (scu, invoice) = await ValidateVoidReceipt(request, queueMe);
            var receiptToCancel = await GetReceiptToCancel(request).ConfigureAwait(false);
            if (receiptToCancel.ReceiptRequest.cbPayItems.Any(x => x.IsCashLocalCurrency()) ||
                request.cbPayItems.Any(x => x.IsCashLocalCurrency()))
            {
                return await VoidForCashReceipts(client, queue, request, queueItem, queueMe, receiptToCancel.ReceiptRequest, receiptToCancel.Invoice, receiptToCancel.QueueItem, receiptToCancel.JournalMe, scu, invoice);
            }
            var invoiceDetailToCancel = await CreateInvoiceDetail(receiptToCancel.ReceiptRequest, receiptToCancel.Invoice, receiptToCancel.QueueItem).ConfigureAwait(false);
            var chargeItems = GetVoidChargeItems(request, receiptToCancel);
            chargeItems.AddRange(from chargeItem in request.cbChargeItems let foundItem = FindChargeItem(receiptToCancel.ReceiptRequest, chargeItem) where foundItem == null select chargeItem);

            request.cbChargeItems = chargeItems.ToArray();
            var invoiceDetails = await CreateInvoiceDetail(request, invoice, queueItem).ConfigureAwait(false);
            invoiceDetails.YearlyOrdinalNumber = await GetNextOrdinalNumber(queueItem).ConfigureAwait(false);
            invoiceDetails.InvoiceCorrectionDetails = GetInvoiceCorrectionDetails(receiptToCancel.QueueItem, receiptToCancel.JournalMe);
            invoiceDetails.InvoicingType = InvoicingType.Corrective;

            return await SendInvoiceDetailToCis(client, queue, request, queueItem, queueMe, scu, invoiceDetails, invoice).ConfigureAwait(false);
        }

        private static List<ChargeItem> GetVoidChargeItems(ReceiptRequest request, ReceiptToCancel receiptToCancel)
        {
            var chargeItems = new List<ChargeItem>();
            foreach (var chargeItem in receiptToCancel.ReceiptRequest.cbChargeItems)
            {
                var foundItem = FindChargeItem(request, chargeItem);
                var existingQuantity = foundItem?.Quantity ?? 0;
                if (existingQuantity > 0 && existingQuantity != chargeItem.Quantity)
                {
                    chargeItem.Amount = chargeItem.Amount / chargeItem.Quantity *
                                        (chargeItem.Quantity - existingQuantity);
                }

                chargeItem.Quantity = (chargeItem.Quantity - existingQuantity) * -1;
                chargeItems.Add(chargeItem);
            }

            return chargeItems;
        }

        private static ChargeItem FindChargeItem(ReceiptRequest request, ChargeItem chargeItem)
        {
            var foundItem = request.cbChargeItems.ToList().Find(
                foundChargeItem => foundChargeItem.AccountNumber == chargeItem.AccountNumber &&
                                   foundChargeItem.CostCenter == chargeItem.CostCenter &&
                                   foundChargeItem.Description == chargeItem.Description &&
                                   foundChargeItem.ProductBarcode == chargeItem.ProductBarcode &&
                                   foundChargeItem.ProductGroup == chargeItem.ProductGroup &&
                                   foundChargeItem.ProductNumber == chargeItem.ProductNumber &&
                                   foundChargeItem.Unit == chargeItem.Unit &&
                                   foundChargeItem.UnitPrice == chargeItem.UnitPrice &&
                                   foundChargeItem.UnitQuantity == chargeItem.UnitQuantity &&
                                   foundChargeItem.VATAmount == chargeItem.VATAmount &&
                                   foundChargeItem.VATRate == chargeItem.VATRate &&
                                   foundChargeItem.ftChargeItemCase == chargeItem.ftChargeItemCase &&
                                   foundChargeItem.ftChargeItemCaseData == chargeItem.ftChargeItemCaseData);
            return foundItem;
        }

        private async Task<RequestCommandResponse> VoidForCashReceipts(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem,
            ftQueueME queueMe, ReceiptRequest receiptRequestToCancel, Invoice invoiceToCancel, ftQueueItem queueItemToCancel,
            ftJournalME journalMeToCancel, ftSignaturCreationUnitME scu, Invoice invoice)
        {
            var invoiceDetailsCancel =
                await CreateInvoiceDetail(receiptRequestToCancel, invoiceToCancel, queueItemToCancel, true)
                    .ConfigureAwait(false);
            invoiceDetailsCancel.YearlyOrdinalNumber = await GetNextOrdinalNumber(queueItem).ConfigureAwait(false);
            invoiceDetailsCancel.InvoiceCorrectionDetails = GetInvoiceCorrectionDetails(queueItemToCancel, journalMeToCancel);
            invoiceDetailsCancel.InvoicingType = InvoicingType.Corrective;
            await SendInvoiceDetailToCis(client, queue, receiptRequestToCancel, queueItem, queueMe, scu,
                invoiceDetailsCancel, invoiceToCancel);
            var invoiceDetails = await CreateInvoiceDetail(request, invoice, queueItem).ConfigureAwait(false);
            invoiceDetails.InvoicingType = InvoicingType.Invoice;
            return await SendInvoiceDetailToCis(client, queue, request, queueItem, queueMe, scu, invoiceDetails, invoice);
        }
    }
}
