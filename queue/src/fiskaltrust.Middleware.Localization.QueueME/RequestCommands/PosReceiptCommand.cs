using System;
using System.Threading.Tasks;
using System.Linq;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v2.me;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;
using fiskaltrust.Middleware.Localization.QueueME.Extensions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class PosReceiptCommand : RequestCommand
    {
        public PosReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryME signatureFactory, IConfigurationRepository configurationRepository, IMasterDataRepository<OutletMasterData> outletMasterDataRepository) : base(logger, signatureFactory, configurationRepository, outletMasterDataRepository)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            try
            {
                var invoice = JsonConvert.DeserializeObject<Invoice>(request.ftReceiptCaseData);
                var queueME = await _configurationRepository.GetQueueMEAsync(queue.ftQueueId).ConfigureAwait(false);
                if (queueME == null)
                {
                    throw new ENUNotRegisteredException();
                }
                if (string.IsNullOrEmpty(invoice.OperatorCode))
                {
                    throw new ArgumentNullException(nameof(invoice.OperatorCode));
                }
                var outlets = await _outletMasterDataRepository.GetAsync().ConfigureAwait(false);
                var outlet = outlets.Where(x => x.VatId.Equals(queueME.IssuerTIN)).FirstOrDefault();

                var totPRice = request.cbChargeItems.Sum(x => x.Amount);
                var registerInvoiceRequest = new RegisterInvoiceRequest();


                var invoiceType = new InvoiceType()
                {
                    TypeOfInv = request.GetInvoiceSType(),
                    InvType = request.GetInvoiceTSType(),
                    IsSimplifiedInv = invoice.IsSimplifiedInv,
                    IssueDateTime = request.cbReceiptMoment,
                    InvNum = string.Concat(queueME.BusinUnitCode,"/", request.cbReceiptReference, "/", request.cbReceiptMoment.Year, "/", queueME.TCRCode),
                    InvOrdNum = int.Parse(request.cbReceiptReference),
                    TCRCode = queueME.TCRCode,
                    IsIssuerInVAT = invoice.IsIssuerInVAT,
                    TaxFreeAmt = request.cbChargeItems.Where(x => x.GetVatRate().Equals(0)).Sum(x => x.Amount*x.Quantity),
                    MarkUpAmt = invoice.MarkUpAmt,
                    GoodsExAmt = invoice.GoodsExAmt,
                    TotPriceWoVAT = request.cbChargeItems.Sum(x => x.Amount / (1 + (x.GetVatRate() / 100))),
                    TotVATAmt = request.cbChargeItems.Sum(x => x.Amount * x.GetVatRate() / (100 + x.GetVatRate())),
                    TotPrice = request.cbChargeItems.Sum(x => x.Amount),
                    OperatorCode = invoice.OperatorCode,
                    BusinUnitCode = queueME.BusinUnitCode,
                    SoftCode = queueME.SoftCode,
                    IICRefs = new IICRefType[] {new IICRefType()
                    {
                        IIC = "Not implemented yet",
                    } },
                    IIC = _signatureFactory.ICCConcatenate(queueME.IssuerTIN, request.cbReceiptMoment, request.cbReceiptReference, queueME.BusinUnitCode, queueME.TCRCode, queueME.SoftCode, totPRice),
                    IICSignature = "Not implemented yet",
                    IsReverseCharge = false,
                    PayDeadline = invoice.PayDeadline,
                    ParagonBlockNum = invoice.ParagonBlockNum,
                    CorrectiveInv = invoice.CorrectiveInv != null ? new CorrectiveInvType() {
                        IICRef = invoice.CorrectiveInv.IICRef,
                        IssueDateTime = invoice.CorrectiveInv.IssueDateTime,
                        Type = (CorrectiveInvTypeSType) Enum.Parse(typeof(CorrectiveInvTypeSType), invoice.CorrectiveInv.Type),
                    } : null,
                    PayMethods = request.GetPaymentMethodTypes(),
                    Currency = new CurrencyType() { Code = CurrencyCodeSType.EUR },
                    Seller = new SellerType()
                    {
                        Name = outlet.OutletName,
                        Address = outlet.Street,
                        IDType = IDTypeSType.TIN,
                        IDNum = queueME.IssuerTIN,
                        Town = outlet.City,
                        Country = CountryCodeSType.MNE
                    },
                    Buyer = GetBuyer(request),
                    Items = GetInvoiceItems(request),
                    Fees = GetFees(invoice),
                    BadDebtInv = GetBadDebtInv(invoice)
                };
               
                if (!string.IsNullOrEmpty(invoice.TypeOfSelfiss))
                {
                    if (Enum.TryParse(invoice.TypeOfSelfiss, out SelfIssSType result))
                    {
                        throw new ArgumentException($"Unknown TypeOfSelfiss {invoice.TypeOfSelfiss}!");
                    }
                }
                registerInvoiceRequest.Invoice = invoiceType;
                registerInvoiceRequest.Signature = _signatureFactory.CreateSignature();
                var registerInvoiceResponse = await client.RegisterInvoiceAsync(registerInvoiceRequest).ConfigureAwait(false);
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

        private BadDebtInvType GetBadDebtInv(Invoice invoice)
        {
            if(invoice.BadDebt is null)
            {
                return null;
            }
            return new BadDebtInvType()
            {
                IICRef = invoice.BadDebt.IICRef,
                IssueDateTime = invoice.BadDebt.IssueDateTime
            };
        }

        private FeeType[] GetFees(Invoice invoice)
        {
            if(invoice.Fees is null)
            {
                return null;
            }
            var result = new FeeType[invoice.Fees.Count()];
            var i=0;
            foreach(var fee in invoice.Fees)
            {
                result[i] = new FeeType()
                {
                    Amt = fee.Amt,
                    Type = (FeeTypeSType) Enum.Parse(typeof(FeeTypeSType), fee.Type)
                };
            }
            return result;
        }

        private InvoiceItemType[] GetInvoiceItems(ReceiptRequest request)
        {
            if (request.cbChargeItems.Count() > 1000)
            {
                throw new MaxInvoiceItemsExceededException();
            }
            var items = new InvoiceItemType[request.cbChargeItems.Count()];
            var i = 0;
            foreach(var chargeItem in request.cbChargeItems)
            {
                items[i] = chargeItem.GetInvoiceItemType();
                i++;
            }
            return items;
        }

        private BuyerType GetBuyer(ReceiptRequest request)
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
                return new BuyerType()
                {
                    IDNum = buyer.IDNum,
                    IDType = (IDTypeSType) Enum.Parse(typeof(IDTypeSType), buyer.IDType),
                    Name = buyer.Name,
                    Address = buyer.Address,
                    Town = buyer.Town,
                    Country = (CountryCodeSType) Enum.Parse(typeof(CountryCodeSType), buyer.Country),
                };
            }
            catch (Exception ex)
            {
                throw new BuyerParseException("Error when parsing Buyer in cbCustomer field!", ex);
            }

        }
    }
}
