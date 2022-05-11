using System;
using System.Threading.Tasks;
using System.Linq;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;
using fiskaltrust.Middleware.Localization.QueueME.Extensions;
using System.Collections.Generic;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class PosReceiptCommand : RequestCommand
    {
        public PosReceiptCommand(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository,
            IMiddlewareJournalMERepository journalMERepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareActionJournalRepository actionJournalRepository) :
            base(logger, configurationRepository, journalMERepository, queueItemRepository, actionJournalRepository)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueME)
        {
            try
            {
                if (await CashDepositeOutstanding().ConfigureAwait(false))
                {
                    throw new CashDepositOutstandingException("Register initial amount with Cash Deposit Receipt for today!");
                }
                var invoice = JsonConvert.DeserializeObject<Invoice>(request.ftReceiptCaseData);
                if (queueME == null)
                {
                    throw new ENUNotRegisteredException("No QueueME!");
                }
                if (queueME.ftSignaturCreationUnitMEId == null)
                {
                    throw new ArgumentNullException(nameof(queueME.ftSignaturCreationUnitMEId));
                }
                if (string.IsNullOrEmpty(invoice.OperatorCode))
                {
                    throw new ArgumentNullException(nameof(invoice.OperatorCode));
                }
                var scu = await _configurationRepository.GetSignaturCreationUnitMEAsync(queueME.ftSignaturCreationUnitMEId.Value).ConfigureAwait(false);
                if (scu == null)
                {
                    throw new ENUNotRegisteredException("No SignaturCreationUnitME!");
                }
                var invoiceDetails = CreateInvoiceDetail(request, invoice);
                var registerInvoiceRequest = CreateInvoiceReqest(request, queueItem, invoice, scu, invoiceDetails);
                var registerInvoiceResponse = await client.RegisterInvoiceAsync(registerInvoiceRequest).ConfigureAwait(false);
                await InsertJournalME(queue, request, queueItem, scu, registerInvoiceResponse).ConfigureAwait(false);
                var receiptResponse = CreateReceiptResponse(request, queueItem);
                return new RequestCommandResponse()
                {
                    ReceiptResponse = receiptResponse
                };
            }
            catch (Exception ex) when (ex.GetType().Name == RETRYPOLICYEXCEPTION_NAME)
            {
                _logger.LogDebug(ex, "TSE not reachable.");
                return await ProcessFailedReceiptRequest(queueItem, request, queueME).ConfigureAwait(false);
            }
            catch (CashDepositOutstandingException ex)
            {
                _logger.LogCritical(ex, "An exception occured while processing this request.");
                throw ex;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "An exception occured while processing this request.");
                return await ProcessFailedReceiptRequest( queueItem, request, queueME).ConfigureAwait(false);
            }
        }
        private async Task<bool> CashDepositeOutstanding()
        {
            var actionJournals = await _actionJournalRepository.GetAsync().ConfigureAwait(false);
            var lastActionJournal = actionJournals.Where(x => x.Type == JournalTypes.CashDepositME.ToString()).OrderByDescending(x => x.TimeStamp).LastOrDefault();
            if (lastActionJournal == null || lastActionJournal.Moment.Date != DateTime.UtcNow.Date)
            {
                return true;           
            }
            return false;
        }

        private InvoiceDetails CreateInvoiceDetail(ReceiptRequest request, Invoice invoice)
        {
            return new InvoiceDetails()
            {
                InvoiceType = request.GetInvoiceType(),
                SelfIssuedInvoiceType = invoice.TypeOfSelfiss == null ? null : (SelfIssuedInvoiceType) Enum.Parse(typeof(SelfIssuedInvoiceType), invoice.TypeOfSelfiss),
                TaxFreeAmount = request.cbChargeItems.Where(x => x.GetVatRate().Equals(0)).Sum(x => x.Amount * x.Quantity),
                NetAmount = request.cbChargeItems.Sum(x => x.Amount / (1 + (x.GetVatRate() / 100))),
                TotalVatAmount = request.cbChargeItems.Sum(x => x.Amount * x.GetVatRate() / (100 + x.GetVatRate())),
                GrossAmount = request.cbChargeItems.Sum(x => x.Amount),
                PaymentDeadline = invoice.PayDeadline,
                InvoiceCorrectionDetails = invoice.CorrectiveInv != null ? new InvoiceCorrectionDetails()
                {
                    ReferencedIKOF = invoice.CorrectiveInv.ReferencedIKOF,
                    ReferencedMoment = invoice.CorrectiveInv.ReferencedMoment,
                    CorrectionType = (InvoiceCorrectionType) Enum.Parse(typeof(InvoiceCorrectionType), invoice.CorrectiveInv.Type),
                } : null,
                PaymentDetails = request.GetPaymentMethodTypes(),
                Currency = new CurrencyDetails() { CurrencyCode = "EUR" },
                Buyer = GetBuyer(request),
                ItemDetails = GetInvoiceItems(request),
                Fees = GetFees(invoice),
                ExportedGoodsAmount = request.cbChargeItems.Where(x => x.IsExportGood()).Sum(x => x.Amount),
                TaxPeriod = new TaxPeriod()
                {
                    Month = (uint) request.cbReceiptMoment.Month,
                    Year = (uint) request.cbReceiptMoment.Year
                }
            };
        }
        private async Task InsertJournalME(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftSignaturCreationUnitME scu, RegisterInvoiceResponse registerInvoiceResponse)
        {
            var lastJournal = await _journalMERepository.GetLastEntryAsync();
            var journal = new ftJournalME()
            {
                ftJournalMEId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                cbReference = request.cbReceiptReference,
                Number = queue.ftReceiptNumerator,
                FIC = registerInvoiceResponse.FIC,
                IIC = registerInvoiceResponse.IIC,
                JournalType = (long)JournalTypes.JournalME
            };
            if (lastJournal == null)
            {
                journal.ftOrdinalNumber = 1;
            }else{ 
                var lastqueuItem = await _queueItemRepository.GetAsync(lastJournal.ftQueueItemId);
                if (queueItem.ftWorkMoment.Value.Year != lastqueuItem.ftWorkMoment.Value.Year)
                {
                    journal.ftOrdinalNumber = 1;
                }
                else
                {
                    journal.ftOrdinalNumber = lastJournal.ftOrdinalNumber +1;
                }
             }
            journal.ftInvoiceNumber = string.Concat(scu.BusinessUnitCode, '/', journal.ftOrdinalNumber, '/', queueItem.ftWorkMoment.Value.Year, '/', scu.TcrCode);
            await _journalMERepository.InsertAsync(journal).ConfigureAwait(false);
        }
        private static RegisterInvoiceRequest CreateInvoiceReqest(ReceiptRequest request, ftQueueItem queueItem, Invoice invoice, ftSignaturCreationUnitME scu, InvoiceDetails invoiceDetails)
        {
            return new RegisterInvoiceRequest()
            {
                InvoiceDetails = invoiceDetails,
                SoftwareCode = scu.SoftwareCode,
                TcrCode = scu.TcrCode,
                IsIssuerInVATSystem = true,
                BusinessUnitCode = scu.BusinessUnitCode,
                Moment = request.cbReceiptMoment,
                OperatorCode = invoice.OperatorCode,
                RequestId = queueItem.ftQueueItemId,
                SubsequentDeliveryType = invoice.SubsequentDeliveryType == null ? null : (SubsequentDeliveryType) Enum.Parse(typeof(SubsequentDeliveryType), invoice.SubsequentDeliveryType)
            };
        }
        private List<InvoiceFee> GetFees(Invoice invoice)
        {
            if(invoice.Fees is null)
            {
                return null;
            }
            var result = new List<InvoiceFee>();
            foreach(var fee in invoice.Fees)
            {
                result.Add(new InvoiceFee()
                {
                    Amount = fee.Amount,
                    FeeType = (FeeType) Enum.Parse(typeof(FeeType), fee.FeeType)
                });
            }
            return result;
        }
        private List<InvoiceItem> GetInvoiceItems(ReceiptRequest request)
        {
            if (request.cbChargeItems.Count() > 1000)
            {
                throw new MaxInvoiceItemsExceededException();
            }
            var items = new List<InvoiceItem>();
            foreach(var chargeItem in request.cbChargeItems)
            {
                items.Add(chargeItem.GetInvoiceItem());
            }
            return items;
        }
        private BuyerDetails GetBuyer(ReceiptRequest request)
        {
            if (string.IsNullOrEmpty(request.cbCustomer))
            {
                return null;
            }
            try
            {
                var buyer = JsonConvert.DeserializeObject<Buyer>(request.cbCustomer);
                if (buyer == null)
                {
                    throw new Exception("Value in Field cbCustomer could not be parsed");
                }
                return new BuyerDetails()
                {
                    IdentificationNumber = buyer.IdentificationNumber,
                    IdentificationType = (BuyerIdentificationType) Enum.Parse(typeof(BuyerIdentificationType), buyer.BuyerIdentificationType),
                    Name = buyer.Name,
                    Address = buyer.Address,
                    Town = buyer.Town,
                    Country = buyer.Country,
                };
            }
            catch (Exception ex)
            {
                throw new BuyerParseException("Error when parsing Buyer in cbCustomer field!", ex);
            }
        }
        public override async Task<bool> ReceiptNeedsReprocessing(ftQueueME queueME, ftQueueItem queueItem, ReceiptRequest request)
        {
            var journalME = await _journalMERepository.GetByQueueItemId(queueItem.ftQueueItemId).FirstOrDefaultAsync().ConfigureAwait(false);
            if (journalME == null || string.IsNullOrEmpty(journalME.IIC) || string.IsNullOrEmpty(journalME.FIC))
            {
                return true;
            }
            return false;
       }
    }
}
