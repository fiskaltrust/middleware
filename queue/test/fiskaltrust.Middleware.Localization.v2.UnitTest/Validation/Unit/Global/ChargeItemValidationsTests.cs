using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation.Unit.Global;

public class ChargeItemValidationsTests
{
    #region MandatoryFields

    [Fact]
    public void ValidChargeItem_ShouldNotHaveErrors()
    {
        var validator = new ChargeItemValidations.MandatoryFields();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Test Product", VATRate = 23.0m, Amount = 10.00m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyDescription_ShouldHaveError()
    {
        var validator = new ChargeItemValidations.MandatoryFields();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "", VATRate = 23.0m, Amount = 10.00m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Description");
    }

    [Fact]
    public void NegativeVATRate_ShouldHaveError()
    {
        var validator = new ChargeItemValidations.MandatoryFields();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Test", VATRate = -1m, Amount = 10.00m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATRate");
    }

    [Fact]
    public void ZeroVATRate_ShouldPass()
    {
        var validator = new ChargeItemValidations.MandatoryFields();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Test", VATRate = 0m, Amount = 10.00m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor("cbChargeItems[0].VATRate");
    }

    [Fact]
    public void ZeroAmount_ShouldHaveError()
    {
        var validator = new ChargeItemValidations.MandatoryFields();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Test", VATRate = 23.0m, Amount = 0m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Amount");
    }

    [Fact]
    public void NegativeAmount_ShouldPass()
    {
        var validator = new ChargeItemValidations.MandatoryFields();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Discount", VATRate = 23.0m, Amount = -5.00m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor("cbChargeItems[0].Amount");
    }

    #endregion

    #region ServiceType

    [Fact]
    public void SupportedServiceType_ShouldPass()
    {
        var validator = new ChargeItemValidations.ServiceType();
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
    public void UnsupportedServiceType_ShouldFail()
    {
        var validator = new ChargeItemValidations.ServiceType();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseTypeOfService.Voucher) }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].ftChargeItemCase")
              .WithErrorCode("UnsupportedChargeItemServiceType");
    }

    #endregion

    #region VatCalculation

    [Fact]
    public void CorrectVATAmount_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatCalculation();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Amount = 123m, VATRate = 23m, VATAmount = 23m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void IncorrectVATAmount_ShouldFail()
    {
        var validator = new ChargeItemValidations.VatCalculation();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Amount = 123m, VATRate = 23m, VATAmount = 50m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATAmount")
              .WithErrorCode("VatAmountMismatch");
    }

    [Fact]
    public void NullVATAmount_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatCalculation();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Amount = 123m, VATRate = 23m, VATAmount = null }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ZeroVATRate_VatCalculation_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatCalculation();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Amount = 100m, VATRate = 0m, VATAmount = 0m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region NegativeAmounts

    [Fact]
    public void NegativeQuantity_NonRefund_ShouldFail()
    {
        var validator = new ChargeItemValidations.NegativeAmounts();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Quantity = -1, Amount = 10m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Quantity")
              .WithErrorCode("NegativeQuantityNotAllowed");
    }

    [Fact]
    public void NegativeAmount_NonRefund_ShouldFail()
    {
        var validator = new ChargeItemValidations.NegativeAmounts();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Quantity = 1, Amount = -10m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Amount")
              .WithErrorCode("NegativeAmountNotAllowed");
    }

    [Fact]
    public void NegativeQuantity_RefundReceipt_ShouldPass()
    {
        var validator = new ChargeItemValidations.NegativeAmounts();
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Refund),
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Quantity = -1, Amount = -10m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NegativeAmount_Discount_ShouldPass()
    {
        var validator = new ChargeItemValidations.NegativeAmounts();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = 1, Amount = -10m,
                    ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseFlags.ExtraOrDiscount)
                }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region ZeroVatRateMustHaveNature

    [Fact]
    public void ZeroVatRate_WithNature_ShouldPass()
    {
        var validator = new ChargeItemValidations.ZeroVatRateMustHaveNature();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { VATRate = 0m, ftChargeItemCase = (ChargeItemCase)0x3000 }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ZeroVatRate_WithoutNature_ShouldFail()
    {
        var validator = new ChargeItemValidations.ZeroVatRateMustHaveNature();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { VATRate = 0m, ftChargeItemCase = (ChargeItemCase)0x0000 }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].ftChargeItemCase")
              .WithErrorCode("ZeroVatRateMissingNature");
    }

    [Fact]
    public void NonZeroVatRate_WithoutNature_ShouldPass()
    {
        var validator = new ChargeItemValidations.ZeroVatRateMustHaveNature();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { VATRate = 23m, ftChargeItemCase = (ChargeItemCase)0x0000 }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region DiscountLimit

    [Fact]
    public void ValidDiscount_ShouldPass()
    {
        var validator = new ChargeItemValidations.DiscountLimit();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", Amount = 100m, ftChargeItemCase = ChargeItemCase.NormalVatRate },
                new ChargeItem
                {
                    Description = "Discount", Amount = -10m,
                    ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseFlags.ExtraOrDiscount)
                }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ExcessiveDiscount_ShouldFail()
    {
        var validator = new ChargeItemValidations.DiscountLimit();
        var request = new ReceiptRequest
        {
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", Amount = 50m, ftChargeItemCase = ChargeItemCase.NormalVatRate },
                new ChargeItem
                {
                    Description = "Discount", Amount = -100m,
                    ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseFlags.ExtraOrDiscount)
                }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError();
    }

    #endregion
}
