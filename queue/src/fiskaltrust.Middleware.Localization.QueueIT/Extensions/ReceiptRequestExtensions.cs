using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.Localization.QueueIT.Extensions
{
    public static class ReceiptRequestExtensions
    {
        public static List<PaymentAdjustment> GetPaymentAdjustments(this ReceiptRequest receiptRequest)
        {
            var paymentAdjustments = new List<PaymentAdjustment>();

            foreach(var item in receiptRequest.cbChargeItems)
            {
                if (item.IsPaymentAdjustment()){
                    paymentAdjustments.Add(new PaymentAdjustment
                    {
                        Amount = item.Amount,
                        Description = item.Description,
                        VatGroup = item.GetVatRate() == 0 ? null : item.GetVatRate()
                    });
                }
            }
            return paymentAdjustments;
        }
    }
}
