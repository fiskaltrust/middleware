using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules.ES;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation.Unit.ES;

public class ChargeItemValidationsTests
{
    private readonly ChargeItemValidations _validator = new();

    [Fact]
    public void MissingVATAmount_ShouldHaveError()
    {
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Test Product",
                    VATRate = 21.0m,
                    Amount = 10.00m,
                    VATAmount = null
                }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATAmount")
              .WithErrorCode("NotNullValidator");
    }

    [Fact]
    public void WithVATAmount_ShouldPass()
    {
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Test Product",
                    VATRate = 21.0m,
                    Amount = 10.00m,
                    VATAmount = 1.74m
                }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("cbChargeItems[0].VATAmount");
    }

    [Fact]
    public void ZeroVATAmount_ShouldPass()
    {
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Exempt Product",
                    VATRate = 0m,
                    Amount = 10.00m,
                    VATAmount = 0m
                }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("cbChargeItems[0].VATAmount");
    }
}
