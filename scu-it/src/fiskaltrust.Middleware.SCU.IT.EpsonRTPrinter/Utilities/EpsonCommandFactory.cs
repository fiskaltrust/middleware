using System;
using System.Linq;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Security.Cryptography;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

#pragma warning disable

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities
{
    public static class EpsonCommandFactory
    {
        private static List<PrintItem> GetChargeItemLines(ChargeItem chargeItem, string vatText, string vatLegendText)
        {
            var resultItems = new List<PrintItem>();
            var isRefundOrVoid = ReceiptCaseHelper.IsVoid(chargeItem) || ReceiptCaseHelper.IsRefund(chargeItem);
            var quantity = isRefundOrVoid ? -chargeItem.Quantity : chargeItem.Quantity;
            var amount = isRefundOrVoid ? -chargeItem.Amount : chargeItem.Amount;
            var description = chargeItem.Description;

            if (quantity == 0)
            {
                while (description.Length > 0)
                {
                    var desc = description.Length <= 46 ? description : description.Substring(0, 46);
                    resultItems.Add(new PrintNormal() { Operator = "1", Data = desc });
                    description = description.Substring(desc.Length);
                }
                if (!string.IsNullOrWhiteSpace(chargeItem.ftChargeItemCaseData))
                {
                    switch (chargeItem.ftChargeItemCase & 0x0000_00F0_0000_0000)
                    {
                        case 0x0000_0010_0000_0000: //BMP
                        {
                            resultItems.Add(new PrintGraphicCoupon() { Operator = "1", GraphicFormat = PrintGraphicCouponGraphicFormat.BMP, Base64Data = chargeItem.ftChargeItemCaseData });
                            break;
                        }
                        case 0x0000_0020_0000_0000: //Raster
                        {
                            resultItems.Add(new PrintGraphicCoupon() { Operator = "1", GraphicFormat = PrintGraphicCouponGraphicFormat.Raster, Base64Data = chargeItem.ftChargeItemCaseData });
                            break;
                        }
                    }
                }
                if (!string.IsNullOrWhiteSpace(chargeItem.ProductBarcode))
                {
                    switch (chargeItem.ftChargeItemCase & 0x0000_000F_0000_0000)
                    {
                        case 0x0000_0000_0000_0000: //EAN13
                        {
                            if ((chargeItem.ProductBarcode.Length != 13) ||
                                !chargeItem.ProductBarcode.All(char.IsDigit))
                            {
                                throw new Exception("EAN 13 code must be 13 numeric chars length!");
                            }
                            resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.EAN13, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                            break;
                        }
                        case 0x0000_0001_0000_0000: //EAN8
                        {
                            if ((chargeItem.ProductBarcode.Length != 8) ||
                                !chargeItem.ProductBarcode.All(char.IsDigit))
                            {
                                throw new Exception("EAN 8 code must be 8 numeric chars length!");
                            }
                            resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.EAN8, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                            break;
                        }
                        case 0x0000_0002_0000_0000: //UPCA
                        {
                            if ((chargeItem.ProductBarcode.Length != 12) ||
                                !chargeItem.ProductBarcode.All(char.IsDigit))
                            {
                                throw new Exception("UPC-A code must be 12 numeric chars length!");
                            }
                            resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.UPCA, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                            break;
                        }
                        case 0x0000_0003_0000_0000: //UPCE
                        {
                            if ((chargeItem.ProductBarcode.Length != 12) ||
                                !chargeItem.ProductBarcode.All(char.IsDigit) ||
                                !chargeItem.ProductBarcode.StartsWith("0"))
                            {
                                throw new Exception("UPC-E code must be 12 numeric chars length and start with 0!");
                            }
                            resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.UPCE, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                            break;
                        }
                        case 0x0000_0004_0000_0000: //CODE39
                        {
                            if ((chargeItem.ProductBarcode.Length < 1) ||
                                (chargeItem.ProductBarcode.Length > 34))
                            {
                                throw new Exception("CODE39 code must be 1 to 34 chars length!");
                            }
                            resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CODE39, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width2 });
                            break;
                        }
                        case 0x0000_0005_0000_0000: //CODE93
                        {
                            if ((chargeItem.ProductBarcode.Length < 1) ||
                                (chargeItem.ProductBarcode.Length > 59))
                            {
                                throw new Exception("CODE93 code must be 1 to 59 chars length!");
                            }
                            resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CODE93, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width2 });
                            break;
                        }
                        case 0x0000_0006_0000_0000: //CODE128
                        {
                            if ((chargeItem.ProductBarcode.Length < 3) ||
                                (chargeItem.ProductBarcode.Length > 100) ||
                                (!chargeItem.ProductBarcode.StartsWith("{A") &&
                                !chargeItem.ProductBarcode.StartsWith("{B") &&
                                !chargeItem.ProductBarcode.StartsWith("{C")))
                            {
                                throw new Exception("CODE128 code must be 3 to 100 chars length and must start with either {A or {B or {C!");
                            }
                            resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CODE128, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width2 });
                            break;
                        }
                        case 0x0000_0007_0000_0000: //CODABAR
                        {
                            if ((chargeItem.ProductBarcode.Length < 1) ||
                                (chargeItem.ProductBarcode.Length > 47))
                            {
                                throw new Exception("CODABAR code must be 1 to 47 chars length!");
                            }
                            resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CODABAR, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                            break;
                        }
                        case 0x0000_0008_0000_0000: //ITF
                        {
                            if ((chargeItem.ProductBarcode.Length < 2) ||
                                (chargeItem.ProductBarcode.Length > 62) ||
                                (chargeItem.ProductBarcode.Length % 2 == 1) ||
                                !chargeItem.ProductBarcode.All(char.IsDigit))
                            {
                                throw new Exception("ITF code must be 2 to 62 numeric chars length!");
                            }
                            resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.ITF, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                            break;
                        }
                        case 0x0000_0009_0000_0000: //QRCODE1
                        {
                            resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.QRCODE1, QRCodeAlignment = PrintBarCodeQRCodeAlignment.Centred, QRCodeDataType = PrintBarCodeQRCodeDataType.AlphaNumeric, QRCodeErrorCorrection = 0, QRCodeSize = 4 });
                            break;
                        }
                        case 0x0000_000A_0000_0000: //QRCODE2
                        {
                            resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.QRCODE2, QRCodeAlignment = PrintBarCodeQRCodeAlignment.Centred, QRCodeDataType = PrintBarCodeQRCodeDataType.AlphaNumeric, QRCodeErrorCorrection = 2, QRCodeSize = 4 });
                            break;
                        }
                        case 0x0000_000B_0000_0000: //CodeType74
                        {
                            if ((chargeItem.ProductBarcode.Length < 2) ||
                                (chargeItem.ProductBarcode.Length > 96))
                            {
                                throw new Exception("74 code must be 2 to 96 chars length!");
                            }
                            resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CodeType74, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                            break;
                        }
                        case 0x0000_000C_0000_0000: //CodeType75
                        {
                            if ((chargeItem.ProductBarcode.Length != 13) ||
                                !chargeItem.ProductBarcode.All(char.IsDigit))
                            {
                                throw new Exception("75 code must be 13 numeric chars length!");
                            }
                            resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CodeType75, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                            break;
                        }
                        case 0x0000_000D_0000_0000: //CodeType76
                        {
                            if ((chargeItem.ProductBarcode.Length != 13) ||
                                !chargeItem.ProductBarcode.All(char.IsDigit))
                            {
                                throw new Exception("76 code must be 13 numeric chars length!");
                            }
                            resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CodeType76, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                            break;
                        }
                        case 0x0000_000E_0000_0000: //CodeType77
                        {
                            if ((chargeItem.ProductBarcode.Length != 13) ||
                                !chargeItem.ProductBarcode.All(char.IsDigit) ||
                                (!chargeItem.ProductBarcode.StartsWith("0") &&
                                !chargeItem.ProductBarcode.StartsWith("1")))
                            {
                                throw new Exception("77 code must be 13 numeric chars length and start with 0 or 1!");
                            }
                            resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CodeType77, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                            break;
                        }
                        case 0x0000_000F_0000_0000: //CodeType78
                        {
                            if ((chargeItem.ProductBarcode.Length < 2) ||
                                (chargeItem.ProductBarcode.Length > 70))
                            {
                                throw new Exception("78 code must be 2 to 70 chars length!");
                            }
                            resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CodeType78, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width2 });
                            break;
                        }
                    }
                }
            }
            else if (quantity > 0)
            {
                var amountText = GetAmountString(amount, 13);

                description = description.Length <= 38 ? description : description.Substring(0, 38);
                if (description.Length <= 25)
                {
                    var desc = description.Length <= 25 ? description + new string(' ', 25 - description.Length) : description.Substring(0, 25);
                    resultItems.Add(new PrintNormal() { Operator = "1", Data = $"{desc} {vatText} {amountText}" });
                }
                else
                {
                    var desc = description.Length <= 25 ? description + new string(' ', 25 - description.Length) : description.Substring(0, 25);
                    resultItems.Add(new PrintNormal() { Operator = "1", Data = $"{desc}" });
                    desc = description.Substring(25);
                    desc += new string(' ', 25 - desc.Length);
                    resultItems.Add(new PrintNormal() { Operator = "1", Data = $"{desc} {vatText} {amountText}" });
                }
                if (quantity > 1)
                {
                    resultItems.Add(new PrintNormal() { Operator = "1", Data = $"  n.{quantity} * {amount / quantity:0.00}" });
                }
            }
            if (!string.IsNullOrWhiteSpace(chargeItem.ProductBarcode))
            {
                //TODO establish the string content
            }

            return resultItems;
        }

        public static PrinterNonFiscal PerformUnspecifiedProtocolReceipt(ReceiptRequest request)
        {
            var content = new PrinterNonFiscal();

            content.BeginNonFiscal = new BeginNonFiscal() { Operator = "1" };
            content.EndNonFiscal = new EndNonFiscal() { Operator = "1" };
            content.PrintItems = new List<PrintItem>();
            foreach (var ci in request.cbChargeItems)
            {
                var vatValues = EpsonCommandFactory.GetVatInfo(ci);
                var cil = GetChargeItemLines(ci, vatValues.Item1, vatValues.Item2);
                content.PrintItems.AddRange(cil);
            }
            return content;
        }

        private static string GetAmountString(decimal amount, int length)
        {
            var amountText = string.Format("{0:0.00}", amount).Replace(".", ",");

            if (amountText.Length < length)
            {
                amountText = new string(' ', length - amountText.Length) + amountText;
            }

            return amountText;
        }

        public static string ReprintCommand(string day, string month, string year, long receiptNumber)
        {
            return $"""
<?xml version="1.0" encoding="utf-8"?>
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
    <s:Body>
        <printerCommand>
            <directIO command="3098" data="01{day}{month}{year}{receiptNumber.ToString().PadLeft(4, '0')}{receiptNumber.ToString().PadLeft(4, '0')}" />
        </printerCommand>
    </s:Body>
</s:Envelope>
""";
        }

        public static string LoginCommand(string configuredPassword)
        {
            var password = (configuredPassword ?? "").PadRight(100, ' ').PadRight(32, ' ');
            var data = $"""
<?xml version="1.0" encoding="utf-8"?>
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
    <s:Body>
        <printerCommand>
            <directIO command="4038" data="02{password}" />
        </printerCommand>
    </s:Body>
</s:Envelope>
""";
            return data;
        }

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

            var lotteryData = receiptRequest.GetLotteryData();
            if (!string.IsNullOrEmpty(lotteryData?.servizi_lotteriadegliscontrini_gov_it?.codicelotteria))
            {
                fiscalReceipt.LotteryID = new LotteryID
                {
                    Code = lotteryData.servizi_lotteriadegliscontrini_gov_it.codicelotteria
                };
            }
            AddTrailerLines(configuration, receiptRequest, fiscalReceipt);
            return fiscalReceipt;
        }

        private static void AddTrailerLines(EpsonRTPrinterSCUConfiguration configuration, ReceiptRequest receiptRequest, FiscalReceipt fiscalReceipt)
        {
            var index = 1;
            var lines = new List<string>();

            if (!string.IsNullOrWhiteSpace(configuration.AdditionalTrailerLines))
                lines.AddRange(JsonConvert.DeserializeObject<List<string>>(configuration.AdditionalTrailerLines));

            if (!string.IsNullOrEmpty(receiptRequest.ftReceiptCaseData))
            {
                try
                {
                    var doc = JsonConvert.DeserializeObject(receiptRequest.ftReceiptCaseData);
                    var children = ((Newtonsoft.Json.Linq.JObject) doc).Children().Where(x => (x as JProperty) != null && (x as JProperty).Name == "cbReceiptLines");
                    if (children.Count() > 0)
                    {
                        var receiptLines = children.Values().FirstOrDefault().ToArray().Select(x => x.ToString()).ToArray(); 
                        lines.AddRange(receiptLines.ToList().Where(x => x != null).Select(x => x!)?.ToList() ?? new List<string>());
                    }
                }
                catch { }
            }

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
                0x09 => new EpsonPaymentType() { PaymentType = 5, Index = 3 },
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
            if ((chargeItem.ftChargeItemCase & 0xF) == 0x8)
            {
                return (chargeItem.ftChargeItemCase & 0xF000) switch
                {
                    0x8000 => 10,
                    0x2000 => 11,
                    0x1000 => 12,
                    0x3000 => 13,
                    0x4000 => 14,
                    0x5000 => 15,
                    _ => _vatRateUnknown // ?
                };
            }

            return (chargeItem.ftChargeItemCase & 0xF) switch
            {
                0x0 => _vatRateUnknown, // 0 ???
                0x1 => _vatRateDeduction1, // 10%
                0x2 => _vatRateDeduction2, // 4%
                0x3 => _vatRateBasic, // 22%
                0x4 => _vatRateSuperReduced1, // 5%
                0x5 => _vatRateSuperReduced2, // ?
                0x6 => _vatRateParking, // ?
                0x7 => _vatRateZero, // ?
                0x8 => _notTaxable, // ? 
                _ => _vatRateUnknown // ?
            };
        }

        public static (string, string, decimal) GetVatInfo(this ChargeItem chargeItem)
        {
            if ((chargeItem.ftChargeItemCase & 0xF) == 0x8)
            {
                return (chargeItem.ftChargeItemCase & 0xF000) switch
                {
                    0x8000 => ("   EE*", "*EE = Esclusa", 0),
                    0x2000 => ("   NS*", "*NS = Non soggetta", 0),
                    0x1000 => ("   NI*", "*NI = Non imponibile", 0),
                    0x3000 => ("   ES*", "*ES = Esente", 0),
                    0x4000 => ("   RM*", "*RM = Regime del margine", 0),
                    0x5000 => ("   AL*", "*AL = Operazione non IVA", 0),
                    _ => ("      ", "", 0) // ?
                };
            }

            return (chargeItem.ftChargeItemCase & 0xF) switch
            {
                0x0 => ("      ", "", 0), // 0 ???
                0x1 => ("10,00%", "", 10), // 10%
                0x2 => (" 4,00%", "", 4), // 4%
                0x3 => ("22,00%", "", 22), // 22%
                0x4 => (" 5,00%", "", 5), // 5%
                0x5 => ("      ", "", 0), // ?
                0x6 => ("      ", "", 0), // ?
                0x7 => ("      ", "", 0), // ?
                0x8 => ("      ", "", 0), // ? 
                _ => ("      ", "", 0) // ?
            };
        }
    }
}
