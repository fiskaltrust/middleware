using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueME.Extensions
{
    public static class ReceiptRequestExtensions
    {
        public static bool IsFailedReceipt(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0x0_0010_0000) == 0x0_0010_0000;
        }

        public static bool IsVoidedReceipt(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0x0_0040_0000) == 0x0_0040_0000;
        }


        public static InvoiceType GetInvoiceType(this ReceiptRequest receiptRequest)
        {
            var result = InvoiceType.Cash;

            foreach(var pay in receiptRequest.cbPayItems)
            {
                if (pay.IsNonCashLocalCurrency())
                {
                    result = InvoiceType.NonCash;
                    break;
                }
            }
            return result;
        }
        public static List<InvoicePayment> GetPaymentMethodTypes(this ReceiptRequest item)
        {
            var paymentTypes = item.cbPayItems.GroupBy(x => x.GetPaymentMethodType()).Select(x => x.Key) ;
            var result = new List<InvoicePayment>();
            foreach (var paymentType in paymentTypes)
            {
                var items = item.cbPayItems.Where(x => x.GetPaymentMethodType().Equals(paymentType));
                var invoicePayment = new InvoicePayment()
                {
                    Type = paymentType,
                    Amount = items.Sum(x => x.Amount)
                };
                if (paymentType is PaymentType.Company)
                {
                    var definition = new { CompCardNumber = "" };
                    var compCardNumbers = "";
                    foreach (var companyCard in items)
                    {
                        var compCardNumber = JsonConvert.DeserializeAnonymousType(companyCard.ftPayItemCaseData, definition);
                        compCardNumbers += compCardNumber + "/";
                    }
                    invoicePayment.CompanyCardNumber = compCardNumbers;
                }else if (paymentType is PaymentType.Voucher)
                {
                    invoicePayment.VoucherNumbers = new List<string>();
                    var definition = new { VoucherNumber = "" };
                    foreach (var voucher in items)
                    {  
                        var compCardNumber = JsonConvert.DeserializeAnonymousType(voucher.ftPayItemCaseData, definition);
                        invoicePayment.VoucherNumbers.Add(compCardNumber.VoucherNumber);
                    }
                }
                result.Add(invoicePayment);
            }
            return result;
        }
    }
}
