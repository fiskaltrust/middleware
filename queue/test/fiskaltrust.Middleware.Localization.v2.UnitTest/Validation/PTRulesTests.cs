using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Validation;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation;

/// <summary>
/// Tests for PT market - Portugal uses only global rules (no PT-specific rules yet).
/// </summary>
public class PTRulesTests
{
    [Fact]
    public void ValidationRunner_PTMarket_MissingVATAmount_Passes()
    {
        // Arrange - PT does NOT require VATAmount
        var runner = new ValidationRunner();
        var request = new ReceiptRequest
        {
            cbChargeItems =
            [
                new ChargeItem
                {
                    Description = "Test Product",
                    VATRate = 23.0m,
                    Amount = 10.00m,
                    VATAmount = null // PT doesn't require this
                }
            ]
        };

        // Act - PT only runs "Always" rules
        var results = runner.ValidateAndCollect(request, RuleSetNames.Always);

        // Assert - valid for PT
        Assert.True(results.IsValid);
    }

    [Fact]
    public void ValidationRunner_PTMarket_ValidRequest_Passes()
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

        // Act - PT runs "Always" rules
        var results = runner.ValidateAndCollect(request, RuleSetNames.Always);

        // Assert
        Assert.True(results.IsValid);
    }

    [Fact]
    public void ValidationRunner_PTMarket_MissingDescription_Fails()
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

        // Act - PT runs "Always" rules
        var results = runner.ValidateAndCollect(request, RuleSetNames.Always);

        // Assert - fails on global rule
        Assert.False(results.IsValid);
    }
}
