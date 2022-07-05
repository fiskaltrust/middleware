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
    public class CompleteVoidedReceipt : PosReceiptCommand
    {
        protected const string PreviousReceiptNotSet =
            "The field cbPreviousReceiptReference must be set to the cbReceiptReference of the receipt to cancel!";
        public CompleteVoidedReceipt(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository,
            IMiddlewareJournalMERepository journalMeRepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareActionJournalRepository actionJournalRepository, QueueMEConfiguration queueMeConfiguration) :
            base(logger, configurationRepository, journalMeRepository, queueItemRepository, actionJournalRepository, queueMeConfiguration)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueMe)
        {
            var (scu, _) = await ValidateVoidReceipt(request, queueItem, queueMe);
            var queueItemToCancel = GetReceiptToCancel(request, out var journalMeToCancel, out var receiptRequestToCancel, out var invoiceToCancel);
            return await CompleteVoidOfReceipt(client, queue, queueItem, queueMe, receiptRequestToCancel, invoiceToCancel, queueItemToCancel, journalMeToCancel, scu);
        }

        protected async Task<RequestCommandResponse> CompleteVoidOfReceipt(IMESSCD client, ftQueue queue, ftQueueItem queueItem, ftQueueME queueMe,
            ReceiptRequest receiptRequestToCancel, Invoice invoiceToCancel, ftQueueItem queueItemToCancel,
            ftJournalME journalMeToCancel, ftSignaturCreationUnitME scu)
        {
            var invoiceDetailsCancel =
                await CreateInvoiceDetail(receiptRequestToCancel, invoiceToCancel, queueItemToCancel, true)
                    .ConfigureAwait(false);
            invoiceDetailsCancel.YearlyOrdinalNumber = await GetNextOrdinalNumber(queueItem).ConfigureAwait(false);
            invoiceDetailsCancel.InvoiceCorrectionDetails = new InvoiceCorrectionDetails
            {
                ReferencedIKOF = journalMeToCancel.IIC,
                ReferencedMoment = queueItemToCancel.cbReceiptMoment,
                CorrectionType = InvoiceCorrectionType.Corrective
            };
            return await SendInvoiceDetailToCis(client, queue, receiptRequestToCancel, queueItem, queueMe, scu,
                invoiceDetailsCancel, invoiceToCancel);
        }

        protected ftQueueItem GetReceiptToCancel(ReceiptRequest request, out ftJournalME journalMeToCancel,
            out ReceiptRequest receiptRequestToCancel, out Invoice invoiceToCancel)
        {
            var queueItemToCancel = QueueItemRepository.GetByReceiptReferenceAsync(request.cbPreviousReceiptReference)
                .ToListAsync().Result.FirstOrDefault();
            if (queueItemToCancel == null)
            {
                throw new ReferenceNotSetException(PreviousReceiptNotSet);
            }

            journalMeToCancel = JournalMeRepository.GetByQueueItemId(queueItemToCancel.ftQueueItemId).ToListAsync()
                .Result.FirstOrDefault();
            if (journalMeToCancel?.IIC == null)
            {
                throw new InvoiceNotSignedException("Invoice was not signed. No IIC was created by the CA");
            }

            receiptRequestToCancel = JsonConvert.DeserializeObject<ReceiptRequest>(queueItemToCancel.request);
            invoiceToCancel = JsonConvert.DeserializeObject<Invoice>(receiptRequestToCancel.ftReceiptCaseData);
            return queueItemToCancel;
        }

        protected async Task<(ftSignaturCreationUnitME, Invoice)> ValidateVoidReceipt(ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueMe)
        {
            var scu = await IsEnuRegistered(queueMe).ConfigureAwait(false);
            await CashDepositOutstanding().ConfigureAwait(false);
            await InvoiceAlreadyReceived(queueItem);
            var invoice = JsonConvert.DeserializeObject<Invoice>(request.ftReceiptCaseData);
            IsOperatorSet(invoice);
            if (string.IsNullOrWhiteSpace(request.cbPreviousReceiptReference))
            {
                throw new ReferenceNotSetException(PreviousReceiptNotSet);
            }

            return (scu, invoice);
        }
    }
}
