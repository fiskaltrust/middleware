using System;
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
                    if (item.IsPaymentAdjustment() && !item.IsMultiUseVoucher())
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
            var sumChargeItemsNoVoucher = receiptRequest.cbChargeItems.Where(x => !x.IsPaymentAdjustment()).Sum(x => x.GetAmount());

            if (receiptRequest.cbPayItems.Any(x => x.GetPaymentType() == PaymentType.Voucher) ||
                receiptRequest.cbChargeItems.Any(x => x.IsMultiUseVoucher() && x.GetAmount() < 0))
            {
                var sumVoucher = receiptRequest.cbPayItems.Where(x => x.GetPaymentType() == PaymentType.Voucher).Sum(x => x.GetAmount()) +
                    receiptRequest.cbChargeItems.Where(x => x.IsMultiUseVoucher() && x.Amount < 0).Sum(x => Math.Abs(x.Amount));
                if (sumVoucher > sumChargeItemsNoVoucher)
                {
                    var dscrPay = receiptRequest.cbPayItems.Where(x => x.GetPaymentType() == PaymentType.Voucher).Select(x => x.Description).ToList();
                    var dscrCharge = receiptRequest.cbChargeItems.Where(x => x.IsMultiUseVoucher() && x.GetAmount() < 0).Select(x => x.Description).ToList();
                    dscrPay.AddRange(dscrCharge);

                    var addiPay = receiptRequest.cbPayItems.Where(x => x.GetPaymentType() == PaymentType.Voucher).Select(x => x.ftPayItemCaseData).ToList();
                    var addiCharge = receiptRequest.cbChargeItems.Where(x => x.IsMultiUseVoucher() && x.GetAmount() < 0).Select(x => x.ftChargeItemCaseData).ToList();
                    addiPay.AddRange(addiCharge);

                    return new List<Payment>()
                    {
                        new Payment
                        {
                            Amount = sumChargeItemsNoVoucher,
                            Description = string.Join(" ",dscrPay),
                            PaymentType = PaymentType.Voucher,
                            AdditionalInformation = string.Join(" ",addiPay),
                        }
                    };
                }
            }
            var payments = receiptRequest.cbPayItems?.Select(p => new Payment
            {
                Amount = p.Amount,
                Description = p.Description,
                PaymentType = p.GetPaymentType(),
                AdditionalInformation = p.ftPayItemCaseData
            }).ToList();
            var vouchersFromChargeItms = receiptRequest.cbChargeItems.Where(x => x.IsMultiUseVoucher() && x.GetAmount() < 0).Select(ch =>
                new Payment
                {
                    Amount = Math.Abs(ch.Amount),
                    Description = ch.Description,
                    PaymentType = PaymentType.Voucher,
                    AdditionalInformation = ch.ftChargeItemCaseData
                }).ToList();
            payments.AddRange(vouchersFromChargeItms);
            return payments;
        }
    }
}
