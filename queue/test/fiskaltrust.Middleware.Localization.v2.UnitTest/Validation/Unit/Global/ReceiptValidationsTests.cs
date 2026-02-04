using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation.Unit.Global;

public class ReceiptValidationsTests
{
    private readonly ReceiptValidations _validator = new();

    [Fact]
    public void ChargeItemsSum_MatchesReceiptAmount_ShouldPass()
    {
        var request = new ReceiptRequest
        {
            cbReceiptAmount = 25.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product 1", VATRate = 23.0m, Amount = 10.00m },
                new ChargeItem { Description = "Product 2", VATRate = 23.0m, Amount = 15.00m }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.cbReceiptAmount);
    }

    [Fact]
    public void ChargeItemsSum_DoesNotMatchReceiptAmount_ShouldFail()
    {
        var request = new ReceiptRequest
        {
            cbReceiptAmount = 100.00m,  // Wrong total
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product 1", VATRate = 23.0m, Amount = 10.00m },
                new ChargeItem { Description = "Product 2", VATRate = 23.0m, Amount = 15.00m }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.cbReceiptAmount)
              .WithErrorCode("ChargeItemsSumMismatch");
    }

    [Fact]
    public void ChargeItemsSum_WithNullReceiptAmount_ShouldPass()
    {
        var request = new ReceiptRequest
        {
            cbReceiptAmount = null,  // No total specified
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", VATRate = 23.0m, Amount = 10.00m }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.cbReceiptAmount);
    }

    [Fact]
    public void ChargeItemsSum_WithNullChargeItems_ShouldPass()
    {
        var request = new ReceiptRequest
        {
            cbReceiptAmount = 10.00m,
            cbChargeItems = null
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.cbReceiptAmount);
    }

    [Fact]
    public void ChargeItemsSum_WithEmptyChargeItems_ShouldPass()
    {
        var request = new ReceiptRequest
        {
            cbReceiptAmount = 10.00m,
            cbChargeItems = new List<ChargeItem>()
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.cbReceiptAmount);
    }

    [Fact]
    public void ChargeItemsSum_WithNegativeAmounts_MatchesReceiptAmount_ShouldPass()
    {
        // Scenario: Product + Discount = Total
        var request = new ReceiptRequest
        {
            cbReceiptAmount = 8.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", VATRate = 23.0m, Amount = 10.00m },
                new ChargeItem { Description = "Discount", VATRate = 23.0m, Amount = -2.00m }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.cbReceiptAmount);
    }
}
