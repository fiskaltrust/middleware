using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest
{   
    /// <summary>
    /// Tests for partial return/cancellation of individual lines within a restaurant order (8.6).
    /// A partial return is signalled per charge item via ChargeItemCaseFlags.Refund (0x0002),
    /// NOT by a receipt-level Void or Refund flag.
    /// myDATA spec: recType=7 lines must carry positive amounts; myDATA treats them as cancellations.
    /// </summary>
    public class PartiallyCancelInvoiceValidationTests
    {
        //
        // Test 1:
        // A single returned line in an 8.6 order must produce recType=7 with positive amounts.
        //
        [Fact]
        public void MapToInvoicesDoc_Order86_SingleReturnedLine_SetsRecType7WithPositiveAmounts()
        {
            var receiptRequest = new ReceiptRequest
            {
                // 8.6 order — NO Void flag on the receipt
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                    .WithCase(ReceiptCase.Order0x3004),
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbArea = "1",
                cbPreviousReceiptReference = "Previous-Reference",
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Position         = 1,
                        Quantity         = 1,
                        Description      = "Bottle of wine",
                        Amount           = 12.40m,
                        VATRate          = 24m,
                        VATAmount        = 2.40m,
                        // ftChargeItemCase 0x0000_2000_0002_0013 — 0x0002 = return flag
                        ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0013)
                            .WithFlag(ChargeItemCaseFlags.Refund)
                    }
                },
                cbPayItems = new List<PayItem>()
            };

            var receiptResponse = new ReceiptResponse
            {
                cbReceiptReference      = receiptRequest.cbReceiptReference,
                ftCashBoxIdentification = "CB001",
                ftReceiptIdentification = "ft123#"
            };
            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");

            var (docInvoice, error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            using (new AssertionScope())
            {
                error.Should().BeNull();
                docInvoice.Should().NotBeNull();

                var invoice = docInvoice!.invoice[0];

                // header must still be a normal 8.6, NOT a full void
                invoice.invoiceHeader.invoiceType.Should().Be(InvoiceType.Item86);
                invoice.invoiceHeader.totalCancelDeliveryOrders.Should().BeFalse(
                    "partial return must not trigger full void behaviour");
                invoice.invoiceHeader.tableAA.Should().Be("1");

                var line = invoice.invoiceDetails.Single();
                line.recTypeSpecified.Should().BeTrue();
                line.recType.Should().Be(7);
                line.netValue.Should().BePositive("myDATA requires positive amounts for recType=7");
                line.vatAmount.Should().BePositive();
                line.quantity.Should().BePositive();
                line.itemDescr.Should().Be("Bottle of wine");
                line.incomeClassification.Should().NotBeNull();
                line.incomeClassification.Should().AllSatisfy(c =>
                    c.amount.Should().BePositive("myDATA requires positive incomeClassification amount for recType=7"));
            }
        }

        //
        // Test 2:
        // Mixed order: normal lines must NOT have recType set; only the returned line gets recType=7.
        //
        [Fact]
        public void MapToInvoicesDoc_Order86_MixedLines_OnlyReturnedLineHasRecType7()
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                    .WithCase(ReceiptCase.Order0x3004),
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbArea = "1",
                cbPreviousReceiptReference = "Previous-Reference",
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>
                {
                    // Normal line
                    new ChargeItem
                    {
                        Position         = 1,
                        Quantity         = 2,
                        Description      = "Pizza Margherita",
                        Amount           = 16.00m,
                        VATRate          = 24m,
                        VATAmount        = decimal.Round(16m / 124m * 24m, 2),
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013
                    },
                    // Returned line
                    new ChargeItem
                    {
                        Position         = 2,
                        Quantity         = 1,
                        Description      = "Bottle of wine",
                        Amount           = 12.40m,
                        VATRate          = 24m,
                        VATAmount        = 2.40m,
                        ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0013)
                            .WithFlag(ChargeItemCaseFlags.Refund)
                    }
                },
                cbPayItems = new List<PayItem>()
            };

            var receiptResponse = new ReceiptResponse
            {
                cbReceiptReference      = receiptRequest.cbReceiptReference,
                ftCashBoxIdentification = "CB001",
                ftReceiptIdentification = "ft123#"
            };
            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");

            var (docInvoice, error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            using (new AssertionScope())
            {
                error.Should().BeNull();
                docInvoice.Should().NotBeNull();

                var invoice = docInvoice!.invoice[0];

                // header must still be a normal 8.6, NOT a full void
                invoice.invoiceHeader.invoiceType.Should().Be(InvoiceType.Item86);
                invoice.invoiceHeader.totalCancelDeliveryOrders.Should().BeFalse(
                    "partial return must not trigger full void behaviour");
                invoice.invoiceHeader.tableAA.Should().Be("1");

                var details = invoice.invoiceDetails;
                details.Should().HaveCount(2);

                var normalLine = details.Single(d => d.lineNumber == 1);
                normalLine.recTypeSpecified.Should().BeFalse("normal lines must not carry recType");

                var returnLine = details.Single(d => d.lineNumber == 2);
                returnLine.recTypeSpecified.Should().BeTrue();
                returnLine.recType.Should().Be(7);
                returnLine.netValue.Should().BePositive();
                returnLine.vatAmount.Should().BePositive();
                returnLine.quantity.Should().BePositive();
                returnLine.incomeClassification.Should().NotBeNull();
                returnLine.incomeClassification.Should().AllSatisfy(c =>
                    c.amount.Should().BePositive("myDATA requires positive incomeClassification amount for recType=7"));
            }
        }

        //
        // Test 3:
        // Amounts sent as negative in the payload (e.g. from VIVA).
        // Math.Abs() must normalise them to positive as required by myDATA for recType=7.
        //
        [Fact]
        public void MapToInvoicesDoc_Order86_ReturnedLineWithNegativeAmounts_OutputsPositiveAmounts()
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                    .WithCase(ReceiptCase.Order0x3004),
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbArea = "1",
                cbPreviousReceiptReference = "Previous-Reference",
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Position         = 1,
                        Quantity         = -1,      // negative — POS sent it this way
                        Description      = "Bottle of wine",
                        Amount           = -12.40m, // negative — POS sent it this way
                        VATRate          = 24m,
                        VATAmount        = -2.40m,  // negative — POS sent it this way
                        ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0013)
                            .WithFlag(ChargeItemCaseFlags.Refund)
                    }
                },
                cbPayItems = new List<PayItem>()
            };

            var receiptResponse = new ReceiptResponse
            {
                cbReceiptReference      = receiptRequest.cbReceiptReference,
                ftCashBoxIdentification = "CB001",
                ftReceiptIdentification = "ft123#"
            };
            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");

            var (docInvoice, error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            using (new AssertionScope())
            {
                error.Should().BeNull();
                docInvoice.Should().NotBeNull();

                var invoice = docInvoice!.invoice[0];

                invoice.invoiceHeader.invoiceType.Should().Be(InvoiceType.Item86);
                invoice.invoiceHeader.totalCancelDeliveryOrders.Should().BeFalse(
                    "partial return must not trigger full void behaviour");
                invoice.invoiceHeader.tableAA.Should().Be("1");

                var line = invoice.invoiceDetails.Single();
                line.recTypeSpecified.Should().BeTrue();
                line.recType.Should().Be(7);
                // Math.Abs() normalises negative payload values to positive for myDATA
                line.netValue.Should().BePositive("myDATA requires positive amounts for recType=7");
                line.vatAmount.Should().BePositive();
                line.quantity.Should().BePositive();
                line.incomeClassification.Should().NotBeNull();
                line.incomeClassification.Should().AllSatisfy(c =>
                    c.amount.Should().BePositive("myDATA requires positive incomeClassification amount for recType=7"));
            }
        }

        private static storage.V0.MasterData.MasterDataConfiguration MockMasterData() =>
            new storage.V0.MasterData.MasterDataConfiguration
            {
                Account = new storage.V0.MasterData.AccountMasterData { VatId = "112545020" }
            };
    }
}
