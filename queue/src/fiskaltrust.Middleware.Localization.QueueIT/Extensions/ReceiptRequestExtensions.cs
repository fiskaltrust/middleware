using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Localization.QueueIT.Exceptions;

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

        public static List<Payment> GetPayments(this ReceiptRequest receiptRequest)
        {
            var sumChargeItems = receiptRequest.cbChargeItems.Sum(x => x.GetAmount());
            var sumPayItems = receiptRequest.cbPayItems.Sum(x => x.GetAmount());

            if ((sumPayItems - sumChargeItems) != 0)
            {
                throw new ItemPaymentInequalityException(sumPayItems, sumChargeItems);
            }

            if (receiptRequest.cbPayItems.Any(x => x.GetPaymentType() == PaymentType.Voucher))
            {
                var sumVoucher = receiptRequest.cbPayItems.Where(x => x.GetPaymentType() == PaymentType.Voucher).Sum(x => x.GetAmount());
                if (sumVoucher > sumChargeItems)
                {
                    return new List<Payment>()
                    {
                        new Payment
                        {
                            Amount = sumChargeItems,
                            Description = string.Join(" ", receiptRequest.cbPayItems.Where(x => x.GetPaymentType() == PaymentType.Voucher).Select(x => x.Description)),
                            PaymentType = PaymentType.Voucher,
                            AdditionalInformation = string.Join(" ", receiptRequest.cbPayItems.Where(x => x.GetPaymentType() == PaymentType.Voucher).Select(x => x.ftPayItemCaseData))
                        }
                    };
                }
            }
            return receiptRequest.cbPayItems?.Select(p => new Payment
            {
                Amount = p.Amount,
                Description = p.Description,
                PaymentType = p.GetPaymentType(),
                AdditionalInformation = p.ftPayItemCaseData
            }).ToList();
        }
    }
}
