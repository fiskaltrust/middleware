using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.ValidationFV.Rules;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation.Unit.PT;

public class ChargeItemValidationsTests
{
    #region DescriptionMinLength

    [Fact]
    public void DescriptionTooShort_ShouldHaveError()
    {
        var validator = new ChargeItemValidations.DescriptionMinLength();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "AB", VATRate = 23.0m, Amount = 10.00m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Description");
    }

    [Fact]
    public void DescriptionExactly3Chars_ShouldPass()
    {
        var validator = new ChargeItemValidations.DescriptionMinLength();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "ABC", VATRate = 23.0m, Amount = 10.00m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor("cbChargeItems[0].Description");
    }

    [Fact]
    public void EmptyDescription_ShouldPass_PTRulesOnly()
    {
        var validator = new ChargeItemValidations.DescriptionMinLength();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "", VATRate = 23.0m, Amount = 10.00m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor("cbChargeItems[0].Description");
    }

    [Fact]
    public void SingleCharDescription_ShouldHaveError()
    {
        var validator = new ChargeItemValidations.DescriptionMinLength();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "X", VATRate = 23.0m, Amount = 10.00m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Description");
    }

    #endregion

    #region PosReceiptNetAmountLimit

    [Fact]
    public void PosReceipt_UnderLimit_ShouldPass()
    {
        var validator = new ChargeItemValidations.PosReceiptNetAmountLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Amount = 123m, VATRate = 23m, VATAmount = 23m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PosReceipt_OverLimit_ShouldFail()
    {
        var validator = new ChargeItemValidations.PosReceiptNetAmountLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Amount = 1300m, VATRate = 23m, VATAmount = 230m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError().WithErrorCode("PosReceiptNetAmountExceedsLimit");
    }

    [Fact]
    public void NonPosReceipt_OverLimit_ShouldPass()
    {
        var validator = new ChargeItemValidations.PosReceiptNetAmountLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Amount = 5000m, VATRate = 23m, VATAmount = 935m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region OtherServiceNetAmountLimit

    [Fact]
    public void OtherService_UnderLimit_ShouldPass()
    {
        var validator = new ChargeItemValidations.OtherServiceNetAmountLimit();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Amount = 50m, VATRate = 23m, VATAmount = 9.35m,
                    ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseTypeOfService.OtherService)
                }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void OtherService_OverLimit_ShouldFail()
    {
        var validator = new ChargeItemValidations.OtherServiceNetAmountLimit();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Amount = 500m, VATRate = 23m, VATAmount = 93.5m,
                    ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseTypeOfService.OtherService)
                }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError().WithErrorCode("OtherServiceNetAmountExceedsLimit");
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
    public void SuperReducedVatRate_ShouldFail()
    {
        var validator = new ChargeItemValidations.SupportedVatRates();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { ftChargeItemCase = ChargeItemCase.SuperReducedVatRate1 }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].ftChargeItemCase")
              .WithErrorCode("UnsupportedVatRate");
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

    #endregion

    #region VatRateCategory

    [Fact]
    public void NormalVatRate23Percent_ShouldPass()
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
                new ChargeItem { ftChargeItemCase = ChargeItemCase.NormalVatRate, VATRate = 21.0m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATRate")
              .WithErrorCode("VatRateMismatch");
    }

    [Fact]
    public void DiscountedVatRate1_6Percent_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatRateCategory();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { ftChargeItemCase = ChargeItemCase.DiscountedVatRate1, VATRate = 6.0m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DiscountedVatRate2_13Percent_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatRateCategory();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { ftChargeItemCase = ChargeItemCase.DiscountedVatRate2, VATRate = 13.0m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region ZeroVatExemption

    [Fact]
    public void ZeroVat_WithValidExemption_ShouldPass()
    {
        var validator = new ChargeItemValidations.ZeroVatExemption();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    VATRate = 0m,
                    ftChargeItemCase = ChargeItemCase.NotTaxable.WithNatureOfVat(ChargeItemCaseNatureOfVatPT.Group0x30)
                }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ZeroVat_WithoutExemption_ShouldFail()
    {
        var validator = new ChargeItemValidations.ZeroVatExemption();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { VATRate = 0m, ftChargeItemCase = ChargeItemCase.NotTaxable }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATRate")
              .WithErrorCode("ZeroVatExemption");
    }

    [Fact]
    public void NonZeroVat_ShouldPass()
    {
        var validator = new ChargeItemValidations.ZeroVatExemption();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { VATRate = 23m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
