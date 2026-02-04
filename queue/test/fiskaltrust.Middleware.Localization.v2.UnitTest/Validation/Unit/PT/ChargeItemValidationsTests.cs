using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules.PT;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation.Unit.PT;

public class ChargeItemValidationsTests
{
    private readonly ChargeItemValidations _validator = new();

    [Fact]
    public void DescriptionTooShort_ShouldHaveError()
    {
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "AB",  // Only 2 chars - PT requires 3+
                    VATRate = 23.0m,
                    Amount = 10.00m
                }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Description")
              .WithErrorCode("MinimumLengthValidator");
    }

    [Fact]
    public void DescriptionExactly3Chars_ShouldPass()
    {
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "ABC",  // Exactly 3 chars - OK
                    VATRate = 23.0m,
                    Amount = 10.00m
                }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("cbChargeItems[0].Description");
    }

    [Fact]
    public void DescriptionMoreThan3Chars_ShouldPass()
    {
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Product Name",
                    VATRate = 23.0m,
                    Amount = 10.00m
                }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("cbChargeItems[0].Description");
    }

    [Fact]
    public void EmptyDescription_ShouldPass_PTRulesOnly()
    {
        // PT rules only check MinLength when Description is not empty
        // Empty description is caught by Global rules (NotEmpty)
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "",
                    VATRate = 23.0m,
                    Amount = 10.00m
                }
            }
        };

        var result = _validator.TestValidate(request);

        // PT rules should not fail on empty (it has .When condition)
        result.ShouldNotHaveValidationErrorFor("cbChargeItems[0].Description");
    }

    [Fact]
    public void SingleCharDescription_ShouldHaveError()
    {
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "X",  // Only 1 char
                    VATRate = 23.0m,
                    Amount = 10.00m
                }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Description");
    }
}
