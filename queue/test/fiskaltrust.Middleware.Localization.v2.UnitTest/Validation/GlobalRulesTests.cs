using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation;

/// <summary>
/// Tests for GlobalRules - validation rules that apply to ALL markets.
/// </summary>
public class GlobalRulesTests
{
    [Fact]
    public void ChargeItemsMandatoryFields_ValidChargeItems_ReturnsNoErrors()
    {
        // Arrange
        var request = new ReceiptRequest
        {
            cbChargeItems =
            [
                new ChargeItem
                {
                    Description = "Test Product",
                    VATRate = 23.0m,
                    Amount = 10.00m
                }
            ]
        };

        // Act
        var results = GlobalRules.ChargeItemsMandatoryFields(request).ToList();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void ChargeItemsMandatoryFields_MissingDescription_ReturnsError()
    {
        // Arrange
        var request = new ReceiptRequest
        {
            cbChargeItems =
            [
                new ChargeItem
                {
                    Description = "",
                    VATRate = 23.0m,
                    Amount = 10.00m
                }
            ]
        };

        // Act
        var results = GlobalRules.ChargeItemsMandatoryFields(request).ToList();

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsValid);
        Assert.Equal("EEEE_ChargeItemDescriptionMissing", results[0].Errors[0].Code);
    }

    [Fact]
    public void ChargeItemsMandatoryFields_NegativeVATRate_ReturnsError()
    {
        // Arrange
        var request = new ReceiptRequest
        {
            cbChargeItems =
            [
                new ChargeItem
                {
                    Description = "Test",
                    VATRate = -1m,
                    Amount = 10.00m
                }
            ]
        };

        // Act
        var results = GlobalRules.ChargeItemsMandatoryFields(request).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal("EEEE_ChargeItemVATRateMissing", results[0].Errors[0].Code);
    }

    [Fact]
    public void ChargeItemsMandatoryFields_ZeroAmount_ReturnsError()
    {
        // Arrange
        var request = new ReceiptRequest
        {
            cbChargeItems =
            [
                new ChargeItem
                {
                    Description = "Test",
                    VATRate = 23.0m,
                    Amount = 0m
                }
            ]
        };

        // Act
        var results = GlobalRules.ChargeItemsMandatoryFields(request).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal("EEEE_ChargeItemAmountMissing", results[0].Errors[0].Code);
    }

    [Fact]
    public void ChargeItemsMandatoryFields_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var request = new ReceiptRequest
        {
            cbChargeItems =
            [
                new ChargeItem { Description = "", VATRate = 23.0m, Amount = 10.00m },
                new ChargeItem { Description = "Test", VATRate = -1m, Amount = 10.00m },
                new ChargeItem { Description = "Test", VATRate = 23.0m, Amount = 0m }
            ]
        };

        // Act
        var results = GlobalRules.ChargeItemsMandatoryFields(request).ToList();

        // Assert
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void ValidationRunner_GlobalRulesOnly_ValidRequest_Passes()
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
                    VATRate = 23.0m,
                    Amount = 10.00m
                }
            ]
        };

        // Act
        var results = runner.ValidateAndCollect(request, RuleSetNames.Always);

        // Assert
        Assert.True(results.IsValid);
    }

    [Fact]
    public void ValidationRunner_GlobalRulesOnly_InvalidRequest_Fails()
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
                    VATRate = 23.0m,
                    Amount = 10.00m
                }
            ]
        };

        // Act
        var results = runner.ValidateAndCollect(request, RuleSetNames.Always);

        // Assert
        Assert.False(results.IsValid);
    }
}
