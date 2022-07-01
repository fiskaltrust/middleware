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
using fiskaltrust.Middleware.Localization.QueueME.Factories;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class PosReceiptCommand : RequestCommand
    {
        public PosReceiptCommand(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository,
            IMiddlewareJournalMERepository journalMERepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareActionJournalRepository actionJournalRepository, QueueMEConfiguration queueMeConfiguration) :
            base(logger, configurationRepository, journalMERepository, queueItemRepository, actionJournalRepository, queueMeConfiguration)
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
                    throw new ENUNotRegisteredException("No SignaturCreationUnitME!");
                }
                var scu = await _configurationRepository.GetSignaturCreationUnitMEAsync(queueME.ftSignaturCreationUnitMEId.Value).ConfigureAwait(false);
                if (scu == null)
                {
                    throw new ENUNotRegisteredException("No SignaturCreationUnitME!");
                }
                if (string.IsNullOrEmpty(invoice.OperatorCode))
                {
                    throw new ArgumentNullException(nameof(invoice.OperatorCode));
                }
                var invoiceDetails = await CreateInvoiceDetail(request, invoice, queueItem).ConfigureAwait(false);

                var computeIICRequest = CreateComputeIICReqest(request, scu, invoiceDetails);
                var computeIICResponse = await client.ComputeIICAsync(computeIICRequest).ConfigureAwait(false);

                RegisterInvoiceResponse registerInvoiceResponse;
                try
                {
                    var registerInvoiceRequest = CreateInvoiceRequest(request, queueItem, invoice, scu, invoiceDetails, computeIICResponse);
                    registerInvoiceResponse = await client.RegisterInvoiceAsync(registerInvoiceRequest).ConfigureAwait(false);
                }
                catch(EntryPointNotFoundException ex)
                {
                    _logger.LogDebug(ex, "TSE is not reachable.");
                    return await ProcessFailedReceiptRequest(queueItem, request, queueME).ConfigureAwait(false);
                }

                await InsertJournalME(queue, request, queueItem, scu, registerInvoiceResponse, invoiceDetails, computeIICResponse).ConfigureAwait(false);
                var receiptResponse = CreateReceiptResponse(request, queueItem);
                receiptResponse.ftSignatures = receiptResponse.ftSignatures.Concat(new SignatureItemFactory(request, computeIICResponse, invoiceDetails.YearlyOrdinalNumber, scu, _queueMeConfiguration).CreateSignatures()).ToArray();

                return new RequestCommandResponse()
                {
                    ReceiptResponse = receiptResponse,
                };
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "An exception occured while processing this request.");
                throw;
            }

        }
        private async Task<bool> CashDepositeOutstanding()
        {
            var _journalME = await _journalMERepository.GetAsync().ConfigureAwait(false);
            var journalME = _journalME.Where(x => x.JournalType == 0x44D5_0000_0000_0007).OrderByDescending(x => x.TimeStamp).FirstOrDefault();

            if (journalME == null || new DateTime(journalME.TimeStamp).Date != DateTime.UtcNow.Date)
            {
                return true;
            }
            return false;
        }

        private async Task<InvoiceDetails> CreateInvoiceDetail(ReceiptRequest request, Invoice invoice, ftQueueItem queueItem)
        {
            var invoiceDetails =  new InvoiceDetails()
            {
                InvoiceType = request.GetInvoiceType(),
                SelfIssuedInvoiceType = invoice.TypeOfSelfiss == null ? null : (SelfIssuedInvoiceType) Enum.Parse(typeof(SelfIssuedInvoiceType), invoice.TypeOfSelfiss),
                TaxFreeAmount = request.cbChargeItems.Where(x => x.GetVatRate().Equals(0)).Sum(x => x.Amount),
                PaymentDeadline = invoice.PayDeadline,
                InvoiceCorrectionDetails = invoice.CorrectiveInv != null ? new InvoiceCorrectionDetails()
                {
                    ReferencedIKOF = invoice.CorrectiveInv.ReferencedIKOF,
                    ReferencedMoment = invoice.CorrectiveInv.ReferencedMoment,
                    CorrectionType = (InvoiceCorrectionType) Enum.Parse(typeof(InvoiceCorrectionType), invoice.CorrectiveInv.Type),
                } : null,
                PaymentDetails = request.GetPaymentMethodTypes(),
                Currency = new CurrencyDetails() { CurrencyCode = "EUR", ExchangeRateToEuro = 1 },
                Buyer = GetBuyer(request),
                ItemDetails = GetInvoiceItems(request),
                Fees = GetFees(invoice),
                ExportedGoodsAmount = request.cbChargeItems.Where(x => x.IsExportGood()).Sum(x => x.Amount),
                TaxPeriod = new TaxPeriod()
                {
                    Month = (uint) request.cbReceiptMoment.Month,
                    Year = (uint) request.cbReceiptMoment.Year
                },
                YearlyOrdinalNumber = await GetNextOrdinalNumber(queueItem).ConfigureAwait(false)
            };

            invoiceDetails.NetAmount = invoiceDetails.ItemDetails.Sum(x => x.NetAmount);
            invoiceDetails.TotalVatAmount = invoiceDetails.ItemDetails.Sum(x => x.NetAmount * x.VatRate/100 );
            invoiceDetails.GrossAmount = invoiceDetails.ItemDetails.Sum(x => x.GrossAmount);
            return invoiceDetails;
        }
        private async Task InsertJournalME(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftSignaturCreationUnitME scu, RegisterInvoiceResponse registerInvoiceResponse, InvoiceDetails invoice, ComputeIICResponse computeIICResponse)
        {
            var journal = new ftJournalME
            {
                ftJournalMEId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                cbReference = request.cbReceiptReference,
                Number = queue.ftReceiptNumerator + 1,
                FIC = registerInvoiceResponse.FIC,
                IIC = computeIICResponse.IIC,
                JournalType = (long) JournalTypes.JournalME,
                ftOrdinalNumber = (int) invoice.YearlyOrdinalNumber
            };
            journal.ftInvoiceNumber = string.Concat(scu.BusinessUnitCode, '/', journal.ftOrdinalNumber, '/', queueItem.ftWorkMoment.Value.Year, '/', scu.TcrCode);
            await _journalMERepository.InsertAsync(journal).ConfigureAwait(false);
        }

        private static ComputeIICRequest CreateComputeIICReqest(ReceiptRequest request, ftSignaturCreationUnitME scu, InvoiceDetails invoiceDetails)
        {
            return new ComputeIICRequest()
            {
                SoftwareCode = scu.SoftwareCode,
                TcrCode = scu.TcrCode,
                BusinessUnitCode = scu.BusinessUnitCode,
                Moment = request.cbReceiptMoment,
                YearlyOrdinalNumber = invoiceDetails.YearlyOrdinalNumber,
                GrossAmount = invoiceDetails.GrossAmount,
            };
        }

        private static RegisterInvoiceRequest CreateInvoiceRequest(ReceiptRequest request, ftQueueItem queueItem, Invoice invoice, ftSignaturCreationUnitME scu, InvoiceDetails invoiceDetails, ComputeIICResponse computeIICResponse)
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
                SubsequentDeliveryType = invoice.SubsequentDeliveryType == null ? null : (SubsequentDeliveryType) Enum.Parse(typeof(SubsequentDeliveryType), invoice.SubsequentDeliveryType),
                IIC = computeIICResponse.IIC,
                IICSignature = computeIICResponse.IICSignature
            };
        }

        private List<InvoiceFee> GetFees(Invoice invoice)
        {
            if (invoice.Fees is null)
            {
                return null;
            }
            var result = new List<InvoiceFee>();
            foreach (var fee in invoice.Fees)
            {
                result.Add(new InvoiceFee()
                {
                    Amount = fee.Amount,
                    FeeType = (FeeType) Enum.Parse(typeof(FeeType), fee.FeeType.ToString())
                });
            }
            return result;
        }
        private static List<InvoiceItem> GetInvoiceItems(ReceiptRequest request)
        {
            if (request.cbChargeItems.Count() > 1000)
            {
                throw new MaxInvoiceItemsExceededException();
            }
            var items = new List<InvoiceItem>();
            foreach (var chargeItem in request.cbChargeItems)
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
