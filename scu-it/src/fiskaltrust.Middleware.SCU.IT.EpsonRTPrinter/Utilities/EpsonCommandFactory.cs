using System;
using System.Linq;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using System.Collections.Generic;
using Newtonsoft.Json;

#pragma warning disable

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities
{
    public static class EpsonCommandFactory
    {
        public static FiscalReceipt CreateInvoiceRequestContent(EpsonRTPrinterSCUConfiguration configuration, ReceiptRequest receiptRequest)
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
            AddTrailerLines(configuration, receiptRequest, fiscalReceipt);
            return fiscalReceipt;
        }

        private static void AddTrailerLines(EpsonRTPrinterSCUConfiguration configuration, ReceiptRequest receiptRequest, FiscalReceipt fiscalReceipt)
        {
            var index = 1;
            if (string.IsNullOrWhiteSpace(configuration.AdditionalTrailerLines))
                return;

            var lines = JsonConvert.DeserializeObject<List<string>>(configuration.AdditionalTrailerLines);
            foreach (var trailerLine in lines)
            {
                var data = trailerLine.Replace("{cbArea}", receiptRequest.cbArea).Replace("{cbUser}", receiptRequest.cbUser);
                fiscalReceipt.PrintRecMessageType3?.Add(new PrintRecMessage
                {
                    MessageType = 3,
                    Index = index.ToString(),
                    Font = "1",
                    Message = data
                });
                index++;
            }
        }

        public static FiscalReceipt CreateRefundRequestContent(EpsonRTPrinterSCUConfiguration configuration, ReceiptRequest receiptRequest, long referenceDocNumber, long referenceZNumber, DateTime referenceDateTime, string serialNr)
        {
            var fiscalReceipt = new FiscalReceipt
            {
                PrintRecMessageType4 = new List<PrintRecMessage>
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
            AddTrailerLines(configuration, receiptRequest, fiscalReceipt);
            return fiscalReceipt;
        }

        public static FiscalReceipt CreateVoidRequestContent(EpsonRTPrinterSCUConfiguration configuration, ReceiptRequest receiptRequest, long referenceDocNumber, long referenceZNumber, DateTime referenceDateTime, string serialNr)
        {
            var fiscalReceipt = new FiscalReceipt
            {
                PrintRecMessageType4 = new List<PrintRecMessage>
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
            AddTrailerLines(configuration, receiptRequest, fiscalReceipt);
            return fiscalReceipt;
        }

        public static List<PrintRecRefund> GetRecRefunds(ReceiptRequest receiptRequest)
        {
            return receiptRequest.cbChargeItems?.Select(p => new PrintRecRefund
            {
                Description = p.Description,
                Quantity = Math.Abs(p.Quantity),
                UnitPrice = p.Quantity == 0 || p.Amount == 0 ? 0 : Math.Abs(p.Amount) / Math.Abs(p.Quantity),
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
                UnitPrice = p.Quantity == 0 || p.Amount == 0 ? 0 : Math.Abs(p.Amount) / Math.Abs(p.Quantity),
                Amount = Math.Abs(p.Amount),
                Department = p.GetVatGroup()
            }).ToList();
        }

        public static List<ItemAndMessage> GetItemAndMessages(ReceiptRequest receiptRequest)
        {
            var itemAndMessages = new List<ItemAndMessage>();
            if (receiptRequest.IsGroupingRequest())
            {
                var chargeItemGroups = receiptRequest.cbChargeItems.GroupBy(x => x.Position / 100);
                foreach (var chargeItemGroup in chargeItemGroups)
                {
                    var mainItem = chargeItemGroup.FirstOrDefault(x => x.Position % 100 == 0);
                    if (mainItem.Quantity == 0 || mainItem.Amount == 0)
                    {
                        itemAndMessages.Add(new()
                        {
                            PrintRecMessage = new PrintRecMessage()
                            {
                                Message = mainItem.Description,
                                MessageType = 4
                            }
                        });

                        foreach (var chargeItem in chargeItemGroup.Where(x => x != mainItem))
                        {
                            if (chargeItem.Amount == 0 || chargeItem.Quantity == 0)
                            {
                                itemAndMessages.Add(new()
                                {
                                    PrintRecMessage = new PrintRecMessage()
                                    {
                                        Message = chargeItem.Description,
                                        MessageType = 4
                                    }
                                });
                            }
                            else
                            {
                                if (chargeItem.Amount < 0)
                                {
                                    itemAndMessages.Add(new()
                                    {
                                        PrintRecVoidItem = new PrintRecVoidItem()
                                        {
                                            Description = chargeItem.Description,
                                            Quantity = Math.Abs(chargeItem.Quantity),
                                            UnitPrice = chargeItem.Quantity == 0 || chargeItem.Amount == 0 ? 0 : Math.Abs(chargeItem.Amount) / Math.Abs(chargeItem.Quantity),
                                            Department = chargeItem.GetVatGroup()
                                        }
                                    });
                                }
                                else
                                {
                                    GenerateItems(itemAndMessages, chargeItem);
                                }
                            }
                        }
                    }
                    else
                    {
                        GenerateItems(itemAndMessages, mainItem);
                        foreach (var chargeItem in chargeItemGroup.Where(x => x != mainItem))
                        {
                            if (chargeItem.Amount == 0 || chargeItem.Quantity == 0)
                            {
                                itemAndMessages.Add(new()
                                {
                                    PrintRecMessage = new PrintRecMessage()
                                    {
                                        Message = chargeItem.Description,
                                        MessageType = 4
                                    }
                                });
                            }
                            else
                            {
                                if (chargeItem.Amount < 0)
                                {
                                    itemAndMessages.Add(new()
                                    {
                                        PrintRecVoidItem = new PrintRecVoidItem()
                                        {
                                            Description = chargeItem.Description,
                                            Quantity = Math.Abs(chargeItem.Quantity),
                                            UnitPrice = chargeItem.Quantity == 0 || chargeItem.Amount == 0 ? 0 : Math.Abs(chargeItem.Amount) / Math.Abs(chargeItem.Quantity),
                                            Department = chargeItem.GetVatGroup()
                                        }
                                    });


                                    //itemAndMessages.Add(new()
                                    //{
                                    //    PrintRecItemAdjustment = new PrintRecItemAdjustment()
                                    //    {
                                    //        AdjustmentType = 0,
                                    //        Amount = Math.Abs(chargeItem.Amount),
                                    //        Department = chargeItem.GetVatGroup(),
                                    //        Description = chargeItem.Description
                                    //    }
                                    //});
                                }
                                else
                                {
                                    itemAndMessages.Add(new()
                                    {
                                        PrintRecItemAdjustment = new PrintRecItemAdjustment()
                                        {
                                            AdjustmentType = 5,
                                            Amount = chargeItem.Amount,
                                            Department = chargeItem.GetVatGroup(),
                                            Description = chargeItem.Description
                                        }
                                    });
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Todo handle payment adjustments / discounts
                foreach (var i in receiptRequest.cbChargeItems)
                {
                    GenerateItems(itemAndMessages, i);
                }
            }
            return itemAndMessages;
        }

        private static void GenerateItems(List<ItemAndMessage> itemAndMessages, ChargeItem? i)
        {
            if (i.Amount == 0 || i.Quantity == 0)
            {
                itemAndMessages.Add(new()
                {
                    PrintRecMessage = new PrintRecMessage()
                    {
                        Message = i.Description,
                        MessageType = 4
                    }
                });
            }
            else
            {
                if (i.IsTip())
                {
                    var printRecItem = new PrintRecItem
                    {
                        Description = i.Description,
                        Quantity = i.Quantity,
                        UnitPrice = i.Quantity == 0 || i.Amount == 0 ? 0 : i.Amount / i.Quantity,
                        Department = 11,
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
                else if (i.IsSingleUseVoucher() && i.Amount < 0)
                {  
                    var printRecItemAdjustment = new PrintRecItemAdjustment
                    {
                        Description = i.Description,
                        Amount = Math.Abs(i.Amount),
                        AdjustmentType = 12,
                        Department = i.GetVatGroup(),
                    };
                    itemAndMessages.Add(new() { PrintRecItemAdjustment = printRecItemAdjustment });
                }
                else if (i.IsMultiUseVoucher())
                {
                    var printRecItem = new PrintRecItem
                    {
                        Description = i.Description,
                        Quantity = i.Quantity,
                        UnitPrice = i.Quantity == 0 || i.Amount == 0 ? 0 : i.Amount / i.Quantity,
                        Department = 11,
                    };
                    itemAndMessages.Add(new() { PrintRecItem = printRecItem });
                }
                else if (i.Amount < 0)
                {
                    var printRecItemAdjustment = new PrintRecItemAdjustment
                    {
                        Description = i.Description,
                        Amount = Math.Abs(i.Amount),
                        AdjustmentType = 3,
                        Department = i.GetVatGroup(),
                    };
                    itemAndMessages.Add(new() { PrintRecItemAdjustment = printRecItemAdjustment });
                }
                else
                {
                    var printRecItem = new PrintRecItem
                    {
                        Description = i.Description,
                        Quantity = i.Quantity,
                        UnitPrice = i.Quantity == 0 || i.Amount == 0 ? 0 : i.Amount / i.Quantity,
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
            }
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
                totalAndMessages.Add(new()
                {
                    PrintRecTotal = printRecTotal,
                    PrintRecMessage = printRecMessage
                });
            }

            if (totalAndMessages.Count == 0)
            {
                totalAndMessages.Add(new()
                {
                    PrintRecTotal = new PrintRecTotal
                    {
                        PaymentType = 0,
                        Index = 0,
                        Payment = 0m
                    }
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
                0x06 => new EpsonPaymentType() { PaymentType = 6, Index = 1 },
                0x07 => new EpsonPaymentType() { PaymentType = 5, Index = 0 },
                0x08 => new EpsonPaymentType() { PaymentType = 5, Index = 0 },
                0x09 => new EpsonPaymentType() { PaymentType = 5, Index = 0 },
                0x0A => new EpsonPaymentType() { PaymentType = 2, Index = 1 },
                0x0B => new EpsonPaymentType() { PaymentType = 2, Index = 1 },
                0x0C => new EpsonPaymentType() { PaymentType = 0, Index = 0 },
                0x0D => new EpsonPaymentType() { PaymentType = 5, Index = 0 },
                0x0E => new EpsonPaymentType() { PaymentType = 5, Index = 0 },
                0x0F => new EpsonPaymentType() { PaymentType = 3, Index = 1 },
                _ => throw new NotSupportedException($"The payitemcase {payItem.ftPayItemCase} is not supported")
            };
        }

        // TODO: check VAT rate table on printer at the moment according to xml example
        private static readonly int _vatRateBasic = 1;
        private static readonly int _vatRateDeduction1 = 2;
        private static readonly int _vatRateDeduction2 = 3;
        private static readonly int _vatRateSuperReduced1 = 4;
        private static readonly int _vatRateZero = 13;
        private static readonly int _vatRateUnknown = -1;
        private static readonly int _notTaxable = 0;
        private static int _vatRateSuperReduced2;
        private static int _vatRateParking;

        public static int GetVatGroup(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0xF) switch
            {
                0x0 => _vatRateUnknown, // 0 ???
                0x1 => _vatRateDeduction1, // 10%
                0x2 => _vatRateDeduction2, // 4%
                0x3 => _vatRateBasic, // 22%
                0x4 => _vatRateSuperReduced1, // ?
                0x5 => _vatRateSuperReduced2, // ?
                0x6 => _vatRateParking, // ?
                0x7 => _vatRateZero, // ?
                0x8 => _notTaxable, // ? 
                _ => _vatRateUnknown // ?
            };
        }
    }
}

public class EpsonPrinterDepartmentConfiguration
{
    public Dictionary<string, long> DepartmentMapping { get; set; } = new Dictionary<string, long>();


    public static EpsonPrinterDepartmentConfiguration Default => new EpsonPrinterDepartmentConfiguration
    {
        DepartmentMapping = new Dictionary<string, long>
        {
            { "0", 8 }, // unknown
            { "1", 2 }, // reduced1 => 10%
            { "2", 3 }, // reduced 2 => 5%
            { "3", 1 }, // basic => 22%
            { "4", -1 }, // superreduced 1
            { "5", -1 }, // superreduced 2
            { "6", -1 }, // parking rate
            { "7", 7 }, // zero rate => 0%
            { "8", 8 }, // not taxable => 0%
        }
    };
}