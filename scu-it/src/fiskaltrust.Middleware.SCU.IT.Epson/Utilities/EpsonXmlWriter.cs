using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Configuration;
using fiskaltrust.Middleware.SCU.IT.Epson.Models;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Utilities
{
    public class EpsonXmlWriter
    {
        private readonly EpsonScuConfiguration _epsonScuConfiguration;

        public EpsonXmlWriter(EpsonScuConfiguration epsonScuConfiguration) { 
        
            _epsonScuConfiguration = epsonScuConfiguration;
        }

        public Stream FiscalReceiptToXml(FiscalReceiptInvoice request)
        {
            var fiscalReceipt = CreateFiscalReceipt(request);
            fiscalReceipt.DisplayText = string.IsNullOrEmpty(request.DisplayText) ? null : (DisplayText) request.DisplayText;
            fiscalReceipt.PrintRecItem = request.Items?.Select(p => new PrintRecItem
            {
                Description = p.Description,
                Quantity = p.Quantity,
                UnitPrice = p.UnitPrice,
                Department = p.VatGroup
            }).ToList();
            fiscalReceipt.PrintRecItemAdjustment = request.PaymentAdjustments?.Where(x => x.VatGroup.HasValue).Select(p => new PrintRecItemAdjustment
            {
                Description = p.Description,
                AdjustmentType = p.Amount < 0 ? 3 : 8,
                Amount = Math.Abs(p.Amount),
                Department = p.VatGroup ?? 0
            }).ToList();
            fiscalReceipt.PrintRecSubtotalAdjustment = request.PaymentAdjustments?.Where(x => !x.VatGroup.HasValue).Select(p => new PrintRecSubtotalAdjustment
            {
                Description = p.Description,
                AdjustmentType = p.Amount < 0 ? 1 : 6,
                Amount = Math.Abs(p.Amount)
            }).ToList();
            fiscalReceipt.PrintRecTotal = request.Payments?.Select(p => new PrintRecTotal
            {
                Description = p.Description,
                PaymentType = (int) p.PaymentType,
                Payment = p.Amount
            }).ToList();
            return GetFiscalReceiptXml(fiscalReceipt);
        }

        public Stream FiscalReceiptToXml(FiscalReceiptRefund request)
        {
            var fiscalReceipt = CreateFiscalReceipt(request);
            fiscalReceipt.PrintRecRefund = request.Refunds?.Select(GetPrintRecRefund).ToList();
            return GetFiscalReceiptXml(fiscalReceipt);
        }

        public static PrinterResponse? GetPrinterResponse(Stream stream)
        {
            var reader = new XmlSerializer(typeof(PrinterResponse));
            return reader.Deserialize(stream) as PrinterResponse;
        }

        private PrintRecRefund GetPrintRecRefund(Refund recRefund)
        {
            if (recRefund.OperationType.HasValue)
            {
                return new PrintRecRefund
                {
                    Amount = recRefund.Amount,
                    OperationType = (int) recRefund.OperationType
                };
            }
            else if (recRefund.UnitPrice != 0 && recRefund.Quantity != 0)
            {
                return new PrintRecRefund
                {
                    Description = recRefund.Description,
                    Quantity = recRefund.Quantity,
                    UnitPrice = recRefund.UnitPrice
                };
            }else
            {
                throw new Exception("Refund properties not set properly!");
            }
        }

      
        private FiscalReceipt CreateFiscalReceipt(FiscalReceiptInvoice request)
        {
            var fiscalReceipt = new FiscalReceipt
            {
                LotteryID = !string.IsNullOrEmpty(request.LotteryID) ? (LotteryID)request.LotteryID : null,
                PrintBarCode = !string.IsNullOrEmpty(request.Barcode) ? new PrintBarCode()
                {
                    Code = request.Barcode,
                    CodeType = _epsonScuConfiguration.CodeType,
                    Height = _epsonScuConfiguration.BarCodeHeight,
                    HRIFont = _epsonScuConfiguration.BarCodeHRIFont,
                    HRIPosition = _epsonScuConfiguration.BarCodeHRIPosition,
                    Position = _epsonScuConfiguration.BarCodePosition,
                    Width = _epsonScuConfiguration.BarCodeWidth
                } : null,
                PrintRecTotal = request.Payments?.Select(p => new PrintRecTotal
                {
                    Description= p.Description,
                    Payment = p.Amount,
                    PaymentType = (int) p.PaymentType,
                    Index= p.Index,
                    Operator = request.Operator
                }).ToList(),
            };
            return fiscalReceipt;
        }


        private FiscalReceipt CreateFiscalReceipt(FiscalReceiptRefund request)
        {
            var fiscalReceipt = new FiscalReceipt
            {
                LotteryID = !string.IsNullOrEmpty(request.LotteryID) ? (LotteryID) request.LotteryID : null,
                PrintBarCode = !string.IsNullOrEmpty(request.Barcode) ? new PrintBarCode()
                {
                    Code = request.Barcode,
                    CodeType = _epsonScuConfiguration.CodeType,
                    Height = _epsonScuConfiguration.BarCodeHeight,
                    HRIFont = _epsonScuConfiguration.BarCodeHRIFont,
                    HRIPosition = _epsonScuConfiguration.BarCodeHRIPosition,
                    Position = _epsonScuConfiguration.BarCodePosition,
                    Width = _epsonScuConfiguration.BarCodeWidth
                } : null,
                PrintRecTotal = request.Payments?.Select(p => new PrintRecTotal
                {
                    Description = p.Description,
                    Payment = p.Amount,
                    PaymentType = (int) p.PaymentType,
                    Index = p.Index,
                    Operator = request.Operator
                }).ToList(),
            };
            return fiscalReceipt;
        }

        private static Stream GetFiscalReceiptXml(FiscalReceipt fiscalReceipt)
        {
            var serializer = new XmlSerializer(typeof(FiscalReceipt));
            var stream = new MemoryStream();
            serializer.Serialize(stream, fiscalReceipt);
            stream.Position= 0;
            return stream;
        }
    }
}
