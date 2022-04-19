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

        private readonly string errorInvNum = $"Incorrect Invoice Number (ReceiptReference) {0}! Expected: invoice ordinal number, year of issuing. Ex: pp123pp123/9934/2019/ab123ab123";
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

                var invOrdNum = GetOrdNum(request.cbReceiptReference);
                var totPRice = request.cbChargeItems.Sum(x => x.Amount);
                var registerInvoiceRequest = new RegisterInvoiceRequest();


                var invoiceType = new InvoiceType()
                {
                    InvType = InvoiceTSType.INVOICE,
                    //isIsSimplifiedInv?
                    IssueDateTime = request.cbReceiptMoment,
                    InvNum = request.cbReceiptReference,
                    InvOrdNum = invOrdNum,
                    TCRCode = queueME.TCRCode,
                    IsIssuerInVAT = invoice.IsIssuerInVAT,
                    TaxFreeAmt = request.cbChargeItems.Where(x => x.VATRate.Equals(0)).Sum(x => x.Amount),
                    MarkUpAmt = invoice.MarkUpAmt,
                    GoodsExAmt = invoice.GoodsExAmt,
                    TotPriceWoVAT = request.cbChargeItems.Sum(x => x.Amount - x.VATAmount).Value,
                    TotVATAmt = request.cbChargeItems.Sum(x => x.VATAmount).Value,
                    TotPrice = request.cbChargeItems.Sum(x => x.Amount),
                    OperatorCode = invoice.OperatorCode,
                    BusinUnitCode = queueME.BusinUnitCode,
                    SoftCode = queueME.SoftCode,
                    IIC = _signatureFactory.ICCConcatenate(queueME.IssuerTIN, request.cbReceiptMoment, invOrdNum, queueME.BusinUnitCode, queueME.TCRCode, queueME.SoftCode, totPRice),
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

        private int GetOrdNum(string invNum)
        {
            try
            {
                var temp = invNum.Split('/');
                return int.Parse(temp[0]);
            }
            catch (Exception ex)
            {
                throw new InvoiceNumIncorrectException(string.Format(errorInvNum, invNum), ex);
            }
        }

        private BuyerType GetBuyer(ReceiptRequest request)
        {
            if (string.IsNullOrEmpty(request.cbCustomer))
            {
                return null;
            }
            try
            {
                var buyer = JsonConvert.DeserializeObject<Buyer>(request.ftReceiptCaseData);
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
