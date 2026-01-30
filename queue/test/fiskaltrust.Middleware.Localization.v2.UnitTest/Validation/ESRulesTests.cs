using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation;

/// <summary>
/// Tests for ESRules - validation rules specific to Spain.
/// </summary>
public class ESRulesTests
{
    [Fact]
    public void ChargeItemsVATAmountRequired_MissingVATAmount_ReturnsError()
    {
        // Arrange
        var request = new ReceiptRequest
        {
            cbChargeItems =
            [
                new ChargeItem
                {
                    Description = "Test Product",
                    VATRate = 21.0m,
                    Amount = 10.00m,
                    VATAmount = null
                }
            ]
        };

        // Act
        var results = ESRules.ChargeItemsVATAmountRequired(request).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal("EEEE_ChargeItemVATAmountMissing", results[0].Errors[0].Code);
    }

    [Fact]
    public void ChargeItemsVATAmountRequired_WithVATAmount_ReturnsNoError()
    {
        // Arrange
        var request = new ReceiptRequest
        {
            cbChargeItems =
            [
                new ChargeItem
                {
                    Description = "Test Product",
                    VATRate = 21.0m,
                    Amount = 10.00m,
                    VATAmount = 1.74m
                }
            ]
        };

        // Act
        var results = ESRules.ChargeItemsVATAmountRequired(request).ToList();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void ValidationRunner_ESMarket_MissingVATAmount_Fails()
    {
        // Arrange - ES requires VATAmount
        var runner = new ValidationRunner();
        var request = new ReceiptRequest
        {
            cbChargeItems =
            [
                new ChargeItem
                {
                    Description = "Test Product",
                    VATRate = 21.0m,
                    Amount = 10.00m,
                    VATAmount = null
                }
            ]
        };

        // Act - ES runs "Always" + "ES" rules
        var results = runner.ValidateAndCollect(request, RuleSetNames.Always, RuleSetNames.ES);

        // Assert - invalid for ES
        Assert.False(results.IsValid);
    }

    [Fact]
    public void ValidationRunner_ESMarket_ValidRequest_Passes()
    {
        // Arrange
        var runner = new ValidationRunner();
        var request = new ReceiptRequest
        {
            cbChargeItems =
            [
                new ChargeItem
                {
                    Description = "Test Product",
                    VATRate = 21.0m,
                    Amount = 10.00m,
                    VATAmount = 1.74m
                }
            ]
        };

        // Act - ES runs "Always" + "ES" rules
        var results = runner.ValidateAndCollect(request, RuleSetNames.Always, RuleSetNames.ES);

        // Assert - valid for ES
        Assert.True(results.IsValid);
    }

    [Fact]
    public void ValidationRunner_ESMarket_MissingDescription_Fails()
    {
        // Arrange
        var runner = new ValidationRunner();
        var request = new ReceiptRequest
        {
            cbChargeItems =
            [
                new ChargeItem
                {
                    Description = "",
                    VATRate = 21.0m,
                    Amount = 10.00m,
                    VATAmount = 1.74m
                }
            ]
        };

        // Act - ES runs "Always" + "ES" rules
        var results = runner.ValidateAndCollect(request, RuleSetNames.Always, RuleSetNames.ES);

        // Assert - fails on global rule
        Assert.False(results.IsValid);
    }
}
