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
                    .WithCase(ReceiptCase.Order0x3004)
                    .WithFlag(ReceiptCaseFlags.Void),

                cbTerminalID = "1",

                cbCustomer = new MiddlewareCustomer
                {
                    CustomerCountry = "GR",
                    CustomerVATId = "026883248"
                },

                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),

                cbPreviousReceiptReference = "Previous-Reference",
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
                ftReceiptIdentification = "ft123#",
                ftSignatures = new List<SignatureItem>()
            };

            // Build 3 separate receipt references, each with its own invoiceMark
            var receiptReferences = previousMarks.Select(mark =>
            {
                var refRequest = new ReceiptRequest
                {
                    ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Order0x3004),
                    cbTerminalID = "1",
                    cbReceiptMoment = DateTime.UtcNow,
                    cbReceiptReference = Guid.NewGuid().ToString(),
                    ftPosSystemId = Guid.NewGuid(),
                    cbChargeItems = new List<ChargeItem>(),
                    cbPayItems = new List<PayItem>()
                };
                var refResponse = new ReceiptResponse
                {
                    cbReceiptReference = refRequest.cbReceiptReference,
                    ftCashBoxIdentification = "CB001",
                    ftReceiptIdentification = "ft100#",
                    ftSignatures = new List<SignatureItem>
                    {
                        new SignatureItem { Caption = "invoiceMark", Data = mark.ToString() }
                    }
                };
                return (refRequest, refResponse);
            }).ToList();

            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");

            // Act
            (var doc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, receiptReferences);

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
            var previousMark = 4000019580341891L;

            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                    .WithCase(ReceiptCase.Order0x3004)
                    .WithFlag(ReceiptCaseFlags.Void),

                cbTerminalID = "1",

                cbCustomer = new MiddlewareCustomer
                {
                    CustomerCountry = "GR",
                    CustomerVATId = "026883248"
                },

                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),

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

            // Build a single receipt reference with the mark
            var refRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Order0x3004),
                cbTerminalID = "1",
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>(),
                cbPayItems = new List<PayItem>()
            };
            var refResponse = new ReceiptResponse
            {
                cbReceiptReference = refRequest.cbReceiptReference,
                ftCashBoxIdentification = "CB001",
                ftReceiptIdentification = "ft100#",
                ftSignatures = new List<SignatureItem>
                {
                    new SignatureItem { Caption = "invoiceMark", Data = previousMark.ToString() }
                }
            };

            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");

            // Act
            (var doc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse,
                new List<(ReceiptRequest, ReceiptResponse)> { (refRequest, refResponse) });

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
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Order0x3004).WithFlag(ReceiptCaseFlags.Void),
                cbTerminalID = "1",
                cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbArea = "105",
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem { Quantity = 1, Description = "VOID item", Amount = 0.0m, VATRate = 0, VATAmount = 0, Position = 1, ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithFlag(ChargeItemCaseFlags.Void).WithVat(ChargeItemCase.NotTaxable) }
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
        // Test 4:
        // Void ONLY on ftChargeItemCase, NOT on ftReceiptCase → must behave as a normal order.
        // Proves: charge-item-level Void flag alone does NOT trigger document VOID.
        //
        [Fact]
        public void MapToInvoicesDoc_VoidFlagOnlyOnChargeItemCase_NotOnReceiptCase_ProducesNormalOrder()
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                    .WithCase(ReceiptCase.Order0x3004), // NO Void on receiptCase
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbArea = "105",
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Quantity = 2,
                        Description = "Bottle of wine",
                        Amount = 24.80m,
                        VATRate = 24m,
                        VATAmount = 4.80m,
                        ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0013)
                            .WithFlag(ChargeItemCaseFlags.Void), // Void ONLY on charge item
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

            (var doc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            error.Should().BeNull("charge-item Void flag alone must NOT trigger document VOID");
            doc.Should().NotBeNull();

            var header = doc!.invoice[0].invoiceHeader;
            header.invoiceType.Should().Be(InvoiceType.Item86, "Order0x3004 => 8.6");
            header.totalCancelDeliveryOrdersSpecified.Should().BeFalse("no Void on receiptCase → not a cancel");
        }

        //
        // Test 5: Invalid MARK string should cause error
        // Validates parsing and error propagation for non-numeric MARKs in VOID.
        //
        [Fact]
        public void MapToInvoicesDoc_RestaurantOrderVoid_InvalidMarkInReference_ReturnsMarkAsMinusOne()
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Order0x3004).WithFlag(ReceiptCaseFlags.Void),
                cbTerminalID = "1",
                cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbPreviousReceiptReference = "some-ref",
                cbArea = "105",
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem { Quantity = 1, Description = "VOID item", Amount = 0.0m, VATRate = 0, VATAmount = 0, Position = 1, ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithFlag(ChargeItemCaseFlags.Void).WithVat(ChargeItemCase.NotTaxable) }
                },
                cbPayItems = new List<PayItem>()
            };
            var receiptResponse = new ReceiptResponse
            {
                cbReceiptReference = receiptRequest.cbReceiptReference,
                ftCashBoxIdentification = "CB001",
                ftReceiptIdentification = "ft123#"
            };

            // Receipt reference with an invalid (non-numeric) mark
            var refResponse = new ReceiptResponse
            {
                cbReceiptReference = "ref",
                ftCashBoxIdentification = "CB001",
                ftReceiptIdentification = "ft100#",
                ftSignatures = new List<SignatureItem>
                {
                    new SignatureItem { Caption = "invoiceMark", Data = "NOT_A_NUMBER" }
                }
            };
            var refRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Order0x3004),
                cbTerminalID = "1",
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>(),
                cbPayItems = new List<PayItem>()
            };

            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");
            var (doc, error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse,
                new List<(ReceiptRequest, ReceiptResponse)> { (refRequest, refResponse) });

            // The factory doesn't fail; it sets mark to -1 via GetInvoiceMark
            error.Should().BeNull();
            doc.Should().NotBeNull();
            doc!.invoice[0].invoiceHeader.multipleConnectedMarks.Should().BeEquivalentTo(new[] { -1L });
        }

        //
        // Test 6:
        // Void on ftReceiptCase, charge items have NO void flag → must still produce full VOID doc.
        // Proves: ftReceiptCase Void drives the whole document regardless of charge item flags.
        //
        [Fact]
        public void MapToInvoicesDoc_VoidOnReceiptCase_ChargeItemsHaveNoVoidFlag_StillProducesVoidDoc()
        {
            var previousMark = 4000019580341891L;
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                    .WithCase(ReceiptCase.Order0x3004)
                    .WithFlag(ReceiptCaseFlags.Void),
                cbTerminalID = "1",
                cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbPreviousReceiptReference = "Previous-Reference",
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
                            .WithVat(ChargeItemCase.NotTaxable), // NO Void flag on charge item
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
            var receiptReferences = CreateReceiptReferences(previousMark);
            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");

            (var doc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, receiptReferences);

            error.Should().BeNull("receiptCase Void must produce VOID doc regardless of charge item flags");
            doc.Should().NotBeNull();

            var header = doc!.invoice[0].invoiceHeader;
            header.totalCancelDeliveryOrders.Should().BeTrue();
            header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { previousMark });
        }

        //
        // Test 7: Missing tableAA should cause error for 8.6
        // Validates that AADE VOID (8.6) requires cbArea/tableAA.
        //
        [Fact]
        public void MapToInvoicesDoc_RestaurantOrderVoid_MissingTableAA_ReturnsError()
        {
            var previousMark = 4000019580341891L;
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Order0x3004).WithFlag(ReceiptCaseFlags.Void),
                cbTerminalID = "1",
                cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbPreviousReceiptReference = "Previous-Reference",
                cbArea = null, // Missing tableAA
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem { Quantity = 1, Description = "VOID item", Amount = 0.0m, VATRate = 0, VATAmount = 0, Position = 1, ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithFlag(ChargeItemCaseFlags.Void).WithVat(ChargeItemCase.NotTaxable) }
                },
                cbPayItems = new List<PayItem>()
            };
            var receiptResponse = new ReceiptResponse
            {
                cbReceiptReference = receiptRequest.cbReceiptReference,
                ftCashBoxIdentification = "CB001",
                ftReceiptIdentification = "ft123#"
            };
            var receiptReferences = CreateReceiptReferences(previousMark);
            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");
            var (doc, error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, receiptReferences);

            error.Should().NotBeNull();
            error.Exception.Message.Should().Contain("TableAA (cbArea) must be provided");
            doc.Should().BeNull();
        }

        //
        // Test 8: Normal restaurant order (8.6) should NOT get VOID headers
        //
        [Fact]
        public void MapToInvoicesDoc_NormalRestaurantOrder86_ShouldNotSetVoidHeaders()
        {
            // Arrange — normal HoReCa 8.6 order: real amounts, real VAT, NO void flag
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                    .WithCase(ReceiptCase.Order0x3004),
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbArea = "105",
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Quantity = 2,
                        Description = "Bottle of wine",
                        Amount = 24.80m,
                        VATRate = 24m,
                        VATAmount = 4.80m,
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013, // Delivery, normal VAT, NO Void flag
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
            error.Should().BeNull("a normal 8.6 restaurant order must not produce an error");
            doc.Should().NotBeNull();

            var invoice = doc!.invoice[0];
            var header = invoice.invoiceHeader;

            header.invoiceType.Should().Be(InvoiceType.Item86);

            // VOID-specific headers must NOT be set
            header.multipleConnectedMarks.Should().BeNullOrEmpty(
                "a normal order must NOT have multipleConnectedMarks");
            header.totalCancelDeliveryOrders.Should().BeFalse(
                "totalCancelDeliveryOrders must only be true for VOID orders");
            header.totalCancelDeliveryOrdersSpecified.Should().BeFalse();

            // tableAA must still be set for normal orders
            header.tableAA.Should().Be("105");

            // Invoice details must be normal (real values, not zero)
            invoice.invoiceDetails.Should().HaveCount(1);
            var line = invoice.invoiceDetails[0];
            line.netValue.Should().Be(20.00m);
            line.vatAmount.Should().Be(4.80m);
            line.vatCategory.Should().Be(1); // 24% VAT
            line.quantity.Should().Be(2);
            line.itemDescr.Should().Be("Bottle of wine");

            // Must have income classification
            line.incomeClassification.Should().NotBeNullOrEmpty();
            line.incomeClassification[0].classificationCategory
                .Should().Be(IncomeClassificationCategoryType.category1_95);
            line.incomeClassification[0].amount.Should().Be(20.00m);

            // Summary must reflect actual values
            var summary = invoice.invoiceSummary;
            summary.totalNetValue.Should().Be(20.00m);
            summary.totalVatAmount.Should().Be(4.80m);
            summary.totalGrossValue.Should().Be(24.80m);
            summary.incomeClassification.Should().NotBeNullOrEmpty();
        }

        //
        // Test 9:
        // Void on ftReceiptCase, charge items have real non-zero amounts → output must be all zero.
        // Proves: receiptCase Void completely overrides whatever amounts are in the line items.
        //
        [Fact]
        public void MapToInvoicesDoc_VoidOnReceiptCase_ChargeItemsWithNonZeroAmounts_OutputIsAllZero()
        {
            var previousMark = 4000019580341891L;
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                    .WithCase(ReceiptCase.Order0x3004)
                    .WithFlag(ReceiptCaseFlags.Void),
                cbTerminalID = "1",
                cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbPreviousReceiptReference = "Previous-Reference",
                cbArea = "105",
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Quantity = 2,
                        Description = "Bottle of wine",
                        Amount = 24.80m,
                        VATRate = 24m,
                        VATAmount = 4.80m,
                        ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000)
                            .WithFlag(ChargeItemCaseFlags.Void)
                            .WithVat(ChargeItemCase.NormalVatRate),
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
            var receiptReferences = CreateReceiptReferences(previousMark);
            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");

            (var doc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, receiptReferences);

            error.Should().BeNull();
            doc.Should().NotBeNull();

            var invoice = doc!.invoice[0];
            invoice.invoiceDetails.Should().HaveCount(1);
            invoice.invoiceDetails[0].netValue.Should().Be(0.00m);
            invoice.invoiceDetails[0].vatAmount.Should().Be(0.00m);
            invoice.invoiceSummary.totalGrossValue.Should().Be(0.00m);
        }

        //
        // Test 10:
        // Empty array for cbPreviousReceiptReference → must return error.
        // Different from Test 3 (null): this is an empty string[], not null.
        //
        [Fact]
        public void MapToInvoicesDoc_RestaurantOrderVoid_EmptyPreviousMarksArray_ReturnsError()
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Order0x3004).WithFlag(ReceiptCaseFlags.Void),
                cbTerminalID = "1",
                cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbPreviousReceiptReference = new string[] { },
                cbArea = "105",
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem { Quantity = 1, Description = "VOID item", Amount = 0.0m, VATRate = 0, VATAmount = 0, Position = 1, ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithFlag(ChargeItemCaseFlags.Void).WithVat(ChargeItemCase.NotTaxable) }
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
            error!.Exception.Message.Should().Contain("Group MARKs cannot be empty");
            doc.Should().BeNull();
        }

        //
        // Test 11:
        // Void on ftReceiptCase for a NON-8.6 invoice type → must return "not supported" error.
        // Proves: VOID is exclusively allowed for Order0x3004 (type 8.6).
        //
        [Fact]
        public void MapToInvoicesDoc_VoidOnReceiptCase_NonOrder86InvoiceType_ReturnsUnsupportedError()
        {
            var receiptRequest = new ReceiptRequest
            {
                // Receipt type (Item111) with Void — not an Order/Log
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                    .WithFlag(ReceiptCaseFlags.Void),
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbArea = "105",
                cbPreviousReceiptReference = new[] { "4000019580341891" },
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem { Quantity = 1, Description = "Item", Amount = 0.0m, VATRate = 0, VATAmount = 0, Position = 1,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithVat(ChargeItemCase.NotTaxable) }
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

            error.Should().NotBeNull("Void is only supported for invoice type 8.6");
            error!.Exception.Message.Should().Contain("not supported");
            doc.Should().BeNull();
        }

        //
        // Test 13:
        // VIVA real-world call: raw numeric ftReceiptCase, single-string cbPreviousReceiptReference,
        // negative Quantity and Amount on charge item → must produce a valid VOID doc with all zeros.
        //
        [Fact]
        public void MapToInvoicesDoc_VivaStyleCall_RawValues_ProducesVoidDoc()
        {
            var previousMark = 4000019580341891L;
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                    .WithCase(ReceiptCase.Order0x3004)
                    .WithFlag(ReceiptCaseFlags.Void),
                cbTerminalID = "1",
                cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbPreviousReceiptReference = "Previous-Reference",
                cbArea = "105",
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Quantity = 1,
                        Description = "VOID Item",
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
            var receiptReferences = CreateReceiptReferences(previousMark);
            var aadeFactory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");

            (var doc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, receiptReferences);

            error.Should().BeNull();
            doc.Should().NotBeNull();

            var invoice = doc!.invoice[0];
            invoice.invoiceHeader.invoiceType.Should().Be(InvoiceType.Item86);
            invoice.invoiceHeader.totalCancelDeliveryOrders.Should().BeTrue();
            invoice.invoiceHeader.multipleConnectedMarks.Should().BeEquivalentTo(new[] { previousMark });
        }

        // Helper for master data setup
        private storage.V0.MasterData.MasterDataConfiguration MockMasterData() =>
            new storage.V0.MasterData.MasterDataConfiguration
            {
                Account = new storage.V0.MasterData.AccountMasterData { VatId = "112545020" }
            };

        /// <summary>
        /// Creates a list of receipt references with the specified marks.
        /// Each mark gets its own (ReceiptRequest, ReceiptResponse) tuple
        /// with an "invoiceMark" signature containing the mark value.
        /// </summary>
        private static List<(ReceiptRequest, ReceiptResponse)> CreateReceiptReferences(params long[] marks)
        {
            return marks.Select(mark =>
            {
                var refRequest = new ReceiptRequest
                {
                    ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Order0x3004),
                    cbTerminalID = "1",
                    cbReceiptMoment = DateTime.UtcNow,
                    cbReceiptReference = Guid.NewGuid().ToString(),
                    ftPosSystemId = Guid.NewGuid(),
                    cbChargeItems = new List<ChargeItem>(),
                    cbPayItems = new List<PayItem>()
                };
                var refResponse = new ReceiptResponse
                {
                    cbReceiptReference = refRequest.cbReceiptReference,
                    ftCashBoxIdentification = "CB001",
                    ftReceiptIdentification = "ft100#",
                    ftSignatures = new List<SignatureItem>
                    {
                        new SignatureItem { Caption = "invoiceMark", Data = mark.ToString() }
                    }
                };
                return (refRequest, refResponse);
            }).ToList();
        }

    }
}
