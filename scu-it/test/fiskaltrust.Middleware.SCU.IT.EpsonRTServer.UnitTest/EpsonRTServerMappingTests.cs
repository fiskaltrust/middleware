using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.IT.EpsonRTServer;
using fiskaltrust.Middleware.SCU.IT.EpsonRTServer.Models;
using FluentAssertions;
using FluentAssertions.Execution;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer.UnitTest
{
    public class EpsonRTServerMappingTests
    {
        private static TillState NewTillState() => new()
        {
            TillId = "FISK0001",
            RTServerSerialNumber = "99SEA004010",
            SrtUtcOffset = 2,
            LastFingerPrint = "99SEA004010FISK0001181062026070207430002000000100",
            LastZNumber = 743,
            LastDocNumber = 1,
            CurrentDailyAmount = 100
        };

        private static ReceiptRequest SaleRequest(string? lotteryCode = null) => new()
        {
            ftReceiptCase = 0x0001,
            cbReceiptMoment = new DateTime(2026, 7, 2, 12, 0, 0),
            cbChargeItems = new[] { new ChargeItem { Amount = 1.00m, Quantity = 1, Description = "TEST", VATRate = 22m, ftChargeItemCase = 0x3 } },
            cbPayItems = new[] { new PayItem { Amount = 1.00m, Quantity = 1, Description = "CONTANTE", ftPayItemCase = 0x0 } },
            ftReceiptCaseData = lotteryCode == null ? null : $"{{\"servizi_lotteriadegliscontrini_gov_it\":{{\"codicelotteria\":\"{lotteryCode}\"}}}}"
        };

        private static string ExtractReceiptElement(string createReceiptXml)
        {
            var start = createReceiptXml.IndexOf("<receipt>", StringComparison.Ordinal);
            var end = createReceiptXml.IndexOf("</receipt>", StringComparison.Ordinal) + "</receipt>".Length;
            return createReceiptXml.Substring(start, end - start);
        }

        [Fact]
        public void BuildFiscalDocument_Ccdc_Should_Equal_Hash_Of_Transmitted_Receipt_Element()
        {
            // The critical invariant: what we hash must be byte-identical to what we send.
            var doc = EpsonRTServerMapping.BuildFiscalDocument(SaleRequest(), NewTillState(), 0);

            var receiptElement = ExtractReceiptElement(doc.CreateReceiptXml);
            doc.Ccdc.Should().Be(GlobalTools.ComputeCcdc(receiptElement));
            doc.CreateReceiptXml.Should().Contain($"<receiptSecurity><hash fingerPrint=\"{doc.Ccdc}\" /></receiptSecurity>");
        }

        [Fact]
        public void BuildFiscalDocument_Should_Use_Dot_Decimal_Separator_And_Structure()
        {
            var doc = EpsonRTServerMapping.BuildFiscalDocument(SaleRequest(), NewTillState(), 0);

            using (new AssertionScope())
            {
                doc.CreateReceiptXml.Should().StartWith("<createReceipt><receipt><hash fingerPrint=\"99SEA004010FISK0001181062026070207430002000000100\"/>");
                doc.CreateReceiptXml.Should().Contain("unitPrice=\"1.00\"");
                doc.CreateReceiptXml.Should().Contain("recAmount=\"1.00\"");
                doc.CreateReceiptXml.Should().Contain("changeAmount=\"0.00\"");
                doc.CreateReceiptXml.Should().NotContain(",00");
                doc.CreateReceiptXml.Should().Contain("recNumber=\"0002\"");
                doc.CreateReceiptXml.Should().Contain("vatID=\"1\"");
                doc.DocNumber.Should().Be(2);
            }
        }

        [Fact]
        public void BuildFiscalDocument_Should_Emit_LotteryId_When_Present()
        {
            var doc = EpsonRTServerMapping.BuildFiscalDocument(SaleRequest("ABCD1234"), NewTillState(), 0);

            doc.CreateReceiptXml.Should().Contain("<printRecLotteryID lotteryID=\"ABCD1234\" />");
            doc.LotteryCode.Should().Be("ABCD1234");
        }

        [Fact]
        public void BuildFiscalDocument_Refund_Should_Reference_Original_Document()
        {
            var refMoment = new DateTime(2026, 7, 1, 10, 30, 0);
            var doc = EpsonRTServerMapping.BuildFiscalDocument(SaleRequest(), NewTillState(), 1, 96, 12, refMoment, "FISK0001");

            using (new AssertionScope())
            {
                doc.CreateReceiptXml.Should().Contain("<beginFiscalReceipt docType=\"1\"");
                doc.CreateReceiptXml.Should().Contain("refZRepNum=\"0096\"");
                doc.CreateReceiptXml.Should().Contain("refRecNum=\"0012\"");
                doc.CreateReceiptXml.Should().Contain("refDateTime=\"20260701T103000\"");
                doc.CreateReceiptXml.Should().Contain("refTillID=\"FISK0001\"");
                doc.CreateReceiptXml.Should().Contain("docType=\"1\"");
            }
        }

        private static ReceiptRequest RequestWith(ChargeItem[] items, decimal cashPayment) => new()
        {
            ftReceiptCase = 0x0001,
            cbReceiptMoment = new DateTime(2026, 7, 2, 12, 0, 0),
            cbChargeItems = items,
            cbPayItems = new[] { new PayItem { Amount = cashPayment, Quantity = 1, Description = "CONTANTE", ftPayItemCase = 0x0 } }
        };

        [Fact]
        public void BuildFiscalDocument_ItemDiscount_Should_Emit_Adjustment_And_Reduce_Total()
        {
            var items = new[]
            {
                new ChargeItem { Amount = 10.00m, Quantity = 1, Description = "Prodotto", VATRate = 22m, ftChargeItemCase = 0x3 },
                new ChargeItem { Amount = -2.00m, Quantity = 1, Description = "Sconto", VATRate = 22m, ftChargeItemCase = 0x3 }
            };
            var doc = EpsonRTServerMapping.BuildFiscalDocument(RequestWith(items, 8.00m), NewTillState(), 0);

            using (new AssertionScope())
            {
                doc.CreateReceiptXml.Should().Contain("<printRecItem description=\"Prodotto\"");
                doc.CreateReceiptXml.Should().Contain("<printRecItemAdjustment adjustmentType=\"3\" description=\"Sconto\" amount=\"2.00\"");
                doc.CreateReceiptXml.Should().Contain("recAmount=\"8.00\"");
                doc.AmountCents.Should().Be(800);
            }
        }

        [Fact]
        public void BuildFiscalDocument_SubtotalDiscount_Should_Emit_SubtotalAdjustment()
        {
            var items = new[]
            {
                new ChargeItem { Amount = 10.00m, Quantity = 1, Description = "Prodotto", VATRate = 22m, ftChargeItemCase = 0x3 },
                new ChargeItem { Amount = -1.00m, Quantity = 1, Description = "Sconto subtotale", VATRate = 22m, ftChargeItemCase = 0x0000_0100_0000_0000 }
            };
            var doc = EpsonRTServerMapping.BuildFiscalDocument(RequestWith(items, 9.00m), NewTillState(), 0);

            doc.CreateReceiptXml.Should().Contain("<printRecSubtotalAdjustment adjustmentType=\"1\" description=\"Sconto subtotale\" amount=\"1.00\" />");
            doc.AmountCents.Should().Be(900);
        }

        [Fact]
        public void BuildFiscalDocument_SingleUseVoucher_Should_Emit_Adjustment_Type12()
        {
            var items = new[]
            {
                new ChargeItem { Amount = 10.00m, Quantity = 1, Description = "Prodotto", VATRate = 22m, ftChargeItemCase = 0x3 },
                new ChargeItem { Amount = -3.00m, Quantity = 1, Description = "Buono", VATRate = 22m, ftChargeItemCase = 0x40 }
            };
            var doc = EpsonRTServerMapping.BuildFiscalDocument(RequestWith(items, 7.00m), NewTillState(), 0);

            doc.CreateReceiptXml.Should().Contain("<printRecItemAdjustment adjustmentType=\"12\" description=\"Buono\" amount=\"3.00\"");
            doc.AmountCents.Should().Be(700);
        }

        [Fact]
        public void BuildFiscalDocument_VoidItem_Should_Emit_PrintRecItemVoid()
        {
            var items = new[]
            {
                new ChargeItem { Amount = 10.00m, Quantity = 1, Description = "Prodotto", VATRate = 22m, ftChargeItemCase = 0x3 },
                new ChargeItem { Amount = 4.00m, Quantity = 1, Description = "Storno", VATRate = 22m, ftChargeItemCase = 0x0000_0000_0001_0000 }
            };
            var doc = EpsonRTServerMapping.BuildFiscalDocument(RequestWith(items, 6.00m), NewTillState(), 0);

            doc.CreateReceiptXml.Should().Contain("<printRecItemVoid description=\"Storno\"");
            doc.AmountCents.Should().Be(600);
        }

        [Fact]
        public void BuildFiscalDocument_ZeroAmountItem_Should_Emit_PrintRecMessage()
        {
            var items = new[]
            {
                new ChargeItem { Amount = 10.00m, Quantity = 1, Description = "Prodotto", VATRate = 22m, ftChargeItemCase = 0x3 },
                new ChargeItem { Amount = 0m, Quantity = 0, Description = "Nota descrittiva", ftChargeItemCase = 0x3 }
            };
            var doc = EpsonRTServerMapping.BuildFiscalDocument(RequestWith(items, 10.00m), NewTillState(), 0);

            doc.CreateReceiptXml.Should().Contain("<printRecMessage message=\"Nota descrittiva\" />");
            doc.AmountCents.Should().Be(1000);
        }

        [Theory]
        [InlineData(0x3, 1)]  // 22%
        [InlineData(0x1, 2)]  // 10%
        [InlineData(0x2, 3)]  // 4%
        [InlineData(0x4, 4)]  // 5%
        public void GetVatId_Should_Map_Rates(long chargeItemCase, int expectedVatId)
        {
            EpsonRTServerMapping.GetVatId(new ChargeItem { ftChargeItemCase = chargeItemCase }).Should().Be(expectedVatId);
        }

        [Theory]
        [InlineData(0x00, 0)] // cash
        [InlineData(0x03, 1)] // cheque
        [InlineData(0x04, 2)] // electronic
        public void GetEpsonPaymentType_Should_Map_PaymentTypes(long payItemCase, int expectedPaymentType)
        {
            EpsonRTServerMapping.GetEpsonPaymentType(new PayItem { ftPayItemCase = payItemCase }).PaymentType.Should().Be(expectedPaymentType);
        }
    }
}
