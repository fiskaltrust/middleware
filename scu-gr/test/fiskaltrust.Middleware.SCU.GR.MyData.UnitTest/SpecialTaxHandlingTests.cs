using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest.SCU.MyData
{
    public class SpecialTaxHandlingTests
    {
        private readonly MasterDataConfiguration _masterDataConfiguration;

        public SpecialTaxHandlingTests()
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
                        Description = "Περιπτ. β'- Τόκοι - 15%",
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
            error.Exception.Message.Should().Contain("No withholding tax, fee, stamp duty, or other tax mapping");
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
                        Description = "Περιπτ. β'- Τόκοι - 15%",
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
                        Description = "Περιβαλλοντικό Τέλος & πλαστικής σακούλας ν. 2339/2001 αρ. 6α 0,07 ευρώ ανά τεμάχιο", // Fixed amount type
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
                        Description = "Περιπτ. β'- Τόκοι - 15%",
                        Amount = -121m, // 15% of net amount (806.45 * 0.15 ≈ 121)
                        VATRate = 0m,
                        Position = 2
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Τέλος στη συνδρομητική τηλεόραση 10%",
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

            // Should only have one invoice detail (the regular item, not the withholding tax item)
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

        #region Stamp Duty Tests

        [Fact]
        public void MapToInvoicesDoc_ShouldProcessPercentageStampDuty()
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
                        Description = "Συντελεστής 1,2 %",
                        Amount = 0.97m, // 1.2% of net amount (80.65 * 0.012 ≈ 0.97)
                        VATRate = 0m,
                        Position = 2
                    }
                ],
                cbPayItems = [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase)0x4752000000000001,
                        Amount = 100.97m
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
            
            // Should have tax totals for stamp duty
            invoice.taxesTotals.Should().NotBeNullOrEmpty();
            invoice.taxesTotals.Length.Should().Be(1);
            
            var stampDuty = invoice.taxesTotals[0];
            stampDuty.taxType.Should().Be(4); // Stamp duty tax type
            stampDuty.taxCategory.Should().Be(1); // 1.2% stamp duty code
            stampDuty.taxAmount.Should().Be(0.97m);

            // Invoice summary should include stamp duty
            invoice.invoiceSummary.totalStampDutyAmount.Should().Be(0.97m);
            invoice.invoiceSummary.totalGrossValue.Should().Be(100.97m);
            invoice.invoiceSummary.totalNetValue.Should().Be(80.65m);

            // Should only have one invoice detail (the regular item, not the stamp duty item)
            invoice.invoiceDetails.Length.Should().Be(1);
            invoice.invoiceDetails[0].netValue.Should().BeApproximately(80.65m, 0.01m);
        }

        [Fact]
        public void MapToInvoicesDoc_ShouldHandleFixedAmountStampDuty()
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
                        Description = "Λοιπές περιπτώσεις Χαρτοσήμου", // Fixed amount type
                        Amount = 25m, // Fixed stamp duty amount
                        VATRate = 0m,
                        Position = 2
                    }
                ],
                cbPayItems = 
                [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase) 0x4752000000000001,
                        Amount = 1025m
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
            var stampDuty = invoice.taxesTotals[0];

            stampDuty.taxType.Should().Be(4); // Stamp duty tax type
            stampDuty.taxCategory.Should().Be(4); // Other stamp duty cases
            stampDuty.taxAmount.Should().Be(25m);
            stampDuty.underlyingValueSpecified.Should().BeFalse(); // No underlying value for fixed amount

            invoice.invoiceSummary.totalStampDutyAmount.Should().Be(25m);
            invoice.invoiceSummary.totalGrossValue.Should().Be(1025m);
            invoice.invoiceSummary.totalNetValue.Should().Be(806.45m);

            // Should only have one invoice detail (the regular item, not the stamp duty item)
            invoice.invoiceDetails.Length.Should().Be(1);
            invoice.invoiceDetails[0].netValue.Should().Be(806.45m);
        }

        [Fact]
        public void MapToInvoicesDoc_ShouldHandleMultipleStampDuties()
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
                        Description = "Συντελεστής 1,2 %",
                        Amount = 9.68m, // 1.2% of net amount (806.45 * 0.012 ≈ 9.68)
                        VATRate = 0m,
                        Position = 2
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Λοιπές περιπτώσεις Χαρτοσήμου",
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
                        Amount = 1024.68m // 1000 + 9.68 + 15 = 1024.68
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
            
            // Should have multiple stamp duties
            invoice.taxesTotals.Length.Should().Be(2);
            
            var percentageStampDuty = invoice.taxesTotals.FirstOrDefault(t => t.taxCategory == 1);
            percentageStampDuty.Should().NotBeNull();
            percentageStampDuty.taxAmount.Should().Be(9.68m);
            
            var fixedStampDuty = invoice.taxesTotals.FirstOrDefault(t => t.taxCategory == 4);
            fixedStampDuty.Should().NotBeNull();
            fixedStampDuty.taxAmount.Should().Be(15m);
            
            // Total stamp duty should be sum of both
            invoice.invoiceSummary.totalStampDutyAmount.Should().Be(24.68m); // 9.68 + 15
            invoice.invoiceSummary.totalGrossValue.Should().Be(1024.68m);
            invoice.invoiceSummary.totalNetValue.Should().Be(806.45m);

            // Should only have one invoice detail (the regular item, not the stamp duty items)
            invoice.invoiceDetails.Length.Should().Be(1);
            invoice.invoiceDetails[0].netValue.Should().Be(806.45m);
        }

        [Fact]
        public void MapToInvoicesDoc_ShouldHandleMixedWithholdingTaxFeesAndStampDuties()
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
                        Description = "Περιπτ. β'- Τόκοι - 15%",
                        Amount = -121m, // 15% of net amount (806.45 * 0.15 ≈ 121)
                        VATRate = 0m,
                        Position = 2
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Τέλος στη συνδρομητική τηλεόραση 10%",
                        Amount = 80m,
                        VATRate = 0m,
                        Position = 3
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Συντελεστής 1,2 %", // Stamp duty
                        Amount = 9.68m,
                        VATRate = 0m,
                        Position = 4
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "β) ασφάλιστρα κλάδου ζωής 4%", // Other tax
                        Amount = 32.26m, // 4% of net amount (806.45 * 0.04 ≈ 32.26)
                        VATRate = 0m,
                        Position = 5
                    }
                ],
                cbPayItems =
                [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase)0x4752000000000001,
                        Amount = 1000.94m // 1000 - 121 + 80 + 9.68 + 32.26 = 1000.94
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
            
            // Should have all four types: withholding tax, fee, stamp duty, and other tax
            invoice.taxesTotals.Length.Should().Be(4);
            
            var withholdingTax = invoice.taxesTotals.FirstOrDefault(t => t.taxType == 1); // Withholding tax
            withholdingTax.Should().NotBeNull();
            withholdingTax.taxCategory.Should().Be(1); // Interest
            withholdingTax.taxAmount.Should().Be(121m);
            
            var fee = invoice.taxesTotals.FirstOrDefault(t => t.taxType == 2); // Fee
            fee.Should().NotBeNull();
            fee.taxCategory.Should().Be(6); // Subscription TV fee
            fee.taxAmount.Should().Be(80m);

            var stampDuty = invoice.taxesTotals.FirstOrDefault(t => t.taxType == 4); // Stamp duty
            stampDuty.Should().NotBeNull();
            stampDuty.taxCategory.Should().Be(1); // 1.2% stamp duty
            stampDuty.taxAmount.Should().Be(9.68m);

            var otherTax = invoice.taxesTotals.FirstOrDefault(t => t.taxType == 3); // Other tax
            otherTax.Should().NotBeNull();
            otherTax.taxCategory.Should().Be(3); // Life insurance premium
            otherTax.taxAmount.Should().Be(32.26m);

            // Invoice summary should include all four types
            invoice.invoiceSummary.totalWithheldAmount.Should().Be(121m);
            invoice.invoiceSummary.totalFeesAmount.Should().Be(80m);
            invoice.invoiceSummary.totalStampDutyAmount.Should().Be(9.68m);
            invoice.invoiceSummary.totalOtherTaxesAmount.Should().Be(32.26m);
            invoice.invoiceSummary.totalGrossValue.Should().Be(1000.94m);
            invoice.invoiceSummary.totalNetValue.Should().Be(806.45m);

            // Should only have one invoice detail (the regular item)
            invoice.invoiceDetails.Length.Should().Be(1);
            invoice.invoiceDetails[0].netValue.Should().Be(806.45m);
        }

        [Fact]
        public void MapToInvoicesDoc_ShouldThrowException_WhenOtherTaxDescriptionNotMapped()
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
                        Description = "Unknown other tax",
                        Amount = 10m,
                        VATRate = 0m,
                        Position = 2
                    }
                ],
                cbPayItems = 
                [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase)0x4752000000000001,
                        Amount = 110m
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
            error.Exception.Message.Should().Contain("No withholding tax, fee, stamp duty, or other tax mapping");
            error.Exception.Message.Should().Contain("Unknown other tax");
        }

        [Fact]
        public void MapToInvoicesDoc_ShouldHandleZeroPercentageOtherTax()
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
                        Amount = 500m,
                        VATRate = 24m,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "δ) απαλλασσόμενα φόρου ασφαλίστρων 0%", // 0% insurance premium exemption
                        Amount = 0m,
                        VATRate = 0m,
                        Position = 2
                    }
                ],
                cbPayItems = [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase)0x4752000000000001,
                        Amount = 500m
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
            var otherTax = invoice.taxesTotals[0];

            otherTax.taxType.Should().Be(3); // Other tax type
            otherTax.taxCategory.Should().Be(5); // Insurance premium exemption code
            otherTax.taxAmount.Should().Be(0m);

            invoice.invoiceSummary.totalOtherTaxesAmount.Should().Be(0m);
            invoice.invoiceSummary.totalGrossValue.Should().Be(500m);
            invoice.invoiceSummary.totalNetValue.Should().Be(403.23m);

            // Should only have one invoice detail (the regular item)
            invoice.invoiceDetails.Length.Should().Be(1);
            invoice.invoiceDetails[0].netValue.Should().Be(403.23m);
        }

        [Fact]
        public void MapToInvoicesDoc_ShouldHandleLuxuryTaxOtherTax()
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
                        Description = "Luxury item",
                        Amount = 1000m,
                        VATRate = 24m,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "3.1 Φόρος πολυτελείας 10% επί της φορολογητέας αξίας για τα ενδοκοινοτικώς αποκτούμενα και εισαγόμενα από τρίτες χώρες", // Luxury tax 10%
                        Amount = 80.65m, // 10% of net amount (806.45 * 0.10 ≈ 80.65)
                        VATRate = 0m,
                        Position = 2
                    }
                ],
                cbPayItems = [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase)0x4752000000000001,
                        Amount = 1080.65m
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
            var otherTax = invoice.taxesTotals[0];

            otherTax.taxType.Should().Be(3); // Other tax type
            otherTax.taxCategory.Should().Be(12); // Luxury tax intra-community acquisition code
            otherTax.taxAmount.Should().Be(80.65m);

            invoice.invoiceSummary.totalOtherTaxesAmount.Should().Be(80.65m);
            invoice.invoiceSummary.totalGrossValue.Should().Be(1080.65m);
            invoice.invoiceSummary.totalNetValue.Should().Be(806.45m);

            // Should only have one invoice detail (the regular item)
            invoice.invoiceDetails.Length.Should().Be(1);
            invoice.invoiceDetails[0].netValue.Should().Be(806.45m);
        }

        [Fact]
        public void MapToInvoicesDoc_ShouldHandleCasinoTaxOtherTax()
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
                        Description = "Casino ticket",
                        Amount = 100m,
                        VATRate = 24m,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Δικαίωμα του Δημοσίου στα εισιτήρια των καζίνο (80% επί του εισιτηρίου)", // Casino tax 80%
                        Amount = 64.52m, // 80% of net amount (80.65 * 0.80 ≈ 64.52)
                        VATRate = 0m,
                        Position = 2
                    }
                ],
                cbPayItems = [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase)0x4752000000000001,
                        Amount = 164.52m
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
            var otherTax = invoice.taxesTotals[0];

            otherTax.taxType.Should().Be(3); // Other tax type
            otherTax.taxCategory.Should().Be(14); // Casino ticket tax code
            otherTax.taxAmount.Should().Be(64.52m);

            invoice.invoiceSummary.totalOtherTaxesAmount.Should().Be(64.52m);
            invoice.invoiceSummary.totalGrossValue.Should().Be(164.52m);
            invoice.invoiceSummary.totalNetValue.Should().Be(80.65m);

            // Should only have one invoice detail (the regular item)
            invoice.invoiceDetails.Length.Should().Be(1);
            invoice.invoiceDetails[0].netValue.Should().Be(80.65m);
        }

        [Fact]
        public void MapToInvoicesDoc_ShouldHandleShortTermRentalTaxOtherTax()
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
                        Description = "Accommodation",
                        Amount = 200m,
                        VATRate = 24m,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase)0x47520000000000F0, // TypeOfService = 0xF
                        Description = "Ακίνητα βραχυχρόνιας μίσθωσης μονοκατοικίες άνω των 80 τ.μ. 10,00€", // Short-term rental villa tax
                        Amount = 10m, // Fixed amount
                        VATRate = 0m,
                        Position = 2
                    }
                ],
                cbPayItems = [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase)0x4752000000000001,
                        Amount = 210m
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
            var otherTax = invoice.taxesTotals[0];

            otherTax.taxType.Should().Be(3); // Other tax type
            otherTax.taxCategory.Should().Be(26); // Short-term rental villa tax code
            otherTax.taxAmount.Should().Be(10m);
            otherTax.underlyingValueSpecified.Should().BeFalse(); // No underlying value for fixed amount

            invoice.invoiceSummary.totalOtherTaxesAmount.Should().Be(10m);
            invoice.invoiceSummary.totalGrossValue.Should().Be(210m);
            invoice.invoiceSummary.totalNetValue.Should().Be(161.29m); // 200 / 1.24 ≈ 161.29

            // Should only have one invoice detail (the regular item, not the other tax item)
            invoice.invoiceDetails.Length.Should().Be(1);
            invoice.invoiceDetails[0].netValue.Should().Be(161.29m);
        }

        #endregion
    }
}