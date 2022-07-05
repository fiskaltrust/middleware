using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private const string PreviousReceiptNotSet =
            "The field cbPreviousReceiptReference must be set to the cbReceiptReference of the receipt to cancel!";
        public CompleteVoidedReceipt(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository,
            IMiddlewareJournalMERepository journalMeRepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareActionJournalRepository actionJournalRepository, QueueMEConfiguration queueMeConfiguration) :
            base(logger, configurationRepository, journalMeRepository, queueItemRepository, actionJournalRepository, queueMeConfiguration)
        { }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueueME queueMe, ftQueueItem queueItem, ReceiptRequest request) => throw new NotImplementedException();

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueMe)
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
            var queueItemToCancel = QueueItemRepository.GetByReceiptReferenceAsync(request.cbPreviousReceiptReference)
                .ToListAsync().Result.FirstOrDefault();
            if (queueItemToCancel == null)
            {
                throw new ReferenceNotSetException(PreviousReceiptNotSet);
            }
            var journalMeToCancel = JournalMeRepository.GetByQueueItemId(queueItemToCancel.ftQueueItemId).ToListAsync()
                .Result.FirstOrDefault();
            if (journalMeToCancel?.IIC == null)
            {
                throw new InvoiceNotSignedException("Invoice was not signed. No IIC was created by the CA");
            }
            var receiptRequestToCancel = JsonConvert.DeserializeObject<ReceiptRequest>(queueItemToCancel.request);
            var invoiceToCancel = JsonConvert.DeserializeObject<Invoice>(receiptRequestToCancel.ftReceiptCaseData);
            var invoiceDetailsCancel = await CreateInvoiceDetail(receiptRequestToCancel, invoiceToCancel, queueItemToCancel, true).ConfigureAwait(false);
            invoiceDetailsCancel.YearlyOrdinalNumber = await GetNextOrdinalNumber(queueItem).ConfigureAwait(false);
            invoiceDetailsCancel.InvoiceCorrectionDetails = new InvoiceCorrectionDetails
            {
                ReferencedIKOF = journalMeToCancel.IIC,
                ReferencedMoment = queueItemToCancel.cbReceiptMoment,
                CorrectionType = InvoiceCorrectionType.Corrective
            };
            return await SendInvoiceDetailToCis(client, queue, receiptRequestToCancel, queueItem, queueMe, scu, invoiceDetailsCancel, invoiceToCancel);
        }
    }
}
