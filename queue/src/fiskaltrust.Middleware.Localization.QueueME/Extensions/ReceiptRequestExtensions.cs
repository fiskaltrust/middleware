using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v2.me;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueME.Extensions
{
    public static class ReceiptRequestExtensions
    {
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
                    var company = items.FirstOrDefault();
                    var compCardNumber = JsonConvert.DeserializeAnonymousType(company.ftPayItemCaseData, definition);
                    payMethodType.CompCard = compCardNumber.CompCardNumber;
                }else if (payMethodTypeS is PaymentMethodTypeSType.SVOUCHER)
                {
                    var voucherTypes = new List<VoucherType>();
                    foreach (var voucher in items)
                    {
                        var definition = new { VoucherNumber = "" };
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
