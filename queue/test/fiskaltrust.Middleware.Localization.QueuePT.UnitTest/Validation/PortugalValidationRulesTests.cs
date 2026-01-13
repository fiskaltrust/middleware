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
            error.Context["VATRate"].Should().Be(0m);
            error.Context["NatureOfVat"].Should().Be("UsualVatApplies");
            error.Context["NatureOfVatValue"].Should().Be("0x0000");
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

        #region Validate_ChargeItems_DiscountExceedsArticleAmount Tests

        [Fact]
        public void Validate_ChargeItems_DiscountExceedsArticleAmount_WithValidDiscount_ShouldPass()
        {
            // Arrange - discount of 10 on item of 100
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Description = "Main item",
                        Quantity = 1,
                        Amount = 123m, // 100 net + 23% VAT
                        VATRate = 23m,
                        ftChargeItemCase = ChargeItemCase.NormalVatRate,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        Description = "Discount",
                        Quantity = 1,
                        Amount = -12.3m, // -10 net + 23% VAT
                        VATRate = 23m,
                        ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseFlags.ExtraOrDiscount),
                        Position = 1
                    }
                }
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_DiscountExceedsArticleAmount(request).ToList();

            // Assert
            results.Should().BeEmpty("discount does not exceed the article amount");
        }

        [Fact]
        public void Validate_ChargeItems_DiscountExceedsArticleAmount_WithExactDiscount_ShouldPass()
        {
            // Arrange - discount equals item amount (100% discount)
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Description = "Main item",
                        Quantity = 1,
                        Amount = 123m, // 100 net + 23% VAT
                        VATRate = 23m,
                        ftChargeItemCase = ChargeItemCase.NormalVatRate,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        Description = "Full discount",
                        Quantity = 1,
                        Amount = -123m, // -100 net + 23% VAT
                        VATRate = 23m,
                        ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseFlags.ExtraOrDiscount),
                        Position = 1
                    }
                }
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_DiscountExceedsArticleAmount(request).ToList();

            // Assert
            results.Should().BeEmpty("100% discount is valid");
        }

        [Fact]
        public void Validate_ChargeItems_DiscountExceedsArticleAmount_WithExtra_ShouldPass()
        {
            // Arrange - extra (positive modifier) should not be validated
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Description = "Main item",
                        Quantity = 1,
                        Amount = 123m,
                        VATRate = 23m,
                        ftChargeItemCase = ChargeItemCase.NormalVatRate,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        Description = "Extra charge",
                        Quantity = 1,
                        Amount = 246m, // positive amount (extra, not discount)
                        VATRate = 23m,
                        ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseFlags.ExtraOrDiscount),
                        Position = 1
                    }
                }
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_DiscountExceedsArticleAmount(request).ToList();

            // Assert
            results.Should().BeEmpty("extras (positive modifiers) are not validated");
        }

        [Fact]
        public void Validate_ChargeItems_DiscountExceedsArticleAmount_WithMultipleGroups_ShouldValidateEachSeparately()
        {
            // Arrange - two article groups, one with valid discount, one with excessive discount
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>
                {
                    // First group - valid discount
                    new ChargeItem
                    {
                        Description = "Article 1",
                        Quantity = 1,
                        Amount = 123m, // 100 net
                        VATRate = 23m,
                        ftChargeItemCase = ChargeItemCase.NormalVatRate,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        Description = "Valid discount",
                        Quantity = 1,
                        Amount = -12.3m, // -10 net
                        VATRate = 23m,
                        ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseFlags.ExtraOrDiscount),
                        Position = 1
                    },
                    // Second group - excessive discount
                    new ChargeItem
                    {
                        Description = "Article 2",
                        Quantity = 1,
                        Amount = 61.5m, // 50 net
                        VATRate = 23m,
                        ftChargeItemCase = ChargeItemCase.NormalVatRate,
                        Position = 2
                    },
                    new ChargeItem
                    {
                        Description = "Excessive discount",
                        Quantity = 1,
                        Amount = -123m, // -100 net
                        VATRate = 23m,
                        ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseFlags.ExtraOrDiscount),
                        Position = 2
                    }
                }
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_DiscountExceedsArticleAmount(request).ToList();

            // Assert
            results.Should().HaveCount(1, "only the second group has excessive discount");
            results[0].Errors[0].Code.Should().Be("EEEE_DiscountExceedsArticleAmount");
            results[0].Errors[0].ItemIndex.Should().Be(2, "should reference Article 2");
        }

        [Fact]
        public void Validate_ChargeItems_DiscountExceedsArticleAmount_WithoutModifiers_ShouldPass()
        {
            // Arrange - article without any discounts or extras
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Description = "Main item",
                        Quantity = 1,
                        Amount = 123m,
                        VATRate = 23m,
                        ftChargeItemCase = ChargeItemCase.NormalVatRate,
                        Position = 1
                    }
                }
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_DiscountExceedsArticleAmount(request).ToList();

            // Assert
            results.Should().BeEmpty("no modifiers to validate");
        }

        [Fact]
        public void Validate_ChargeItems_DiscountExceedsArticleAmount_WithNullChargeItems_ShouldPass()
        {
            // Arrange
            var request = new ReceiptRequest
            {
                cbChargeItems = null
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_DiscountExceedsArticleAmount(request).ToList();

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void Validate_ChargeItems_DiscountExceedsArticleAmount_WithEmptyChargeItems_ShouldPass()
        {
            // Arrange
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>()
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_DiscountExceedsArticleAmount(request).ToList();

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void Validate_ChargeItems_DiscountExceedsArticleAmount_WithDifferentVATRates_ShouldValidateCorrectly()
        {
            // Arrange - discount with 6% VAT exceeding article with 6% VAT
            var request = new ReceiptRequest
            {
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Description = "Reduced VAT item",
                        Quantity = 1,
                        Amount = 106m, // 100 net + 6% VAT
                        VATRate = 6m,
                        ftChargeItemCase = ChargeItemCase.DiscountedVatRate1,
                        Position = 1
                    },
                    new ChargeItem
                    {
                        Description = "Excessive discount",
                        Quantity = 1,
                        Amount = -159m, // -150 net + 6% VAT
                        VATRate = 6m,
                        ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.DiscountedVatRate1 | (long)ChargeItemCaseFlags.ExtraOrDiscount),
                        Position = 1
                    }
                }
            };

            // Act
            var results = ChargeItemValidations.Validate_ChargeItems_DiscountExceedsArticleAmount(request).ToList();

            // Assert
            results.Should().HaveCount(1);
            results[0].Errors[0].Code.Should().Be("EEEE_DiscountExceedsArticleAmount");
        }

        #endregion
    }
}
