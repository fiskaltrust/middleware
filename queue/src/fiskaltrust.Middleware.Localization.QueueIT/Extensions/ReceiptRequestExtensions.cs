using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http.Headers;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Localization.QueueIT.Exceptions;

namespace fiskaltrust.Middleware.Localization.QueueIT.Extensions
{
    public static class ReceiptRequestExtensions
    {
        public static bool IsMultiUseVoucherSale(this ReceiptRequest receiptRequest)
        {
            var hasChargeItemVoucher = receiptRequest?.cbChargeItems?.Any(x => x.IsMultiUseVoucherSale()) ?? false;
            var hasPayItemVoucher = receiptRequest?.cbPayItems?.Any(x => x.IsVoucherSale()) ?? false;

            if(hasChargeItemVoucher || hasPayItemVoucher)
            {
                if(receiptRequest?.cbChargeItems?.Any(x => !x.IsPaymentAdjustment()) ?? false)
                {
                    throw new MultiUseVoucherNoSaleException();
                }
                return true;
            }
            return false;
        }

        public static List<PaymentAdjustment> GetPaymentAdjustments(this ReceiptRequest receiptRequest)
        {
            var paymentAdjustments = new List<PaymentAdjustment>();

            if (receiptRequest.cbChargeItems != null)
            {
                foreach (var item in receiptRequest.cbChargeItems)
                {
                    if (item.IsPaymentAdjustment() && !item.IsMultiUseVoucherRedeem())
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
            var sumChargeItems = receiptRequest.cbChargeItems?.Sum(x => x.GetAmount()) ?? 0;
            var sumPayItems = receiptRequest.cbPayItems?.Sum(x => x.GetAmount()) ?? 0;

            if ((sumPayItems - sumChargeItems) != 0)
            {
                throw new ItemPaymentInequalityException(sumPayItems, sumChargeItems);
            }
            var payment = receiptRequest.GetPaymentFullyRedeemedByVouchers();

            if (payment.Any())
            {
                return payment;
            }
            var payments = receiptRequest.cbPayItems?.Select(p => new Payment
            {
                Amount = p.Amount,
                Description = p.Description,
                PaymentType = p.GetPaymentType(),
                AdditionalInformation = p.ftPayItemCaseData
            }).ToList() ?? new List<Payment>();
            var vouchersFromChargeItms = receiptRequest.cbChargeItems.Where(x => x.IsMultiUseVoucherRedeem()).Select(ch =>
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

        private static List<Payment> GetPaymentFullyRedeemedByVouchers(this ReceiptRequest receiptRequest)
        {
            var sumChargeItemsNoVoucher = receiptRequest.cbChargeItems?.Where(x => !x.IsPaymentAdjustment()).Sum(x => x.GetAmount()) ?? 0;

            var payments = new List<Payment>();
            if ((receiptRequest.cbPayItems != null && receiptRequest.cbPayItems.Any(x => x.IsVoucherRedeem())) ||
                (receiptRequest.cbChargeItems != null && receiptRequest.cbChargeItems.Any(x => x.IsMultiUseVoucherRedeem())))
            {
                var sumVoucher = receiptRequest.cbPayItems.Where(x => x.IsVoucherRedeem()).Sum(x => x.GetAmount()) +
                    receiptRequest.cbChargeItems.Where(x => x.IsMultiUseVoucherRedeem()).Sum(x => Math.Abs(x.Amount));
                if (sumVoucher > sumChargeItemsNoVoucher)
                {
                    var dscrPay = receiptRequest.cbPayItems.Where(x => x.IsVoucherRedeem()).Select(x => x.Description).ToList();
                    var dscrCharge = receiptRequest.cbChargeItems.Where(x => x.IsMultiUseVoucherRedeem()).Select(x => x.Description).ToList();
                    dscrPay.AddRange(dscrCharge);

                    var addiPay = receiptRequest.cbPayItems.Where(x => x.IsVoucherRedeem()).Select(x => x.ftPayItemCaseData).ToList();
                    var addiCharge = receiptRequest.cbChargeItems.Where(x => x.IsMultiUseVoucherRedeem()).Select(x => x.ftChargeItemCaseData).ToList();
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

