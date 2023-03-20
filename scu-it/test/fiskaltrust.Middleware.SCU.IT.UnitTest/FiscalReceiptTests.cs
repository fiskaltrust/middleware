using fiskaltrust.Middleware.SCU.IT.Epson.Utilities;
using Xunit;
using fiskaltrust.ifPOS.v1.it;
using System.Collections.Generic;
using System.IO;
using fiskaltrust.Middleware.SCU.IT.Epson;

namespace fiskaltrust.Middleware.SCU.IT.UnitTest
{
    public class FiscalReceiptTests
    {
        [Fact]
        public void CommercailDocument_SendInvoice_CreateValidXml()
        {
            var epsonScuConfiguration = new EpsonScuConfiguration ();
            var epsonXmlWriter = new EpsonCommandFactory(epsonScuConfiguration);

            var fiscalReceiptRequest = new FiscalReceiptInvoice()
            {
                Barcode = "0123456789",
                DisplayText = "Message on customer display",
                Items = new List<Item>()
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
                        Description = "Discount",
                        Amount = -5.12m,
                        VatGroup = 1,
                    },
                    new PaymentAdjustment()
                    {
                        Description = "Surcharge",
                        Amount = 3.12m,
                        VatGroup = 2,
                    },
                    new PaymentAdjustment()
                    {
                        Description = "Discount applied to the subtotal",
                        Amount = -100.12m
                    }
                },
                Payments = new List<Payment>()
                {
                    new Payment(){ Description = "Payment in cash", Amount = 0, PaymentType = PaymentType.Cash, Index = 1}
                }

            };

            var xml = epsonXmlWriter.CreateInvoiceRequestContent(fiscalReceiptRequest);
            WriteFile(xml, "FiscalReceiptInvoice");
        }
        [Fact]
        public void CommercailDocument_SendRefundItem_CreateValidXml()
        {
            var epsonScuConfiguration = new EpsonScuConfiguration();
            var epsonXmlWriter = new EpsonCommandFactory(epsonScuConfiguration);
            var fiscalReceiptRequest = new FiscalReceiptRefund()
            {
                Operator = "1",
                DisplayText = "REFUND 0279 0010 08012021 99MEY123456",
                Refunds= new List<Refund>()
                {
                    new Refund(){ UnitPrice = 600, Quantity = 1, VatGroup = 1, Description = "TV" }
                },
                Payments = new List<Payment>()
                {
                    new Payment(){ Description = "Payment in cash", Amount= 600, PaymentType = PaymentType.Cash, Index = 1}
                }

            };
            var xml = epsonXmlWriter.CreateRefundRequestContent(fiscalReceiptRequest);
            WriteFile(xml, "FiscalReceiptRefund");
        }

        [Fact]
        public void CommercailDocument_SendRefundAcconto_CreateValidXml()
        {
            var epsonScuConfiguration = new EpsonScuConfiguration();
            var epsonXmlWriter = new EpsonCommandFactory(epsonScuConfiguration);
            var fiscalReceiptRequest = new FiscalReceiptRefund()
            {
                Operator = "1",
                DisplayText = "REFUND 0279 0010 08012021 99MEY123456",
                Refunds = new List<Refund>()
                {
                    new Refund(){ Amount = 600, OperationType = OperationType.Acconto, VatGroup = 1, Description = "Acconto" }
                },
                Payments = new List<Payment>()
                {
                    new Payment(){ Description = "Payment in cash", Amount= 600, PaymentType = PaymentType.Cash, Index = 1}
                }

            };
            var xml = epsonXmlWriter.CreateRefundRequestContent(fiscalReceiptRequest);
            WriteFile(xml, "FiscalReceiptRefundAcconto");
        }
        [Fact]
        public void CommercailDocument_SendInvoiceWithLottery_CreateValidXml()
        {
            var epsonScuConfiguration = new EpsonScuConfiguration();
            var epsonXmlWriter = new EpsonCommandFactory(epsonScuConfiguration);
            var fiscalReceiptRequest = new FiscalReceiptInvoice()
            {
                Operator = "1",
                DisplayText = "Message on customer display",
                LotteryID= "ABCDEFGN",
                Items = new List<Item>()
                {
                    new Item(){ Quantity = 1, UnitPrice = 6, VatGroup = 1, Description = "PANINO" }
                },
                Payments = new List<Payment>()
                {
                    new Payment(){ Description = "Payment in cash", Amount= 0, PaymentType = PaymentType.Cash, Index = 1}
                }
            };
            var xml = epsonXmlWriter.CreateInvoiceRequestContent(fiscalReceiptRequest);
            WriteFile(xml, "FiscalReceiptLottery");
        }
        [Fact]
        public void CommercailDocument_SendInvoiceWithDepositAdjustment_CreateValidXml()
        {
            var epsonScuConfiguration = new EpsonScuConfiguration();
            var epsonXmlWriter = new EpsonCommandFactory(epsonScuConfiguration);
            var fiscalReceiptRequest = new FiscalReceiptInvoice()
            {
                Operator = "1",
                DisplayText = "Message on customer display",
                Items = new List<Item>()
                {
                    new Item(){ Quantity = 1, UnitPrice = 650, VatGroup = 1, Description = "TELEVISION" }
                },
                PaymentAdjustments = new List<PaymentAdjustment>()
                {
                    new PaymentAdjustment(){ Description = "DEPOSIT ADJUSTMENT", Amount = 100}
                },
                Payments = new List<Payment>()
                {
                    new Payment(){ Description = "Payment in cash", Amount= 550, PaymentType = PaymentType.Cash, Index = 1}
                }
            };
            var xml = epsonXmlWriter.CreateInvoiceRequestContent(fiscalReceiptRequest);
            WriteFile(xml, "FiscalReceiptInvoiceDepositAdjustment");
        }


        private static void WriteFile(Stream xml, string filename)
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            using (var outputFileStream = new FileStream(filename, FileMode.Create))
            {
                xml.CopyTo(outputFileStream);
            }
        }
    }
}
