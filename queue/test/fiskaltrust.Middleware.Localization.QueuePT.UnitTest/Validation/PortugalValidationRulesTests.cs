using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Validation;
using fiskaltrust.Middleware.Localization.QueuePT.Validation.Rules;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Validation;

public class PortugalValidationRulesTests
{
    public class ChargeItemValidationsTests
    {
        #region Validate_ChargeItems_VATRate_ZeroVatRateNature Tests

        [Fact]
        public void Validate_ChargeItems_VATRate_ZeroVatRateNature_WithValidNatureGroup0x30_ShouldPass()
        {
            // Arrange
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Description = "Tax exempt item",
                        Quantity = 1,
                        Amount = 100m,
                        VATRate = 0m,
                        ftChargeItemCase = ((ChargeItemCase)ChargeItemCase.NotTaxable).WithNatureOfVat(ChargeItemCaseNatureOfVatPT.Group0x30)
                    }
                }
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).ToList();

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void Validate_ChargeItems_VATRate_ZeroVatRateNature_WithValidNatureGroup0x40_ShouldPass()
        {
            // Arrange
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Description = "Tax exempt item",
                        Quantity = 1,
                        Amount = 100m,
                        VATRate = 0m,
                        ftChargeItemCase = ((ChargeItemCase)ChargeItemCase.NotTaxable).WithNatureOfVat(ChargeItemCaseNatureOfVatPT.Group0x40)
                    }
                }
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).ToList();

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void Validate_ChargeItems_VATRate_ZeroVatRateNature_WithoutNature_ShouldFail()
        {
            // Arrange
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Description = "Tax exempt item",
                        Quantity = 1,
                        Amount = 100m,
                        VATRate = 0m,
                        ftChargeItemCase = ChargeItemCase.NotTaxable // No nature specified (defaults to UsualVatApplies)
                    }
                }
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).ToList();

            // Assert
            results.Should().HaveCount(1);
            results[0].IsValid.Should().BeFalse();
            results[0].Errors.Should().HaveCount(1);
            results[0].Errors[0].Code.Should().Be("EEEE_ZeroVatRateMissingNature");
            results[0].Errors[0].Message.Should().Contain("When VAT rate is 0%");
            results[0].Errors[0].Message.Should().Contain("tax exemption reason");
            results[0].Errors[0].Field.Should().Be("cbChargeItems.ftChargeItemCase");
            results[0].Errors[0].ItemIndex.Should().Be(0);
        }

        [Fact]
        public void Validate_ChargeItems_VATRate_ZeroVatRateNature_WithMultipleItems_ShouldFailOnlyInvalid()
        {
            // Arrange
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Description = "Regular item",
                        Quantity = 1,
                        Amount = 100m,
                        VATRate = 23m,
                        ftChargeItemCase = ChargeItemCase.NormalVatRate
                    },
                    new ChargeItem
                    {
                        Description = "Tax exempt item without nature",
                        Quantity = 1,
                        Amount = 50m,
                        VATRate = 0m,
                        ftChargeItemCase = ChargeItemCase.NotTaxable // No nature specified
                    },
                    new ChargeItem
                    {
                        Description = "Tax exempt item with nature",
                        Quantity = 1,
                        Amount = 75m,
                        VATRate = 0m,
                        ftChargeItemCase = ((ChargeItemCase)ChargeItemCase.NotTaxable).WithNatureOfVat(ChargeItemCaseNatureOfVatPT.Group0x30)
                    }
                }
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).ToList();

            // Assert
            results.Should().HaveCount(1);
            results[0].Errors[0].ItemIndex.Should().Be(1);
        }

        [Fact]
        public void Validate_ChargeItems_VATRate_ZeroVatRateNature_WithNullChargeItems_ShouldPass()
        {
            // Arrange
            var request = new ReceiptRequest
            {
                cbChargeItems = null
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).ToList();

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void Validate_ChargeItems_VATRate_ZeroVatRateNature_WithEmptyChargeItems_ShouldPass()
        {
            // Arrange
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>()
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).ToList();

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void Validate_ChargeItems_VATRate_ZeroVatRateNature_WithNonZeroVATRate_ShouldPass()
        {
            // Arrange
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Description = "Regular item",
                        Quantity = 1,
                        Amount = 100m,
                        VATRate = 23m,
                        ftChargeItemCase = ChargeItemCase.NormalVatRate
                    }
                }
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).ToList();

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void Validate_ChargeItems_VATRate_ZeroVatRateNature_ValidationError_ShouldContainContext()
        {
            // Arrange
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Description = "Tax exempt item",
                        Quantity = 1,
                        Amount = 100m,
                        VATRate = 0m,
                        ftChargeItemCase = ChargeItemCase.NotTaxable
                    }
                }
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).ToList();

            // Assert
            results.Should().HaveCount(1);
            var error = results[0].Errors[0];
            error.Context.Should().ContainKey("VATRate");
            error.Context.Should().ContainKey("NatureOfVat");
            error.Context.Should().ContainKey("NatureOfVatValue");
            error.Context.Should().ContainKey("ValidExemptions");
            error.Context["VATRate"].Should().Be(0m);
            error.Context["NatureOfVat"].Should().Be("UsualVatApplies");
            error.Context["NatureOfVatValue"].Should().Be("0x0000");
            // ValidExemptions should contain references to M06 and M16
            var validExemptions = error.Context["ValidExemptions"].ToString();
            validExemptions.Should().Contain("M06");
            validExemptions.Should().Contain("M16");
            validExemptions.Should().Contain("artigo 15");
            validExemptions.Should().Contain("artigo 14");
        }

        [Fact]
        public void Validate_ChargeItems_VATRate_ZeroVatRateNature_WithVerySmallVATRate_ShouldTreatAsZero()
        {
            // Arrange - VAT rate is technically not exactly 0 but close enough (0.0001)
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Description = "Tax exempt item",
                        Quantity = 1,
                        Amount = 100m,
                        VATRate = 0.0001m,
                        ftChargeItemCase = ChargeItemCase.NotTaxable
                    }
                }
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).ToList();

            // Assert
            results.Should().HaveCount(1);
            results[0].Errors[0].Code.Should().Be("EEEE_ZeroVatRateMissingNature");
        }

        [Fact]
        public void Validate_ChargeItems_VATRate_ZeroVatRateNature_WithMultipleInvalidItems_ShouldReturnMultipleErrors()
        {
            // Arrange
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Description = "Tax exempt item 1",
                        Quantity = 1,
                        Amount = 50m,
                        VATRate = 0m,
                        ftChargeItemCase = ChargeItemCase.NotTaxable
                    },
                    new ChargeItem
                    {
                        Description = "Tax exempt item 2",
                        Quantity = 1,
                        Amount = 75m,
                        VATRate = 0m,
                        ftChargeItemCase = ChargeItemCase.NotTaxable
                    },
                    new ChargeItem
                    {
                        Description = "Tax exempt item 3",
                        Quantity = 1,
                        Amount = 100m,
                        VATRate = 0m,
                        ftChargeItemCase = ChargeItemCase.NotTaxable
                    }
                }
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).ToList();

            // Assert
            results.Should().HaveCount(3);
            results[0].Errors[0].ItemIndex.Should().Be(0);
            results[1].Errors[0].ItemIndex.Should().Be(1);
            results[2].Errors[0].ItemIndex.Should().Be(2);
        }

        [Fact]
        public void Validate_ChargeItems_VATRate_ZeroVatRateNature_ErrorMessage_ShouldReferenceExemptionCodes()
        {
            // Arrange
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Description = "Tax exempt item",
                        Quantity = 1,
                        Amount = 100m,
                        VATRate = 0m,
                        ftChargeItemCase = ChargeItemCase.NotTaxable
                    }
                }
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).ToList();

            // Assert
            results.Should().HaveCount(1);
            var errorMessage = results[0].Errors[0].Message;
            errorMessage.Should().Contain("M06");
            errorMessage.Should().Contain("M16");
            errorMessage.Should().Contain("0x3000");
            errorMessage.Should().Contain("0x4000");
            errorMessage.Should().Contain("artigo 15");
            errorMessage.Should().Contain("artigo 14");
        }

        [Fact]
        public void Validate_ChargeItems_VATRate_ZeroVatRateNature_WithBothValidNatures_ShouldPass()
        {
            // Arrange - testing both valid nature codes
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Description = "Exempt M06",
                        Quantity = 1,
                        Amount = 50m,
                        VATRate = 0m,
                        ftChargeItemCase = ((ChargeItemCase)ChargeItemCase.NotTaxable).WithNatureOfVat(ChargeItemCaseNatureOfVatPT.Group0x30)
                    },
                    new ChargeItem
                    {
                        Description = "Exempt M16",
                        Quantity = 1,
                        Amount = 75m,
                        VATRate = 0m,
                        ftChargeItemCase = ((ChargeItemCase)ChargeItemCase.NotTaxable).WithNatureOfVat(ChargeItemCaseNatureOfVatPT.Group0x40)
                    }
                }
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).ToList();

            // Assert
            results.Should().BeEmpty("both Group0x30 (M06) and Group0x40 (M16) are valid exemption natures");
        }

        #endregion
    }
}
