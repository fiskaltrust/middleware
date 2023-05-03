using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Epson.Models;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Utilities
{
    public class EpsonCommandFactory
    {
        private readonly EpsonScuConfiguration _epsonScuConfiguration;

        public EpsonCommandFactory(EpsonScuConfiguration epsonScuConfiguration)
        {

            _epsonScuConfiguration = epsonScuConfiguration;
        }

        public string CreateInvoiceRequestContent(FiscalReceiptInvoice request)
        {
            var fiscalReceipt = CreateFiscalReceipt(request);
            fiscalReceipt.DisplayText = string.IsNullOrEmpty(request.DisplayText) ? null : new DisplayText() { Data = request.DisplayText };
            fiscalReceipt.PrintRecItem = request.Items?.Select(p => new PrintRecItem
            {
                Description = p.Description,
                Quantity = p.Quantity,
                UnitPrice = p.UnitPrice,
                Department = p.VatGroup
            }).ToList();
            fiscalReceipt.PrintRecItemAdjustment = request.PaymentAdjustments?.Select(p => new PrintRecItemAdjustment
            {
                Description = p.Description,
                AdjustmentType = p.Amount < 0 ? 3 : 8,
                Amount = Math.Abs(p.Amount),
                Department = p.VatGroup ?? 0
            }).ToList();
            fiscalReceipt.PrintRecTotal = request.Payments?.Select(p => new PrintRecTotal
            {
                Description = p.Description,
                PaymentType = (int) p.PaymentType,
                Payment = p.Amount
            }).ToList();
            return SoapSerializer.Serialize(fiscalReceipt);
        }

        public string CreateRefundRequestContent(FiscalReceiptRefund request)
        {
            var fiscalReceipt = CreateFiscalReceipt(request);
            fiscalReceipt.BeginFiscalReceipt.Operator = "1";
            fiscalReceipt.EndFiscalReceipt.Operator = "1";
            fiscalReceipt.PrintRecTotal?.ForEach(x => x.Operator = "1");
            fiscalReceipt.PrintRecMessage = new PrintRecMessage()
            {
                Operator = "1",
                Message = request.DisplayText,
                MessageType = "4"
            };
            fiscalReceipt.PrintRecRefund = request.Refunds?.Select(GetPrintRecRefund).ToList();
            return SoapSerializer.Serialize(fiscalReceipt);
        }

        public string CreateQueryPrinterStatusRequestContent()
        {
            var queryPrinterStatus = new QueryPrinterStatusCommand { QueryPrinterStatus = new QueryPrinterStatus { StatusType = 1 } };
            return SoapSerializer.Serialize(queryPrinterStatus);
        }

        public string CreatePrintZReportRequestContent(DailyClosingRequest request)
        {
            var fiscalReport = new FiscalReport
            {
                ZReport = new ZReport { Operator = request.Operator },
                DisplayText = string.IsNullOrEmpty(request.DisplayText) ? null : new DisplayText() { Data = request.DisplayText }
            };
            return SoapSerializer.Serialize(fiscalReport);
        }

        public static T? Deserialize<T>(Stream stream) where T : class
        {
            var reader = new XmlSerializer(typeof(T));
            return reader.Deserialize(stream) as T;
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
                    UnitPrice = recRefund.UnitPrice,
                    Department = recRefund.VatGroup,
                    Operator = "1"
                };
            }
            else
            {
                throw new Exception("Refund properties not set properly!");
            }
        }


        private FiscalReceipt CreateFiscalReceipt(FiscalReceiptInvoice request)
        {
            var fiscalReceipt = new FiscalReceipt
            {
                LotteryID = !string.IsNullOrEmpty(request.LotteryID) ? new LotteryID() { Code = request.LotteryID} : null,
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


        private FiscalReceipt CreateFiscalReceipt(FiscalReceiptRefund request)
        {
            var fiscalReceipt = new FiscalReceipt
            {
                LotteryID = !string.IsNullOrEmpty(request.LotteryID) ? new LotteryID() { Code = request.LotteryID } : null,
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
                    Index = p.Index
                }).ToList(),
            };
            return fiscalReceipt;
        }
    }
}
