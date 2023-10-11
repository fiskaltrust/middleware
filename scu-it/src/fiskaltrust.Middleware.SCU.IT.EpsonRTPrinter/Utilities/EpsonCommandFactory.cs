using System;
using System.Linq;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Extensions;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using System.Collections.Generic;

#pragma warning disable

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities
{
    public class EpsonCommandFactory
    {
        public static FiscalReceipt CreateInvoiceRequestContent(ReceiptRequest receiptRequest)
        {
            // TODO check for lottery ID
            var fiscalReceipt = new FiscalReceipt();
            fiscalReceipt.ItemAndMessages = GetItemAndMessages(receiptRequest);
            fiscalReceipt.AdjustmentAndMessages = new List<AdjustmentAndMessage>();
            fiscalReceipt.RecTotalAndMessages = GetTotalAndMessages(receiptRequest);
            var customerData = receiptRequest.GetCustomer();
            if (customerData != null)
            {
                if (!string.IsNullOrEmpty(customerData.CustomerVATId))
                {
                    var vat = customerData.CustomerVATId!;
                    if (vat.ToUpper().StartsWith("IT"))
                    {
                        vat = vat.Substring(2);
                    }
                    if (vat.Length == 11)
                    {
                        fiscalReceipt.DirectIOCommands.Add(new DirectIO
                        {
                            Command = "1060",
                            Data = "01" + vat,
                        });
                    }
                }
            }
            return fiscalReceipt;
        }

        public static FiscalReceipt CreateRefundRequestContent(ReceiptRequest receiptRequest, long referenceDocNumber, long referenceZNumber, DateTime referenceDateTime, string serialNr)
        {
            return new FiscalReceipt
            {
                PrintRecMessage = new List<PrintRecMessage>
                {
                    new PrintRecMessage()
                    {
                        Message = $"REFUND {referenceZNumber:D4} {referenceDocNumber:D4} {referenceDateTime:ddMMyyyy} {serialNr}",
                        MessageType = (int) Messagetype.AdditionalInfo
                    }
                },
                PrintRecRefund = GetRecRefunds(receiptRequest),
                AdjustmentAndMessages = new List<AdjustmentAndMessage>(),
                RecTotalAndMessages = GetTotalAndMessages(receiptRequest)
            };
        }

        public static FiscalReceipt CreateVoidRequestContent(ReceiptRequest receiptRequest, long referenceDocNumber, long referenceZNumber, DateTime referenceDateTime, string serialNr)
        {
            return new FiscalReceipt
            {
                PrintRecMessage = new List<PrintRecMessage>
                {
                   new PrintRecMessage()
                    {
                        Message = $"VOID {referenceZNumber:D4} {referenceDocNumber:D4} {referenceDateTime:ddMMyyyy} {serialNr}",
                        MessageType = (int) Messagetype.AdditionalInfo
                    }
                },
                PrintRecVoid = GetRecvoids(receiptRequest),
                AdjustmentAndMessages = new List<AdjustmentAndMessage>(),
                RecTotalAndMessages = GetTotalAndMessages(receiptRequest)
            };
        }

        public static List<PrintRecRefund> GetRecRefunds(ReceiptRequest receiptRequest)
        {
            return receiptRequest.cbChargeItems?.Select(p => new PrintRecRefund
            {
                Description = p.Description,
                Quantity = Math.Abs(p.Quantity),
                UnitPrice = Math.Abs(p.Amount) / Math.Abs(p.Quantity),
                Amount = Math.Abs(p.Amount),
                Department = p.GetVatGroup()
            }).ToList();
        }

        public static List<PrintRecVoid> GetRecvoids(ReceiptRequest receiptRequest)
        {
            return receiptRequest.cbChargeItems?.Select(p => new PrintRecVoid
            {
                Description = p.Description,
                Quantity = Math.Abs(p.Quantity),
                UnitPrice = Math.Abs(p.Amount) / Math.Abs(p.Quantity),
                Amount = Math.Abs(p.Amount),
                Department = p.GetVatGroup()
            }).ToList();
        }

        public static List<ItemAndMessage> GetItemAndMessages(ReceiptRequest receiptRequest)
        {
            var itemAndMessages = new List<ItemAndMessage>();
            // Todo handle payment adjustments / discounts
            foreach (var i in receiptRequest.cbChargeItems)
            {
                var printRecItem = new PrintRecItem
                {
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.Amount / i.Quantity,
                    Department = i.GetVatGroup(),
                };
                PrintRecMessage? printRecMessage = null;
                if (!string.IsNullOrEmpty(i.ftChargeItemCaseData))
                {
                    printRecMessage = new PrintRecMessage()
                    {
                        Message = i.ftChargeItemCaseData,
                        MessageType = 4
                    };
                }
                itemAndMessages.Add(new() { PrintRecItem = printRecItem, PrintRecMessage = printRecMessage });
            }
            return itemAndMessages;
        }

        public static List<TotalAndMessage> GetTotalAndMessages(ReceiptRequest request)
        {
            var totalAndMessages = new List<TotalAndMessage>();
            foreach (var pay in request.cbPayItems)
            {
                var paymentType = GetEpsonPaymentType(pay);
                var printRecTotal = new PrintRecTotal
                {
                    Description = pay.Description,
                    PaymentType = paymentType.PaymentType,
                    Index = paymentType.Index,
                    Payment = (request.IsRefund() || request.IsVoid() || pay.IsRefund() || pay.IsVoid()) ? Math.Abs(pay.Amount) : pay.Amount,
                };
                PrintRecMessage? printRecMessage = null;
                if (!string.IsNullOrEmpty(pay.ftPayItemCaseData))
                {
                    printRecMessage = new PrintRecMessage()
                    {
                        Message = pay.ftPayItemCaseData,
                        MessageType = 4
                    };
                }
                totalAndMessages.Add(new()
                {
                    PrintRecTotal = printRecTotal,
                    PrintRecMessage = printRecMessage
                });
            }
            return totalAndMessages;
        }

        public struct EpsonPaymentType
        {
            public int PaymentType;
            public int Index;
        }

        public static EpsonPaymentType GetEpsonPaymentType(PayItem payItem)
        {
            return (payItem.ftPayItemCase & 0xFF) switch
            {
                0x00 => new EpsonPaymentType() { PaymentType = 0, Index = 0 },
                0x01 => new EpsonPaymentType() { PaymentType = 0, Index = 0 },
                0x02 => new EpsonPaymentType() { PaymentType = 0, Index = 0 },
                0x03 => new EpsonPaymentType() { PaymentType = 1, Index = 0 },
                0x04 => new EpsonPaymentType() { PaymentType = 2, Index = 1 },
                0x05 => new EpsonPaymentType() { PaymentType = 2, Index = 1 },
                0x06 => new EpsonPaymentType() { PaymentType = 3, Index = 1 },
                0x07 => new EpsonPaymentType() { PaymentType = 5, Index = 0 },
                0x08 => new EpsonPaymentType() { PaymentType = 5, Index = 0 },
                0x09 => new EpsonPaymentType() { PaymentType = 5, Index = 0 },
                0x0A => new EpsonPaymentType() { PaymentType = 2, Index = 1 },
                0x0B => new EpsonPaymentType() { PaymentType = 2, Index = 1 },
                0x0C => new EpsonPaymentType() { PaymentType = 0, Index = 0 },
                0x0D => new EpsonPaymentType() { PaymentType = 5, Index = 0 },
                0x0E => new EpsonPaymentType() { PaymentType = 5, Index = 0 },
                _ => throw new NotSupportedException($"The payitemcase {payItem.ftPayItemCase} is not supported")
            };
        }
    }
}