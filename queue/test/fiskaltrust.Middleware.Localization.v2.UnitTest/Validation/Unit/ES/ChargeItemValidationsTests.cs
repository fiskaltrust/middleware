using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Models.Cases.ES;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules.ES;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation.Unit.ES;

public class ChargeItemValidationsTests
{
    #region VatAmountRequired

    [Fact]
    public void MissingVATAmount_ShouldHaveError()
    {
        var validator = new ChargeItemValidations.VatAmountRequired();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Test", VATRate = 21.0m, Amount = 10.00m, VATAmount = null }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATAmount");
    }

    [Fact]
    public void WithVATAmount_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatAmountRequired();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Test", VATRate = 21.0m, Amount = 10.00m, VATAmount = 1.74m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor("cbChargeItems[0].VATAmount");
    }

    [Fact]
    public void ZeroVATAmount_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatAmountRequired();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Exempt", VATRate = 0m, Amount = 10.00m, VATAmount = 0m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor("cbChargeItems[0].VATAmount");
    }

    #endregion

    #region SupportedVatRates

    [Fact]
    public void NormalVatRate_ShouldPass()
    {
        var validator = new ChargeItemValidations.SupportedVatRates();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { ftChargeItemCase = ChargeItemCase.NormalVatRate }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ParkingVatRate_ShouldFail()
    {
        var validator = new ChargeItemValidations.SupportedVatRates();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { ftChargeItemCase = ChargeItemCase.ParkingVatRate }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].ftChargeItemCase")
              .WithErrorCode("UnsupportedVatRate");
    }

    [Fact]
    public void DiscountedVatRate_ShouldPass()
    {
        var validator = new ChargeItemValidations.SupportedVatRates();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { ftChargeItemCase = ChargeItemCase.DiscountedVatRate1 }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region VatRateCategory

    [Fact]
    public void NormalVatRate21Percent_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatRateCategory();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { ftChargeItemCase = ChargeItemCase.NormalVatRate, VATRate = 21.0m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NormalVatRateWrongPercentage_ShouldFail()
    {
        var validator = new ChargeItemValidations.VatRateCategory();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { ftChargeItemCase = ChargeItemCase.NormalVatRate, VATRate = 23.0m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATRate")
              .WithErrorCode("VatRateMismatch");
    }

    [Fact]
    public void SuperReduced4Percent_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatRateCategory();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { ftChargeItemCase = ChargeItemCase.SuperReducedVatRate1, VATRate = 4.0m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Discounted10Percent_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatRateCategory();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { ftChargeItemCase = ChargeItemCase.DiscountedVatRate1, VATRate = 10.0m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region ZeroVatNature

    [Fact]
    public void ZeroVat_WithValidNature_ShouldPass()
    {
        var validator = new ChargeItemValidations.ZeroVatNature();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    VATRate = 0m,
                    ftChargeItemCase = ChargeItemCase.NotTaxable.WithNatureOfVat(ChargeItemCaseNatureOfVatES.ExteptArticle20)
                }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ZeroVat_WithoutNature_ShouldFail()
    {
        var validator = new ChargeItemValidations.ZeroVatNature();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { VATRate = 0m, ftChargeItemCase = ChargeItemCase.NotTaxable }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATRate")
              .WithErrorCode("ZeroVatNature");
    }

    [Fact]
    public void NonZeroVat_ShouldPass()
    {
        var validator = new ChargeItemValidations.ZeroVatNature();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { VATRate = 21m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
