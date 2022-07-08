using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class CompleteVoidedReceiptCommand : PosReceiptCommand
    {
        protected const string PreviousReceiptNotSet =
            "The field cbPreviousReceiptReference must be set to the cbReceiptReference of the receipt to cancel!";
        public CompleteVoidedReceiptCommand(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository,
            IMiddlewareJournalMERepository journalMeRepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareActionJournalRepository actionJournalRepository, QueueMEConfiguration queueMeConfiguration) :
            base(logger, configurationRepository, journalMeRepository, queueItemRepository, actionJournalRepository, queueMeConfiguration)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueMe)
        {
            var (scu, _) = await ValidateVoidReceipt(request, queueMe);
            var receiptToCancel = await GetReceiptToCancel(request).ConfigureAwait(false);
            var invoiceDetailsCancel =
                await CreateInvoiceDetail(receiptToCancel.ReceiptRequest, receiptToCancel.Invoice, receiptToCancel.QueueItem, true)
                    .ConfigureAwait(false);
            invoiceDetailsCancel.YearlyOrdinalNumber = await GetNextOrdinalNumber(queueItem).ConfigureAwait(false);
            invoiceDetailsCancel.InvoiceCorrectionDetails = GetInvoiceCorrectionDetails(receiptToCancel.QueueItem, receiptToCancel.JournalMe);
            invoiceDetailsCancel.InvoicingType = InvoicingType.Corrective;
            return await SendInvoiceDetailToCis(client, queue, receiptToCancel.ReceiptRequest, queueItem, queueMe, scu,
                invoiceDetailsCancel, receiptToCancel.Invoice);
        }
        
        protected InvoiceCorrectionDetails GetInvoiceCorrectionDetails(ftQueueItem queueItemToCancel, ftJournalME journalMeToCancel)
        {
            return new InvoiceCorrectionDetails
            {
                ReferencedIKOF = journalMeToCancel.IIC,
                ReferencedMoment = queueItemToCancel.cbReceiptMoment,
                CorrectionType = InvoiceCorrectionType.Corrective
            };
        }

        protected async Task<ReceiptToCancel> GetReceiptToCancel(ReceiptRequest request)
        {
            var journalMeToCancel = JournalMeRepository.GetByReceiptReference(request.cbPreviousReceiptReference).ToListAsync()
                .Result.FirstOrDefault();
            if (journalMeToCancel?.IIC == null)
            {
                throw new InvoiceNotSignedException("Invoice was not signed. No IIC was created by the CA");
            }
            var queueItemToCancel = await QueueItemRepository.GetAsync(journalMeToCancel.ftQueueItemId).ConfigureAwait(false);
            if (queueItemToCancel == null)
            {
                throw new ReferenceNotSetException(PreviousReceiptNotSet);
            }
            var receiptRequestToCancel = JsonConvert.DeserializeObject<ReceiptRequest>(queueItemToCancel.request);
            var invoiceToCancel = JsonConvert.DeserializeObject<Invoice>(receiptRequestToCancel.ftReceiptCaseData);
            return new ReceiptToCancel
            {
                QueueItem = queueItemToCancel,
                ReceiptRequest = receiptRequestToCancel,
                Invoice = invoiceToCancel,
                JournalMe = journalMeToCancel
            };
        }

        protected async Task<(ftSignaturCreationUnitME, Invoice)> ValidateVoidReceipt(ReceiptRequest request, ftQueueME queueMe)
        {
            var scu = await IsEnuRegistered(queueMe).ConfigureAwait(false);
            await CashDepositOutstanding().ConfigureAwait(false);
            await InvoiceAlreadyReceived(request.cbReceiptReference);
            var invoice = JsonConvert.DeserializeObject<Invoice>(request.ftReceiptCaseData);
            IsOperatorSet(invoice);
            return string.IsNullOrWhiteSpace(request.cbPreviousReceiptReference)
                ? throw new ReferenceNotSetException(PreviousReceiptNotSet)
                : (scu, invoice);
        }
    }

    public struct ReceiptToCancel
    {
        public ftQueueItem QueueItem;
        public ftJournalME JournalMe;
        public ReceiptRequest ReceiptRequest;
        public Invoice Invoice;
    }
}
