using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v2.me;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueME.Extensions
{
    public static class ReceiptRequestExtensions
    {

        public static InvoiceSType GetInvoiceSType(this ReceiptRequest receiptRequest)
        {
            var result = InvoiceSType.CASH;

            foreach(var pay in receiptRequest.cbPayItems)
            {
                if (pay.IsNonCashLocalCurrency())
                {
                    result = InvoiceSType.NONCASH;
                    break;
                }
            }
            return result;
        }

        public static InvoiceTSType GetInvoiceTSType(this ReceiptRequest receiptRequest)
        {
            if ((receiptRequest.ftReceiptCase & 0x0_0001_0000) == 0x0_0001_0000)
            {
                return InvoiceTSType.INVOICE;
            }
            else if ((receiptRequest.ftReceiptCase & 0x0_0002_0000) == 0x0_0002_0000)
            {
                return InvoiceTSType.CORRECTIVE;
            }
            else if ((receiptRequest.ftReceiptCase & 0x0_0003_0000) == 0x0_0003_0000)
            {
                return InvoiceTSType.SUMMARY;
            }
            else if ((receiptRequest.ftReceiptCase & 0x0_0004_0000) == 0x0_0004_0000)
            {
                return InvoiceTSType.PERIODICAL;
            }
            else if ((receiptRequest.ftReceiptCase & 0x0_0005_0000) == 0x0_0005_0000)
            {
                return InvoiceTSType.ADVANCE;
            }
            else if ((receiptRequest.ftReceiptCase & 0x0_0006_0000) == 0x0_0006_0000)
            {
                return InvoiceTSType.CORRECTIVE;
            };
            throw new UnkownInvoiceTypeException("ChargeItemCase holds unkown Invoice Type!");
        }

        public static PayMethodType[] GetPaymentMethodTypes(this ReceiptRequest item)
        {
            var payMethodTypeSs = item.cbPayItems.GroupBy(x => x.GetPaymentMethodType()).Select(x => x.Key) ;
            var result = new List<PayMethodType>();
            foreach (var payMethodTypeS in payMethodTypeSs)
            {
                var items = item.cbPayItems.Where(x => x.GetPaymentMethodType().Equals(payMethodTypeS));
                var payMethodType = new PayMethodType()
                {
                    Type = payMethodTypeS,
                    Amt = items.Sum(x => x.Amount)
                };
                if (payMethodTypeS is PaymentMethodTypeSType.COMPANY)
                {
                    var definition = new { CompCardNumber = "" };
                    var compCardNumbers = "";
                    foreach (var companyCard in items)
                    {
                        var compCardNumber = JsonConvert.DeserializeAnonymousType(companyCard.ftPayItemCaseData, definition);
                        compCardNumbers += compCardNumber + "/";
                    }
                    payMethodType.CompCard = compCardNumbers;
                }else if (payMethodTypeS is PaymentMethodTypeSType.SVOUCHER)
                {
                    var definition = new { VoucherNumber = "" };
                    var voucherTypes = new List<VoucherType>();
                    foreach (var voucher in items)
                    {  
                        var compCardNumber = JsonConvert.DeserializeAnonymousType(voucher.ftPayItemCaseData, definition);
                        voucherTypes.Add(new VoucherType()
                        {
                            Num = compCardNumber.VoucherNumber
                        });
                    }
                    payMethodType.Vouchers = voucherTypes.ToArray();
                }
                result.Add(payMethodType);
            }
            return result.ToArray();
        }
    }
}
