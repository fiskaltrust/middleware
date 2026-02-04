using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation.Unit.Global;

public class ChargeItemValidationsTests
{
    private readonly ChargeItemValidations _validator = new();

    [Fact]
    public void ValidChargeItem_ShouldNotHaveErrors()
    {
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Test Product",
                    VATRate = 23.0m,
                    Amount = 10.00m
                }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyDescription_ShouldHaveError()
    {
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

        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Description")
              .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public void NegativeVATRate_ShouldHaveError()
    {
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Test",
                    VATRate = -1m,
                    Amount = 10.00m
                }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATRate")
              .WithErrorCode("GreaterThanOrEqualValidator");
    }

    [Fact]
    public void ZeroVATRate_ShouldPass()
    {
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Test",
                    VATRate = 0m,
                    Amount = 10.00m
                }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("cbChargeItems[0].VATRate");
    }

    [Fact]
    public void ZeroAmount_ShouldHaveError()
    {
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Test",
                    VATRate = 23.0m,
                    Amount = 0m
                }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Amount")
              .WithErrorCode("NotEqualValidator");
    }

    [Fact]
    public void NegativeAmount_ShouldPass()
    {
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Discount",
                    VATRate = 23.0m,
                    Amount = -5.00m
                }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("cbChargeItems[0].Amount");
    }
}
