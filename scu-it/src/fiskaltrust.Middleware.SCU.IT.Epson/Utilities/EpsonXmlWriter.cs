using System;
using System.Collections.Generic;
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

        public Stream GetFiscalReceiptfromRequestXml(FiscalReceiptInvoice request)
        {
            var fiscalReceipt = CreateFiscalReceipt(request);
            fiscalReceipt.PrintRecItem = request.RecItems?.Select(p => new PrintRecItem
            {
                Description = p.Description,
                Quantity = p.Quantity,
                UnitPrice = p.UnitPrice,
                //Department = 
                //Operator = request.
            }).ToList();
            return GetFiscalReceiptXml(fiscalReceipt);
        }
        public Stream GetFiscalReceiptfromRequestXml(FiscalReceiptRefund request)
        {
            var fiscalReceipt = CreateFiscalReceipt(request);
            return GetFiscalReceiptXml(fiscalReceipt);
        }

        private FiscalReceipt CreateFiscalReceipt(FiscalReceiptRequest request)
        {
            var fiscalReceipt = new FiscalReceipt
            {
                PrintBarCode = !string.IsNullOrEmpty(request.Barcode) ? new PrintBarCode()
                {
                    Code = request.Barcode,
                    CodeType = _epsonScuConfiguration.CodeType,
                    Height = _epsonScuConfiguration.BarCodeHeight,
                    HRIFont = _epsonScuConfiguration.BarCodeHRIFont,
                    HRIPosition = _epsonScuConfiguration.BarCodeHRIPosition,
                    //Operator = request.
                    Position = _epsonScuConfiguration.BarCodePosition,
                    Width = _epsonScuConfiguration.BarCodeWidth
                } : null,
                PrintRecTotal = request.RecTotals?.Select(p => new PrintRecTotal
                {
                    Description= p.Description,
                    Payment = p.Payment,
                    PaymentType = (int) p.PaymentType,
                    Index= p.Index,
                    //Operator = request.
                }).ToList(),
            };
            return fiscalReceipt;
        }

        private Stream GetFiscalReceiptXml(FiscalReceipt fiscalReceipt)
        {
            var writer = new XmlSerializer(typeof(FiscalReceipt));
            var stream = new MemoryStream();
            using (var file = new StreamWriter(stream))
            {
                writer.Serialize(file, fiscalReceipt);
            }
            stream.Position= 0;
            return stream;
        }
    }
}
