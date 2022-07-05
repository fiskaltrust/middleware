using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueME.Extensions
{
    public static class ReceiptRequestExtensions
    {
        public static bool IsVoidedComplete(this ReceiptRequest receiptRequest)
        {
            return (receiptRequest.ftReceiptCase & 0x0000_0000_000F_0000) == 0x4_0000;
        }

        public static bool IsVoidedPartial(this ReceiptRequest receiptRequest)
        {
            return (receiptRequest.ftReceiptCase & 0x0000_0000_000F_0000) == 0x5_0000;
        }

        public static InvoiceType GetInvoiceType(this ReceiptRequest receiptRequest)
        {
            var result = InvoiceType.Cash;
            if (receiptRequest.cbPayItems.Any(pay => pay.IsNonCashLocalCurrency()))
            {
                result = InvoiceType.NonCash;
            }
            return result;
        }
        public static List<InvoicePayment> GetPaymentMethodTypes(this ReceiptRequest item, bool isVoid)
        {
            var paymentTypes = item.cbPayItems.GroupBy(x => x.GetPaymentMethodType()).Select(x => x.Key) ;
            var result = new List<InvoicePayment>();
            foreach (var paymentType in paymentTypes)
            {
                var items = item.cbPayItems.Where(x => x.GetPaymentMethodType().Equals(paymentType)).ToList();
                var invoicePayment = new InvoicePayment
                {
                    Type = paymentType,
                    Amount = items.Sum(x => x.Amount)
                };
                if (isVoid)
                {
                    invoicePayment.Amount *= -1;
                }
                switch (paymentType)
                {
                    case PaymentType.Company:
                    {
                        var definition = new { CompCardNumber = "" };
                        var compCardNumbers = items.Select(companyCard => JsonConvert.DeserializeAnonymousType(companyCard.ftPayItemCaseData, definition)).Aggregate("", (current, compCardNumber) => current + compCardNumber + "/");
                        invoicePayment.CompanyCardNumber = compCardNumbers;
                        break;
                    }
                    case PaymentType.Voucher:
                    {
                        invoicePayment.VoucherNumbers = new List<string>();
                        var definition = new { VoucherNumber = "" };
                        foreach (var compCardNumber in items.Select(voucher => JsonConvert.DeserializeAnonymousType(voucher.ftPayItemCaseData, definition)))
                        {
                            invoicePayment.VoucherNumbers.Add(compCardNumber.VoucherNumber);
                        }
                        break;
                    }
                }
                result.Add(invoicePayment);
            }
            return result;
        }
    }
}
