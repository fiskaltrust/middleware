using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.SCU.MyData
{
    public class WithholdingTaxTests
    {
        private readonly MasterDataConfiguration _masterDataConfiguration;

        public WithholdingTaxTests()
        {
            _masterDataConfiguration = new MasterDataConfiguration
            {
                Account = new AccountMasterData
                {
                },
                Outlet = new OutletMasterData
                {
                }
            };
        }
   
        [Fact]
        public void MapToInvoicesDoc_ShouldProcessPercentageWithholdingTax()
        {
            // Arrange
            var factory = new AADEFactory(_masterDataConfiguration);
            
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftPosSystemId = Guid.NewGuid(),
                cbTerminalID = "T001",
                cbReceiptReference = "R001",
                cbReceiptMoment = DateTime.UtcNow,
                ftReceiptCase = (ReceiptCase)0x4752000000000001,
                cbChargeItems = 
                [
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x4752000000000013,
                        Description = "Service",
                        Amount = 100m,
                        VATRate = 24m,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Περιπτ. β’- Τόκοι - 15%",
                        Amount = -15m, // 15% of net amount (80.65 * 0.15 ≈ 12.10, but let's use exact amount)
                        VATRate = 0m,
                        Position = 2
                    }
                ],
                cbPayItems = [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase)0x4752000000000001,
                        Amount = 85m
                    }
                ]
            };

            var receiptResponse = new ReceiptResponse
            {
                ftReceiptIdentification = "ft123456789",
                ftCashBoxIdentification = "CB001",
                ftState = (State)0x4752000000000000,
                ftSignatures = []
            };

            // Act
            var (invoiceDoc, error) = factory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            // Assert
            error.Should().BeNull();
            invoiceDoc.Should().NotBeNull();
            
            var invoice = invoiceDoc.invoice[0];
            
            // Should have tax totals for withholding tax
            invoice.taxesTotals.Should().NotBeNullOrEmpty();
            invoice.taxesTotals.Length.Should().Be(1);
            
            var withholdingTax = invoice.taxesTotals[0];
            withholdingTax.taxType.Should().Be(1); // Withholding tax type
            withholdingTax.taxCategory.Should().Be(1); // Interest code
            withholdingTax.taxAmount.Should().Be(15m);
            
            // Invoice summary should include withholding tax
            invoice.invoiceSummary.totalWithheldAmount.Should().Be(15m);
            invoice.invoiceSummary.totalGrossValue.Should().Be(85m);
            invoice.invoiceSummary.totalNetValue.Should().Be(80.65m);

            // Should only have one invoice detail (the regular item, not the withholding tax item)
            invoice.invoiceDetails.Length.Should().Be(1);
            invoice.invoiceDetails[0].netValue.Should().BeApproximately(80.65m, 0.01m); // 100 / 1.24 ≈ 80.65
        }

        [Fact]
        public void MapToInvoicesDoc_ShouldThrowException_WhenWithholdingTaxDescriptionNotMapped()
        {
            // Arrange
            var factory = new AADEFactory(_masterDataConfiguration);
            
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftPosSystemId = Guid.NewGuid(),
                cbTerminalID = "T001",
                cbReceiptReference = "R001",
                cbReceiptMoment = DateTime.UtcNow,
                ftReceiptCase = (ReceiptCase)0x4752000000000001,
                cbChargeItems =
                [                    
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x4752000000000013,
                        Description = "Service",
                        Amount = 100m,
                        VATRate = 24m,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Unknown withholding tax",
                        Amount = -10m,
                        VATRate = 0m,
                        Position = 1
                    }
                ],
                cbPayItems = 
                [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase)0x4752000000000001,
                        Amount = 90m
                    }
                ]
            };

            var receiptResponse = new ReceiptResponse
            {
                ftReceiptIdentification = "ft123456789",
                ftCashBoxIdentification = "CB001",
                ftState = (State)0x4752000000000000,
                ftSignatures = []
            };

            // Act & Assert
            var (invoiceDoc, error) = factory.MapToInvoicesDoc(receiptRequest, receiptResponse);
            
            error.Should().NotBeNull();
            error.Exception.Message.Should().Contain("No withholding tax or fee mapping found");
            error.Exception.Message.Should().Contain("Unknown withholding tax");
        }

        [Fact]
        public void MapToInvoicesDoc_ShouldHandleFixedAmountWithholdingTax()
        {
            // Arrange
            var factory = new AADEFactory(_masterDataConfiguration);
            
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftPosSystemId = Guid.NewGuid(),
                cbTerminalID = "T001",
                cbReceiptReference = "R001",
                cbReceiptMoment = DateTime.UtcNow,
                ftReceiptCase = (ReceiptCase) 0x4752000000000001,
                cbChargeItems =
                [
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x4752000000000013,
                        Description = "Service",
                        Amount = 1000m,
                        VATRate = 24m,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Παρακράτηση Ειδικής Εισφοράς Αλληλεγγύης", // Fixed amount type
                        Amount = -50m,
                        VATRate = 0m,
                        Position = 2
                    }
                ],
                cbPayItems = 
                [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase) 0x4752000000000001,
                        Amount = 950m
                    }
                ]
            };

            var receiptResponse = new ReceiptResponse
            {
                ftReceiptIdentification = "ft123456789",
                ftCashBoxIdentification = "CB001",
                ftState = (State)0x4752000000000000,
                ftSignatures = []
            };

            // Act
            var (invoiceDoc, error) = factory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            // Assert
            error.Should().BeNull();
            invoiceDoc.Should().NotBeNull();
            
            var invoice = invoiceDoc.invoice[0];
            var withholdingTax = invoice.taxesTotals[0];

            withholdingTax.taxType.Should().Be(1); // Withholding tax type
            withholdingTax.taxCategory.Should().Be(14); // Special solidarity contribution
            withholdingTax.taxAmount.Should().Be(50m);
            withholdingTax.underlyingValueSpecified.Should().BeFalse(); // No underlying value for fixed amount

            invoice.invoiceSummary.totalWithheldAmount.Should().Be(50m);
            invoice.invoiceSummary.totalGrossValue.Should().Be(950m);
            invoice.invoiceSummary.totalNetValue.Should().Be(806.45m);

            // Should only have one invoice detail (the regular item, not the withholding tax item)
            invoice.invoiceDetails.Length.Should().Be(1);
            invoice.invoiceDetails[0].netValue.Should().Be(806.45m); 

        }

        [Fact]
        public void MapToInvoicesDoc_ShouldHandleMultipleWithholdingTaxes()
        {
            // Arrange
            var factory = new AADEFactory(_masterDataConfiguration);
            
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftPosSystemId = Guid.NewGuid(),
                cbTerminalID = "T001",
                cbReceiptReference = "R001",
                cbReceiptMoment = DateTime.UtcNow,
                ftReceiptCase = (ReceiptCase) 0x4752000000000001,
                cbChargeItems =
                [
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x4752000000000013,
                        Description = "Service",
                        Amount = 1000m,
                        VATRate = 24m,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Περιπτ. β’- Τόκοι - 15%",
                        Amount = -121m, // 15% of net amount (806.45 * 0.15 ≈ 121)
                        VATRate = 0m,
                        Position = 2
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Παρακράτηση Ειδικής Εισφοράς Αλληλεγγύης",
                        Amount = -25m,
                        VATRate = 0m,
                        Position = 3
                    }
                ],
                cbPayItems =
                [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase)0x4752000000000001,
                        Amount = 854m // 1000 - 121 - 25 = 854
                    }
                ]
            };

            var receiptResponse = new ReceiptResponse
            {
                ftReceiptIdentification = "ft123456789",
                ftCashBoxIdentification = "CB001",
                ftState = (State)0x4752000000000000,
                ftSignatures = []
            };

            // Act
            var (invoiceDoc, error) = factory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            // Assert
            error.Should().BeNull();
            invoiceDoc.Should().NotBeNull();
            
            var invoice = invoiceDoc.invoice[0];
            
            // Should have multiple withholding taxes
            invoice.taxesTotals.Length.Should().Be(2);
            
            var interestTax = invoice.taxesTotals.FirstOrDefault(t => t.taxCategory == 1);
            interestTax.Should().NotBeNull();
            interestTax.taxAmount.Should().Be(121m);
            
            var solidarityTax = invoice.taxesTotals.FirstOrDefault(t => t.taxCategory == 14);
            solidarityTax.Should().NotBeNull();
            solidarityTax.taxAmount.Should().Be(25m);
            
            // Total withholding tax should be sum of both
            invoice.invoiceSummary.totalWithheldAmount.Should().Be(146m); // 121 + 25
            invoice.invoiceSummary.totalGrossValue.Should().Be(854m);
            invoice.invoiceSummary.totalNetValue.Should().Be(806.45m);

            // Should only have one invoice detail (the regular item, not the withholding tax item)
            invoice.invoiceDetails.Length.Should().Be(1);
            invoice.invoiceDetails[0].netValue.Should().Be(806.45m);
        }

        #region Fee Tests

        [Fact]
        public void MapToInvoicesDoc_ShouldProcessPercentageFee()
        {
            // Arrange
            var factory = new AADEFactory(_masterDataConfiguration);
            
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftPosSystemId = Guid.NewGuid(),
                cbTerminalID = "T001",
                cbReceiptReference = "R001",
                cbReceiptMoment = DateTime.UtcNow,
                ftReceiptCase = (ReceiptCase)0x4752000000000001,
                cbChargeItems = 
                [
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x4752000000000013,
                        Description = "Service",
                        Amount = 100m,
                        VATRate = 24m,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Για μηνιαίο λογαριασμό μέχρι και 50 ευρώ 12%",
                        Amount = 12m, // 12% fee
                        VATRate = 0m,
                        Position = 2
                    }
                ],
                cbPayItems = [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase)0x4752000000000001,
                        Amount = 112m
                    }
                ]
            };

            var receiptResponse = new ReceiptResponse
            {
                ftReceiptIdentification = "ft123456789",
                ftCashBoxIdentification = "CB001",
                ftState = (State)0x4752000000000000,
                ftSignatures = []
            };

            // Act
            var (invoiceDoc, error) = factory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            // Assert
            error.Should().BeNull();
            invoiceDoc.Should().NotBeNull();
            
            var invoice = invoiceDoc.invoice[0];
            
            // Should have tax totals for fee
            invoice.taxesTotals.Should().NotBeNullOrEmpty();
            invoice.taxesTotals.Length.Should().Be(1);
            
            var fee = invoice.taxesTotals[0];
            fee.taxType.Should().Be(2); // Fee tax type
            fee.taxCategory.Should().Be(1); // Monthly bill up to 50 EUR code
            fee.taxAmount.Should().Be(12m);

            // Invoice summary should include fee
            invoice.invoiceSummary.totalFeesAmount.Should().Be(12m);
            invoice.invoiceSummary.totalGrossValue.Should().Be(112m);
            invoice.invoiceSummary.totalNetValue.Should().Be(80.65m);

            // Should only have one invoice detail (the regular item, not the fee item)
            invoice.invoiceDetails.Length.Should().Be(1);
            invoice.invoiceDetails[0].netValue.Should().BeApproximately(80.65m, 0.01m);
        }

        [Fact]
        public void MapToInvoicesDoc_ShouldHandleFixedAmountFee()
        {
            // Arrange
            var factory = new AADEFactory(_masterDataConfiguration);
            
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftPosSystemId = Guid.NewGuid(),
                cbTerminalID = "T001",
                cbReceiptReference = "R001",
                cbReceiptMoment = DateTime.UtcNow,
                ftReceiptCase = (ReceiptCase) 0x4752000000000001,
                cbChargeItems =
                [
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x4752000000000013,
                        Description = "Service",
                        Amount = 1000m,
                        VATRate = 24m,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Περιβαλλοντικό Τέλος & πλαστικής σακούλας ν. 2339/2001 αρ. 6α – 0,07 ευρώ ανά τεμάχιο", // Fixed amount type
                        Amount = 7m, // 0.07 * 100 items = 7 EUR
                        VATRate = 0m,
                        Position = 2
                    }
                ],
                cbPayItems = 
                [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase) 0x4752000000000001,
                        Amount = 1007m
                    }
                ]
            };

            var receiptResponse = new ReceiptResponse
            {
                ftReceiptIdentification = "ft123456789",
                ftCashBoxIdentification = "CB001",
                ftState = (State)0x4752000000000000,
                ftSignatures = []
            };

            // Act
            var (invoiceDoc, error) = factory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            // Assert
            error.Should().BeNull();
            invoiceDoc.Should().NotBeNull();
            
            var invoice = invoiceDoc.invoice[0];
            var fee = invoice.taxesTotals[0];

            fee.taxType.Should().Be(2); // Fee tax type
            fee.taxCategory.Should().Be(8); // Environmental fee plastic bags
            fee.taxAmount.Should().Be(7m);
            fee.underlyingValueSpecified.Should().BeFalse(); // No underlying value for fixed amount

            invoice.invoiceSummary.totalFeesAmount.Should().Be(7m);
            invoice.invoiceSummary.totalGrossValue.Should().Be(1007m);
            invoice.invoiceSummary.totalNetValue.Should().Be(806.45m);

            // Should only have one invoice detail (the regular item, not the fee item)
            invoice.invoiceDetails.Length.Should().Be(1);
            invoice.invoiceDetails[0].netValue.Should().Be(806.45m);
        }

        [Fact]
        public void MapToInvoicesDoc_ShouldHandleMixedWithholdingTaxAndFees()
        {
            // Arrange
            var factory = new AADEFactory(_masterDataConfiguration);
            
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftPosSystemId = Guid.NewGuid(),
                cbTerminalID = "T001",
                cbReceiptReference = "R001",
                cbReceiptMoment = DateTime.UtcNow,
                ftReceiptCase = (ReceiptCase) 0x4752000000000001,
                cbChargeItems =
                [
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x4752000000000013,
                        Description = "Service",
                        Amount = 1000m,
                        VATRate = 24m,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Περιπτ. β’- Τόκοι - 15%", // Withholding tax
                        Amount = -121m,
                        VATRate = 0m,
                        Position = 2
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Τέλος στη συνδρομητική τηλεόραση 10%", // Fee
                        Amount = 80m,
                        VATRate = 0m,
                        Position = 3
                    }
                ],
                cbPayItems =
                [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase)0x4752000000000001,
                        Amount = 959m // 1000 - 121 + 80 = 959
                    }
                ]
            };

            var receiptResponse = new ReceiptResponse
            {
                ftReceiptIdentification = "ft123456789",
                ftCashBoxIdentification = "CB001",
                ftState = (State)0x4752000000000000,
                ftSignatures = []
            };

            // Act
            var (invoiceDoc, error) = factory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            // Assert
            error.Should().BeNull();
            invoiceDoc.Should().NotBeNull();
            
            var invoice = invoiceDoc.invoice[0];
            
            // Should have both withholding tax and fee
            invoice.taxesTotals.Length.Should().Be(2);
            
            var withholdingTax = invoice.taxesTotals.FirstOrDefault(t => t.taxType == 1); // Withholding tax
            withholdingTax.Should().NotBeNull();
            withholdingTax.taxCategory.Should().Be(1); // Interest
            withholdingTax.taxAmount.Should().Be(121m);
            
            var fee = invoice.taxesTotals.FirstOrDefault(t => t.taxType == 2); // Fee
            fee.Should().NotBeNull();
            fee.taxCategory.Should().Be(6); // Subscription TV fee
            fee.taxAmount.Should().Be(80m);

            // Invoice summary should include both
            invoice.invoiceSummary.totalWithheldAmount.Should().Be(121m);
            invoice.invoiceSummary.totalFeesAmount.Should().Be(80m);
            invoice.invoiceSummary.totalGrossValue.Should().Be(959m);
            invoice.invoiceSummary.totalNetValue.Should().Be(806.45m);

            // Should only have one invoice detail (the regular item)
            invoice.invoiceDetails.Length.Should().Be(1);
            invoice.invoiceDetails[0].netValue.Should().Be(806.45m);
        }

        [Fact]
        public void MapToInvoicesDoc_ShouldHandleMultipleFees()
        {
            // Arrange
            var factory = new AADEFactory(_masterDataConfiguration);
            
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftPosSystemId = Guid.NewGuid(),
                cbTerminalID = "T001",
                cbReceiptReference = "R001",
                cbReceiptMoment = DateTime.UtcNow,
                ftReceiptCase = (ReceiptCase) 0x4752000000000001,
                cbChargeItems =
                [
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x4752000000000013,
                        Description = "Service",
                        Amount = 1000m,
                        VATRate = 24m,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Τέλος στη συνδρομητική τηλεόραση 10%",
                        Amount = 80m,
                        VATRate = 0m,
                        Position = 2
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Λοιπά τέλη", // Fixed amount fee
                        Amount = 15m,
                        VATRate = 0m,
                        Position = 3
                    }
                ],
                cbPayItems =
                [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase)0x4752000000000001,
                        Amount = 1095m // 1000 + 80 + 15 = 1095
                    }
                ]
            };

            var receiptResponse = new ReceiptResponse
            {
                ftReceiptIdentification = "ft123456789",
                ftCashBoxIdentification = "CB001",
                ftState = (State)0x4752000000000000,
                ftSignatures = []
            };

            // Act
            var (invoiceDoc, error) = factory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            // Assert
            error.Should().BeNull();
            invoiceDoc.Should().NotBeNull();
            
            var invoice = invoiceDoc.invoice[0];
            
            // Should have multiple fees
            invoice.taxesTotals.Length.Should().Be(2);
            
            var subscriptionFee = invoice.taxesTotals.FirstOrDefault(t => t.taxCategory == 6);
            subscriptionFee.Should().NotBeNull();
            subscriptionFee.taxAmount.Should().Be(80m);

            var otherFee = invoice.taxesTotals.FirstOrDefault(t => t.taxCategory == 10);
            otherFee.Should().NotBeNull();
            otherFee.taxAmount.Should().Be(15m);

            // Total fees should be sum of both
            invoice.invoiceSummary.totalFeesAmount.Should().Be(95m); // 80 + 15
            invoice.invoiceSummary.totalGrossValue.Should().Be(1095m);
            invoice.invoiceSummary.totalNetValue.Should().Be(806.45m);

            // Should only have one invoice detail (the regular item)
            invoice.invoiceDetails.Length.Should().Be(1);
            invoice.invoiceDetails[0].netValue.Should().Be(806.45m);
        }

        #endregion
    }
}