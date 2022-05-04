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
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class PosReceiptCommand : RequestCommand
    {
        public PosReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryME signatureFactory, IConfigurationRepository configurationRepository, IJournalMERepository journalMERepository) : base(logger, signatureFactory, configurationRepository, journalMERepository)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            try
            {
                var invoice = JsonConvert.DeserializeObject<Invoice>(request.ftReceiptCaseData);
                var queueME = await _configurationRepository.GetQueueMEAsync(queue.ftQueueId).ConfigureAwait(false);
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
                var totPRice = request.cbChargeItems.Sum(x => x.Amount);
                var invoiceDetails = new InvoiceDetails()
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
                var registerInvoiceRequest = new RegisterInvoiceRequest()
                {
                    InvoiceDetails = invoiceDetails,
                    SoftwareCode = scu.SoftwareCode,
                    TcrCode = scu.TcrCode,
                    IsIssuerInVATSystem = true,
                    BusinessUnitCode = scu.BusinessUnitCode,
                    Moment = request.cbReceiptMoment,
                    OperatorCode = invoice.OperatorCode,
                    RequestId = queueItem.ftQueueItemId,
                    SubsequentDeliveryType = invoice.SubsequentDeliveryType == null ? null :(SubsequentDeliveryType) Enum.Parse(typeof(SubsequentDeliveryType), invoice.SubsequentDeliveryType)
                };
                var registerInvoiceResponse = await client.RegisterInvoiceAsync(registerInvoiceRequest).ConfigureAwait(false);
                var lastJournal = await _journalMERepository.GetLastEntryAsync();
                var journal = new ftJournalME()
                {
                    ftJournalMEId = Guid.NewGuid(),
                    ftQueueId = queue.ftQueueId,
                    ftQueueItemId = queueItem.ftQueueItemId,
                    cbReference = request.cbReceiptReference,
                    ftOrdinalNumber = lastJournal == null ? 1 : lastJournal.ftOrdinalNumber++                    
                };
                journal.ftInvoiceNumber = string.Concat(scu.BusinessUnitCode, '/', journal.ftOrdinalNumber, '/', request.cbReceiptMoment.Year, '/', scu.TcrCode);
                await _journalMERepository.InsertAsync(journal).ConfigureAwait(false);
                var receiptResponse = CreateReceiptResponse(request, queueItem);
                return new RequestCommandResponse()
                {
                    ReceiptResponse = receiptResponse
                };
            }
            catch (Exception ex) when (ex.GetType().Name == RETRYPOLICYEXCEPTION_NAME)
            {
                _logger.LogDebug(ex, "TSE not reachable.");
                throw;
            }
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
    }
}
