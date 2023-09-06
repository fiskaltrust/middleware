using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.QueueLogic.Exceptions;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.QueueLogic.Extensions
{
    public static class ReceiptRequestV2Extensions
    {
        public static List<PaymentAdjustment> GetV2PaymentAdjustments(this ReceiptRequest receiptRequest)
        {
            var paymentAdjustments = new List<PaymentAdjustment>();

            if (receiptRequest.cbChargeItems != null)
            {
                foreach (var item in receiptRequest.cbChargeItems)
                {
                    if (item.IsV2PaymentAdjustment() && !item.IsV2MultiUseVoucherRedeem())
                    {
                        paymentAdjustments.Add(new PaymentAdjustment
                        {
                            Amount = item.GetAmount(),
                            Description = item.Description,
                            VatGroup = item.GetV2VatGroup(),
                            PaymentAdjustmentType = item.GetV2PaymentAdjustmentType(),
                            AdditionalInformation = item.ftChargeItemCaseData
                        });
                    }
                }
            }
            return paymentAdjustments;
        }

        public static List<Payment> GetV2Payments(this ReceiptRequest receiptRequest)
        {
            if (receiptRequest == null)
            {
                return new List<Payment>();
            }
            var sumChargeItems = receiptRequest.cbChargeItems?.Sum(x => x.GetAmount()) ?? 0;
            var sumPayItems = receiptRequest.cbPayItems?.Sum(x => x.GetAmount()) ?? 0;

            if (sumPayItems - sumChargeItems != 0)
            {
                throw new ItemPaymentInequalityException(sumPayItems, sumChargeItems);
            }
            var payment = receiptRequest.GetV2PaymentFullyRedeemedByVouchers();

            if (payment.Any())
            {
                return payment;
            }
            var payments = receiptRequest.cbPayItems?.Select(p => new Payment
            {
                Amount = p.Amount,
                Description = p.Description,
                PaymentType = p.GetV2PaymentType(),
                AdditionalInformation = p.ftPayItemCaseData
            }).ToList() ?? new List<Payment>();
            var vouchersFromChargeItms = receiptRequest.cbChargeItems?.Where(x => x.IsV2MultiUseVoucherRedeem()).Select(ch =>
                new Payment
                {
                    Amount = Math.Abs(ch.Amount),
                    Description = ch.Description,
                    PaymentType = PaymentType.Voucher,
                    AdditionalInformation = ch.ftChargeItemCaseData
                }).ToList() ?? new List<Payment>();
            payments.AddRange(vouchersFromChargeItms);
            return payments;
        }

        private static List<Payment> GetV2PaymentFullyRedeemedByVouchers(this ReceiptRequest receiptRequest)
        {
            if (receiptRequest == null)
            {
                return new List<Payment>();
            }
            var sumChargeItemsNoVoucher = receiptRequest.cbChargeItems?.Where(x => !x.IsV2PaymentAdjustment()).Sum(x => x.GetAmount()) ?? 0;

            var payments = new List<Payment>();
            if ((receiptRequest.cbPayItems != null && receiptRequest.cbPayItems.Any(x => x.IsV2VoucherRedeem())) ||
                (receiptRequest.cbChargeItems != null && receiptRequest.cbChargeItems.Any(x => x.IsV2MultiUseVoucherRedeem())))
            {
                var sumVoucher = receiptRequest.cbPayItems?.Where(x => x.IsV2VoucherRedeem()).Sum(x => x.GetAmount()) +
                    receiptRequest.cbChargeItems?.Where(x => x.IsV2MultiUseVoucherRedeem()).Sum(x => Math.Abs(x.Amount));
                if (sumVoucher > sumChargeItemsNoVoucher)
                {
                    var dscrPay = receiptRequest.cbPayItems?.Where(x => x.IsV2VoucherRedeem()).Select(x => x.Description).ToList() ?? new List<string>();
                    var dscrCharge = receiptRequest.cbChargeItems?.Where(x => x.IsV2MultiUseVoucherRedeem()).Select(x => x.Description).ToList() ?? new List<string>();
                    dscrPay.AddRange(dscrCharge);

                    var addiPay = receiptRequest.cbPayItems?.Where(x => x.IsV2VoucherRedeem()).Select(x => x.ftPayItemCaseData).ToList() ?? new List<string>();
                    var addiCharge = receiptRequest.cbChargeItems?.Where(x => x.IsV2MultiUseVoucherRedeem()).Select(x => x.ftChargeItemCaseData).ToList() ?? new List<string>();
                    addiPay.AddRange(addiCharge);

                    payments.Add(
                        new Payment
                        {
                            Amount = sumChargeItemsNoVoucher,
                            Description = string.Join(" ", dscrPay),
                            PaymentType = PaymentType.Voucher,
                            AdditionalInformation = string.Join(" ", addiPay),
                        });
                };
            }
            return payments;
        }
    }
}

