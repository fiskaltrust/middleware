using System;
using System.Linq;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Extensions;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities
{
    public class EpsonCommandFactory
    {
        public static FiscalReceipt CreateInvoiceRequestContent(ReceiptRequest receiptRequest)
        {
            // TODO check for lottery ID
            var fiscalReceipt = new FiscalReceipt();
            var items = receiptRequest.cbChargeItems.Where(x => !x.IsV2PaymentAdjustment()).Select(p => new Item
            {
                Description = p.Description,
                Quantity = p.Quantity,
                UnitPrice = p.UnitPrice ?? p.Amount / p.Quantity,
                Amount = p.Amount,
                VatGroup = p.GetVatGroup(),
                AdditionalInformation = p.ftChargeItemCaseData
            }).ToList();
            fiscalReceipt.ItemAndMessages = GetItemAndMessages(items);
            fiscalReceipt.AdjustmentAndMessages = GetAdjustmentAndMessages(GetV2PaymentAdjustments(receiptRequest));
            fiscalReceipt.RecTotalAndMessages = GetTotalAndMessages(GetV2Payments(receiptRequest));
            var customerData = receiptRequest.GetCustomer();
            if (customerData != null)
            {
                fiscalReceipt.DirectIOCommands.Add(new DirectIO
                {
                    Command = "1060",
                    Data = "01" + customerData.CustomerVATId,
                });
            }
            return fiscalReceipt;
        }

        public static List<Payment> GetV2Payments(ReceiptRequest receiptRequest)
        {
            var payment = GetV2PaymentFullyRedeemedByVouchers(receiptRequest);
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

        public static FiscalReceipt CreateRefundRequestContent(ReceiptRequest receiptRequest, long referenceDocNumber, long referenceZNumber, DateTime referenceDateTime, string serialNr)
        {
            var refunds = receiptRequest.cbChargeItems?.Select(p => new Refund
            {
                Description = p.Description,
                Quantity = Math.Abs(p.Quantity),
                UnitPrice = Math.Abs(p.Amount) / Math.Abs(p.Quantity),
                Amount = Math.Abs(p.Amount),
                VatGroup = p.GetVatGroup()
            }).ToList() ?? new List<Refund>();
            var payments = receiptRequest.cbPayItems?.Select(p => new Payment
            {
                Amount = Math.Abs(p.Amount),
                Description = p.Description,
                PaymentType = p.GetV2PaymentType(),
            }).ToList() ?? new List<Payment>();
            var fiscalReceipt = new FiscalReceipt
            {
                PrintRecMessage = new List<PrintRecMessage>
                {
                    new PrintRecMessage()
                    {
                        Message = $"REFUND {referenceZNumber:D4} {referenceDocNumber:D4} {referenceDateTime:ddMMyyyy} {serialNr}",
                        MessageType = (int) Messagetype.AdditionalInfo
                    }
                },
                PrintRecRefund = refunds.Select(recRefund => new PrintRecRefund
                {
                    Description = recRefund.Description,
                    Quantity = recRefund.Quantity,
                    UnitPrice = recRefund.UnitPrice,
                    Department = recRefund.VatGroup
                }).ToList(),
                AdjustmentAndMessages = GetAdjustmentAndMessages(GetV2PaymentAdjustments(receiptRequest)),
                RecTotalAndMessages = GetTotalAndMessages(payments) 
            };
            return fiscalReceipt;
        }

        public static FiscalReceipt CreateVoidRequestContent(ReceiptRequest receiptRequest, long referenceDocNumber, long referenceZNumber, DateTime referenceDateTime, string serialNr)
        {
            var refunds = receiptRequest.cbChargeItems?.Select(p => new Refund
            {
                Description = p.Description,
                Quantity = Math.Abs(p.Quantity),
                UnitPrice = Math.Abs(p.Amount) / Math.Abs(p.Quantity),
                Amount = Math.Abs(p.Amount),
                VatGroup = p.GetVatGroup()
            }).ToList() ?? new List<Refund>();
            var payments = receiptRequest.cbPayItems?.Select(p => new Payment
            {
                Amount = Math.Abs(p.Amount),
                Description = p.Description,
                PaymentType = p.GetV2PaymentType(),
            }).ToList() ?? new List<Payment>();
            var fiscalReceipt = new FiscalReceipt
            {
                PrintRecMessage = new List<PrintRecMessage>
                {
                   new PrintRecMessage()
                    {
                        Message = $"VOID {referenceZNumber:D4} {referenceDocNumber:D4} {referenceDateTime:ddMMyyyy} {serialNr}",
                        MessageType = (int) Messagetype.AdditionalInfo
                    }
                },
                PrintRecVoid = refunds.Select(recRefund => new PrintRecVoid
                {
                    Description = recRefund.Description,
                    Quantity = recRefund.Quantity,
                    UnitPrice = recRefund.UnitPrice,
                    Department = recRefund.VatGroup
                }).ToList(),
                AdjustmentAndMessages = GetAdjustmentAndMessages(GetV2PaymentAdjustments(receiptRequest)),
                RecTotalAndMessages = GetTotalAndMessages(payments)
            };
            return fiscalReceipt;
        }

        public static List<AdjustmentAndMessage> GetAdjustmentAndMessages(List<PaymentAdjustment> paymentAdjustments)
        {
            var adjustmentAndMessages = new List<AdjustmentAndMessage>();
            if (paymentAdjustments != null)
            {
                foreach (var adj in paymentAdjustments)
                {
                    var printRecItemAdjustment = new PrintRecItemAdjustment
                    {
                        Description = adj.Description,
                        AdjustmentType = GetAdjustmentType(adj.PaymentAdjustmentType, adj.Amount),
                        Amount = Math.Abs(adj.Amount),
                        Department = adj.VatGroup ?? 0,
                    };
                    PrintRecMessage? printRecMessage = null;
                    if (!string.IsNullOrEmpty(adj.AdditionalInformation))
                    {
                        printRecMessage = new PrintRecMessage()
                        {
                            Message = adj.AdditionalInformation,
                            MessageType = 4
                        };
                    }
                    adjustmentAndMessages.Add(new()
                    {
                        PrintRecItemAdjustment = printRecItemAdjustment,
                        PrintRecMessage = printRecMessage
                    });
                }
            }
            return adjustmentAndMessages;
        }

        public static int GetAdjustmentType(PaymentAdjustmentType paymentAdjustmentType, decimal amount)
        {
            return paymentAdjustmentType switch
            {
                PaymentAdjustmentType.Adjustment => amount < 0 ? 3 : 8,
                PaymentAdjustmentType.SingleUseVoucher => 12,
                PaymentAdjustmentType.FreeOfCharge => 11,
                PaymentAdjustmentType.Acconto => 10,
                _ => 0,
            };
        }

        public static List<ItemAndMessage> GetItemAndMessages(List<Item> items)
        {
            var itemAndMessages = new List<ItemAndMessage>();
            foreach (var i in items)
            {
                var printRecItem = new PrintRecItem
                {
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Department = i.VatGroup
                };
                PrintRecMessage? printRecMessage = null;
                if (!string.IsNullOrEmpty(i.AdditionalInformation))
                {
                    printRecMessage = new PrintRecMessage()
                    {
                        Message = i.AdditionalInformation,
                        MessageType = 4
                    };
                }
                itemAndMessages.Add(new() { PrintRecItem = printRecItem, PrintRecMessage = printRecMessage });
            }
            return itemAndMessages;
        }

        public static List<PaymentAdjustment> GetV2PaymentAdjustments(ReceiptRequest receiptRequest)
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
                            VatGroup = item.GetVatGroup(),
                            PaymentAdjustmentType = item.GetV2PaymentAdjustmentType(),
                            AdditionalInformation = item.ftChargeItemCaseData
                        });
                    }
                }
            }
            return paymentAdjustments;
        }

        private static List<Payment> GetV2PaymentFullyRedeemedByVouchers(ReceiptRequest receiptRequest)
        {
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

        public static List<TotalAndMessage> GetTotalAndMessages(List<Payment> payments)
        {
            var totalAndMessages = new List<TotalAndMessage>();
            if (payments != null)
            {
                foreach (var pay in payments)
                {
                    var paymentType = GetEpsonPaymentType(pay.PaymentType);
                    var printRecTotal = new PrintRecTotal
                    {
                        Description = pay.Description,
                        PaymentType = paymentType.PaymentType,
                        Index = paymentType.Index,
                        Payment = pay.Amount
                    };
                    PrintRecMessage? printRecMessage = null;
                    if (!string.IsNullOrEmpty(pay.AdditionalInformation))
                    {
                        printRecMessage = new PrintRecMessage()
                        {
                            Message = pay.AdditionalInformation,
                            MessageType = 4
                        };
                    }
                    totalAndMessages.Add(new()
                    {
                        PrintRecTotal = printRecTotal,
                        PrintRecMessage = printRecMessage
                    });
                }
            }
            if (totalAndMessages.Count == 0)
            {
                totalAndMessages.Add(new()
                {
                    PrintRecTotal = new PrintRecTotal()
                    {
                        Description = PaymentType.Cash.ToString(),
                        PaymentType = (int) PaymentType.Cash,
                        Payment = 0
                    },
                    PrintRecMessage = null
                });
            }
            return totalAndMessages;
        }

        public struct EpsonPaymentType
        {
            public int PaymentType;
            public int Index;
        }

        public static EpsonPaymentType GetEpsonPaymentType(PaymentType paymentType)
        {
            return paymentType switch
            {
                PaymentType.Cheque => new EpsonPaymentType() { PaymentType = 1, Index = 0 },
                PaymentType.CreditCard => new EpsonPaymentType() { PaymentType = 2, Index = 1 },
                PaymentType.Ticket => new EpsonPaymentType() { PaymentType = 3, Index = 1 },
                PaymentType.MultipleTickets => new EpsonPaymentType() { PaymentType = 4, Index = 0 },
                PaymentType.NotPaid => new EpsonPaymentType() { PaymentType = 5, Index = 0 },
                PaymentType.Voucher => new EpsonPaymentType() { PaymentType = 6, Index = 1 },
                PaymentType.PaymentDiscount => new EpsonPaymentType() { PaymentType = 6, Index = 0 },
                _ => new EpsonPaymentType() { PaymentType = 0, Index = 0 },
            };
        }
    }
}
