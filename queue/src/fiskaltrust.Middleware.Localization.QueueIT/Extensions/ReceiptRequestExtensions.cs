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

            if (receiptRequest.cbChargeItems != null)
            {
                foreach (var item in receiptRequest.cbChargeItems)
                {
                    if (item.IsPaymentAdjustment())
                    {
                        paymentAdjustments.Add(new PaymentAdjustment
                        {
                            Amount = item.GetAmount(),
                            Description = item.Description,
                            VatGroup = item.GetVatGroup(),
                            PaymentAdjustmentType = (PaymentAdjustmentType) item.GetPaymentAdjustmentType(),
                            AdditionalInformation = item.ftChargeItemCaseData
                        });
                    }
                }
            }
            return paymentAdjustments;
        }
    }
}
