using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Xml.Linq;
using fiskaltrust.Middleware.SCU.IT.Configuration;
using fiskaltrust.Middleware.SCU.IT.Epson.Utilities;
using fiskaltrust.Middleware.SCU.IT.FiscalizationService;
using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Xunit;
using fiskaltrust.ifPOS.v1.it;
using System.Collections.Generic;
using System.IO;

namespace fiskaltrust.Middleware.SCU.IT.UnitTest
{
    public class FiscalReceiptTests
    {
        [Fact]
        public void CommercailDocument_SendInvoice_CreateValidXml()
        {
            var epsonScuConfiguration = new EpsonScuConfiguration ();
            var epsonXmlWriter = new EpsonXmlWriter(epsonScuConfiguration);

            var fiscalReceiptRequest = new FiscalReceiptInvoice()
            {
                Barcode = "0123456789",
                DisplayText = "Message on customer display",
                RecItems = new List<Item>()
                {
                    new Item() { Description = "PANINO", Quantity = 1, UnitPrice = 6.00m, VatGroup = 2 },
                    new Item() { Description = "Selling Item 2 VAT 22%", Quantity = 1.234m, UnitPrice = 10.00m, VatGroup = 1 },
                    new Item() { Description = "Selling Item 3 VAT 22%", Quantity = 2.5m, UnitPrice = 100.17m, VatGroup = 1  },
                    new Item() { Description = "Selling Item 4 VAT 10%", Quantity = 12.13m, UnitPrice = 216.17m, VatGroup = 2  },
                    new Item() { Description = "Selling Item 5 4%", Quantity = 12.13m, UnitPrice = 216.17m, VatGroup = 3 },
                },
                PaymentAdjustments = new List<PaymentAdjustment>()
                {
                    new PaymentAdjustment()
                    {
                        Description = "Discount applied to the subtotal",
                        Amount = -300.12m
                    }
                },
                Payments = new List<Payment>()
                {
                    new Payment(){ Description = "Payment in cash", Amount = 0, PaymentType = PaymentType.Cash, Index = 1}
                }

            };

            var xml = epsonXmlWriter.GetFiscalReceiptfromRequestXml(fiscalReceiptRequest);
            if (File.Exists("FiscalReceiptInvoice"))
            {
                File.Delete("FiscalReceiptInvoice");
            }

            using (var outputFileStream = new FileStream("FiscalReceiptInvoice", FileMode.Create))
            {
                xml.CopyTo(outputFileStream);
            }
        }
        [Fact]
        public void CommercailDocument_SendRefund_CreateValidXml()
        {
            var epsonScuConfiguration = new EpsonScuConfiguration();
            var epsonXmlWriter = new EpsonXmlWriter(epsonScuConfiguration);
            var fiscalReceiptRequest = new FiscalReceiptRefund()
            {
                Barcode = "0123456789",
                DisplayText = "REFUND 0279 0010 08012021 99MEY123456",
                RecRefunds= new List<Refund>()
                {
                    new Refund(){ UnitPrice = 600, Quantity = 1, VatGroup = 1, Description = "TV" }
                },
                Payments = new List<Payment>()
                {
                    new Payment(){ Description = "Payment in cash", Amount= 600, PaymentType = PaymentType.Cash, Index = 1}
                }

            };
            var xml = epsonXmlWriter.GetFiscalReceiptfromRequestXml(fiscalReceiptRequest);
            if (File.Exists("FiscalReceiptRefund"))
            {
                File.Delete("FiscalReceiptRefund");
            }
            using (var outputFileStream = new FileStream("FiscalReceiptRefund", FileMode.Create))
            {
                xml.CopyTo(outputFileStream);
            }
        }
    }
}
