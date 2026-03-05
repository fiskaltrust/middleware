using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest
{
    /// <summary>
    /// Focused tests to validate error messages and conditions for invoice cancellation
    /// </summary>
    public class CancelInvoiceValidationTests
    {

        //
        // Test 1: 
        // Restaurant Order VOID (AADE 8.6 multiple cancellation)
        //
        [Fact]
        public void MapToInvoicesDoc_RestaurantOrderVoid_SetsMultipleConnectedMarks()
        {
            // Arrange
            long[] previousMarks =
            {
            4000019580341891L,
            4000019580341892L,
            4000019580341893L
        };

            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                    .WithCase(ReceiptCase.Order0x3004),

                cbTerminalID = "1",

                cbCustomer = new MiddlewareCustomer
                {
                    CustomerCountry = "GR",
                    CustomerVATId = "026883248"
                },

                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),

                cbPreviousReceiptReference = previousMarks.Select(x => x.ToString()).ToArray(),
                cbArea = "105",
                ftPosSystemId = Guid.NewGuid(),

                // Multiple void lines → must collapse into ONE
                cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Quantity = 1,
                Description = "Bottle cocal cola 1",
                Amount = 0.0m,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000)
                    .WithFlag(ChargeItemCaseFlags.Void)
                    .WithVat(ChargeItemCase.NotTaxable),
                VATRate = 0m,
                VATAmount = 0m,
                Position = 1
            },
            new ChargeItem
            {
                Quantity = 2,
                Description = "Bottle cocal cola 2",
                Amount = 0.0m,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000)
                    .WithFlag(ChargeItemCaseFlags.Void)
                    .WithVat(ChargeItemCase.NotTaxable),
                VATRate = 0m,
                VATAmount = 0m,
                Position = 2
            }
        },

                cbPayItems = new List<PayItem>()
            };

            var receiptResponse = new ReceiptResponse
            {
                cbReceiptReference = receiptRequest.cbReceiptReference,
                ftCashBoxIdentification = "CB001",
                ftReceiptIdentification = "ft123#"
            };

            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");

            // Act
            (var doc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            // Assert
            error.Should().BeNull();
            doc.Should().NotBeNull();

            var invoice = doc!.invoice[0];
            var header = invoice.invoiceHeader;

            // Header validations
            header.invoiceType.Should().Be(InvoiceType.Item86);
            header.multipleConnectedMarks.Should().NotBeNull();
            header.multipleConnectedMarks.Should().BeEquivalentTo(previousMarks);
            header.totalCancelDeliveryOrders.Should().BeTrue();
            header.tableAA.Should().Be("105");

            // MUST contain exactly one detail row
            invoice.invoiceDetails.Should().HaveCount(1);

            var line = invoice.invoiceDetails[0];

            line.lineNumber.Should().Be(1);
            line.quantity.Should().Be(1);
            line.netValue.Should().Be(0.00m);
            line.vatCategory.Should().Be(8);
            line.vatAmount.Should().Be(0.00m);

            // No classifications
            line.incomeClassification.Should().BeNullOrEmpty();
            line.expensesClassification.Should().BeNullOrEmpty();

            // Summary must be all zeros
            var summary = invoice.invoiceSummary;

            summary.Should().NotBeNull();
            summary.totalNetValue.Should().Be(0.00m);
            summary.totalVatAmount.Should().Be(0.00m);
            summary.totalWithheldAmount.Should().Be(0m);
            summary.totalFeesAmount.Should().Be(0m);
            summary.totalStampDutyAmount.Should().Be(0m);
            summary.totalOtherTaxesAmount.Should().Be(0m);
            summary.totalDeductionsAmount.Should().Be(0m);
            summary.totalGrossValue.Should().Be(0.00m);
        }

        //
        // Test 2: 
        // Restaurant Order VOID (AADE 8.6 single cancellation)
        //
        [Fact]
        public void MapToInvoicesDoc_RestaurantOrderVoid_SetsSingleConnectedMark()
        {
            // Arrange
            var previousMark = 4000019580341891;

            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                    .WithCase(ReceiptCase.Order0x3004),

                cbTerminalID = "1",

                cbCustomer = new MiddlewareCustomer
                {
                    CustomerCountry = "GR",
                    CustomerVATId = "026883248"
                },

                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),

                // SINGLE MARK
                cbPreviousReceiptReference = new[] { previousMark.ToString() },

                cbArea = "105",
                ftPosSystemId = Guid.NewGuid(),

                cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Quantity = 1,
                Description = "Bottle cocal cola",
                Amount = 0.0m,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000)
                    .WithFlag(ChargeItemCaseFlags.Void)
                    .WithVat(ChargeItemCase.NotTaxable),

                VATRate = 0m,
                VATAmount = 0m,
                Position = 1
            }
        },

                cbPayItems = new List<PayItem>()
            };

            var receiptResponse = new ReceiptResponse
            {
                cbReceiptReference = receiptRequest.cbReceiptReference,
                ftCashBoxIdentification = "CB001",
                ftReceiptIdentification = "ft123#"
            };

            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");

            // Act
            (var doc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            // Assert
            error.Should().BeNull();
            doc.Should().NotBeNull();

            var invoice = doc!.invoice[0];
            var header = invoice.invoiceHeader;

            // Header assertions
            header.invoiceType.Should().Be(InvoiceType.Item86);

            // Must set SINGLE connected mark
            header.multipleConnectedMarks.Should().NotBeNull();
            header.multipleConnectedMarks.Should().HaveCount(1);
            header.multipleConnectedMarks.Should()
                  .BeEquivalentTo(new[] { previousMark });

            header.totalCancelDeliveryOrders.Should().BeTrue();
            header.tableAA.Should().Be("105");

            // Must have exactly ONE detail line
            invoice.invoiceDetails.Should().HaveCount(1);

            var line = invoice.invoiceDetails[0];
            line.lineNumber.Should().Be(1);
            line.quantity.Should().Be(1);
            line.netValue.Should().Be(0.00m);
            line.vatCategory.Should().Be(8);
            line.vatAmount.Should().Be(0.00m);

            // No classifications
            line.incomeClassification.Should().BeNullOrEmpty();
            line.expensesClassification.Should().BeNullOrEmpty();

            // Summary must be zero
            var summary = invoice.invoiceSummary;

            summary.totalNetValue.Should().Be(0.00m);
            summary.totalVatAmount.Should().Be(0.00m);
            summary.totalWithheldAmount.Should().Be(0m);
            summary.totalFeesAmount.Should().Be(0m);
            summary.totalStampDutyAmount.Should().Be(0m);
            summary.totalOtherTaxesAmount.Should().Be(0m);
            summary.totalDeductionsAmount.Should().Be(0m);
            summary.totalGrossValue.Should().Be(0.00m);
        }

        //
        // Test 3: Missing MARKs should cause error
        // Validates that AADE VOID (8.6) always requires at least one previous MARK in cbPreviousReceiptReference.
        //
        [Fact]
        public void MapToInvoicesDoc_RestaurantOrderVoid_MissingPreviousMarks_ReturnsError()
        {
            var receiptRequest = new ReceiptRequest
            {
                // cbPreviousReceiptReference is intentionally missing/null
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Order0x3004),
                cbTerminalID = "1",
                cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbArea = "105",
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem { Quantity = 1, Description = "VOID item", Amount = 0.0m, VATRate = 0, VATAmount = 0, Position = 1 }
        },
                cbPayItems = new List<PayItem>()
            };
            var receiptResponse = new ReceiptResponse
            {
                cbReceiptReference = receiptRequest.cbReceiptReference,
                ftCashBoxIdentification = "CB001",
                ftReceiptIdentification = "ft123#"
            };
            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");
            var (doc, error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            error.Should().NotBeNull();
            error.Exception.Message.Should().Contain("MultipleConnectedMarks");
            doc.Should().BeNull();
        }

        //
        // Test 4: Non-zero amount should cause error
        // Validates that every VOID charge item must have Amount = 0, per AADE specs.
        //
        [Fact]
        public void MapToInvoicesDoc_RestaurantOrderVoid_NonZeroChargeItemAmount_ReturnsError()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbPreviousReceiptReference = new[] { "4000019580341891" },
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Order0x3004),
                cbTerminalID = "1",
                cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbArea = "105",
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem { Quantity = 1, Description = "VOID item", Amount = 99.99m, VATRate = 0, VATAmount = 0, Position = 1 }
        },
                cbPayItems = new List<PayItem>()
            };
            var receiptResponse = new ReceiptResponse
            {
                cbReceiptReference = receiptRequest.cbReceiptReference,
                ftCashBoxIdentification = "CB001",
                ftReceiptIdentification = "ft123#"
            };
            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");
            var (doc, error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            error.Should().NotBeNull();
            error.Exception.Message.Should().Contain("Amount = 0");
            doc.Should().BeNull();
        }

        //
        // Test 5: Invalid MARK string should cause error
        // Validates parsing and error propagation for non-numeric MARKs in VOID.
        //
        [Fact]
        public void MapToInvoicesDoc_RestaurantOrderVoid_InvalidMarkString_ReturnsError()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbPreviousReceiptReference = new[] { "not_a_number" },
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Order0x3004),
                cbTerminalID = "1",
                cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbArea = "105",
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem { Quantity = 1, Description = "VOID item", Amount = 0.0m, VATRate = 0, VATAmount = 0, Position = 1 }
        },
                cbPayItems = new List<PayItem>()
            };
            var receiptResponse = new ReceiptResponse
            {
                cbReceiptReference = receiptRequest.cbReceiptReference,
                ftCashBoxIdentification = "CB001",
                ftReceiptIdentification = "ft123#"
            };
            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");
            var (doc, error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            error.Should().NotBeNull();
            error.Exception.Message.Should().Contain("Invalid MARK format");
            doc.Should().BeNull();
        }

        //
        // Test 6: Missing charge items should cause error
        // Validates AADE logic: VOID must have at least one charge item present.
        //
        [Fact]
        public void MapToInvoicesDoc_RestaurantOrderVoid_MissingChargeItems_ReturnsError()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbPreviousReceiptReference = new[] { "4000019580341891" },
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Order0x3004),
                cbTerminalID = "1",
                cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbArea = "105",
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>(), // Empty list
                cbPayItems = new List<PayItem>()
            };
            var receiptResponse = new ReceiptResponse
            {
                cbReceiptReference = receiptRequest.cbReceiptReference,
                ftCashBoxIdentification = "CB001",
                ftReceiptIdentification = "ft123#"
            };
            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");
            var (doc, error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            error.Should().NotBeNull();
            error.Exception.Message.Should().Contain("at least one charge item");
            doc.Should().BeNull();
        }


        //
        // Test 7: Missing tableAA should cause error for 8.6
        // Validates that AADE VOID (8.6) requires cbArea/tableAA.
        //
        [Fact]
        public void MapToInvoicesDoc_RestaurantOrderVoid_MissingTableAA_ReturnsError()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbPreviousReceiptReference = new[] { "4000019580341891" },
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Order0x3004),
                cbTerminalID = "1",
                cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem { Quantity = 1, Description = "VOID item", Amount = 0.0m, VATRate = 0, VATAmount = 0, Position = 1 }
                },
                cbPayItems = new List<PayItem>()
            };
            var receiptResponse = new ReceiptResponse
            {
                cbReceiptReference = receiptRequest.cbReceiptReference,
                ftCashBoxIdentification = "CB001",
                ftReceiptIdentification = "ft123#"
            };
            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");
            var (doc, error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            error.Should().NotBeNull();
            error.Exception.Message.Should().Contain("TableAA (cbArea) must be provided");
            doc.Should().BeNull();
        }


        // Helper for master data setup
        private storage.V0.MasterData.MasterDataConfiguration MockMasterData() =>
            new storage.V0.MasterData.MasterDataConfiguration
            {
                Account = new storage.V0.MasterData.AccountMasterData { VatId = "112545020" }
            };

    }
}
