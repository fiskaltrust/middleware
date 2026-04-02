using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using ReceiptCaseFlags = fiskaltrust.ifPOS.v2.Cases.ReceiptCaseFlags;
using fiskaltrust.Middleware.Localization.QueuePT.ValidationFV.Rules;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation;

public class PTChargeItemValidationsTests
{
    private static ReceiptCase PtReceiptCase => ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");
    private static ReceiptCase PtHandWrittenCase => ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.HandWritten);
    private static ReceiptCase PtInvoiceCase => ReceiptCase.InvoiceB2C0x1001.WithCountry("PT");

    private static ChargeItem ValidChargeItem() => new()
    {
        Description = "Test item",
        VATRate = 23m,
        Amount = 12.3m,
        VATAmount = 2.3m,
        Quantity = 1m,
        ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
    };

    // ─── DescriptionMustNotBeEmpty ──────────────────────────────────────────────

    [Fact]
    public void DescriptionMustNotBeEmpty_EmptyDescription_ShouldFail()
    {
        var validator = new ChargeItemValidations.DescriptionMustNotBeEmpty();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem { Description = "   ", VATRate = 23m, Amount = 10m, Quantity = 1m }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Description")
            .WithErrorCode("ChargeItemDescriptionMissing");
    }

    [Fact]
    public void DescriptionMustNotBeEmpty_ValidDescription_ShouldPass()
    {
        var validator = new ChargeItemValidations.DescriptionMustNotBeEmpty();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [ValidChargeItem()],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DescriptionMustNotBeEmpty_HandWritten_ShouldSkip()
    {
        var validator = new ChargeItemValidations.DescriptionMustNotBeEmpty();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtHandWrittenCase,
            cbChargeItems = [new ChargeItem { Description = "", VATRate = 23m, Amount = 10m }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── DescriptionMinLength ───────────────────────────────────────────────────

    [Fact]
    public void DescriptionMinLength_TooShort_ShouldFail()
    {
        var validator = new ChargeItemValidations.DescriptionMinLength();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem { Description = "AB", VATRate = 23m, Amount = 10m, Quantity = 1m }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Description")
            .WithErrorCode("ChargeItemDescriptionTooShort");
    }

    [Fact]
    public void DescriptionMinLength_SufficientLength_ShouldPass()
    {
        var validator = new ChargeItemValidations.DescriptionMinLength();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [ValidChargeItem()],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── VatRateMustNotBeNegative ───────────────────────────────────────────────

    [Fact]
    public void VatRateMustNotBeNegative_NegativeRate_ShouldFail()
    {
        var validator = new ChargeItemValidations.VatRateMustNotBeNegative();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem { Description = "Item", VATRate = -5m, Amount = 10m, Quantity = 1m }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATRate")
            .WithErrorCode("ChargeItemVatRateMissing");
    }

    [Fact]
    public void VatRateMustNotBeNegative_ZeroRate_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatRateMustNotBeNegative();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem { Description = "Item", VATRate = 0m, Amount = 10m, Quantity = 1m }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── AmountMustNotBeZero ────────────────────────────────────────────────────

    [Fact]
    public void AmountMustNotBeZero_ZeroAmount_ShouldFail()
    {
        var validator = new ChargeItemValidations.AmountMustNotBeZero();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem { Description = "Item", VATRate = 23m, Amount = 0m, Quantity = 1m }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Amount")
            .WithErrorCode("ChargeItemAmountMissing");
    }

    [Fact]
    public void AmountMustNotBeZero_NonZeroAmount_ShouldPass()
    {
        var validator = new ChargeItemValidations.AmountMustNotBeZero();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [ValidChargeItem()],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── QuantityMustNotBeZero ──────────────────────────────────────────────────

    [Fact]
    public void QuantityMustNotBeZero_ZeroQuantity_ShouldFail()
    {
        var validator = new ChargeItemValidations.QuantityMustNotBeZero();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem { Description = "Item", VATRate = 23m, Amount = 10m, Quantity = 0m }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Quantity")
            .WithErrorCode("ChargeItemQuantityZeroNotAllowed");
    }

    [Fact]
    public void QuantityMustNotBeZero_NonZeroQuantity_ShouldPass()
    {
        var validator = new ChargeItemValidations.QuantityMustNotBeZero();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [ValidChargeItem()],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── PosReceiptNetAmountLimit ───────────────────────────────────────────────

    [Fact]
    public void PosReceiptNetAmountLimit_ExceedsLimit_ShouldFail()
    {
        var validator = new ChargeItemValidations.PosReceiptNetAmountLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 23m,
                Amount = 124m,
                VATAmount = 23m,
                Quantity = 1m,
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbChargeItems)
            .WithErrorCode("PosReceiptNetAmountExceedsLimit");
    }

    [Fact]
    public void PosReceiptNetAmountLimit_WithinLimit_ShouldPass()
    {
        var validator = new ChargeItemValidations.PosReceiptNetAmountLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 23m,
                Amount = 123m,
                VATAmount = 23m,
                Quantity = 1m,
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── OtherServiceNetAmountLimit ─────────────────────────────────────────────

    [Fact]
    public void OtherServiceNetAmountLimit_ExceedsLimit_ShouldFail()
    {
        var validator = new ChargeItemValidations.OtherServiceNetAmountLimit();
        var otherServiceCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.OtherService);
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtInvoiceCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Service",
                VATRate = 23m,
                Amount = 124m,
                VATAmount = 23m,
                Quantity = 1m,
                ftChargeItemCase = otherServiceCase
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbChargeItems)
            .WithErrorCode("OtherServiceNetAmountExceedsLimit");
    }

    [Fact]
    public void OtherServiceNetAmountLimit_WithinLimit_ShouldPass()
    {
        var validator = new ChargeItemValidations.OtherServiceNetAmountLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtInvoiceCase,
            cbChargeItems = [ValidChargeItem()],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── SupportedVatRates ──────────────────────────────────────────────────────

    [Fact]
    public void SupportedVatRates_ZeroVatRateCase_ShouldFail()
    {
        var validator = new ChargeItemValidations.SupportedVatRates();
        var zeroVatCase = ChargeItemCase.ZeroVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery);
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 0m,
                Amount = 10m,
                Quantity = 1m,
                ftChargeItemCase = zeroVatCase
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].ftChargeItemCase")
            .WithErrorCode("UnsupportedVatRate");
    }

    [Fact]
    public void SupportedVatRates_NormalVatRate_ShouldPass()
    {
        var validator = new ChargeItemValidations.SupportedVatRates();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [ValidChargeItem()],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── SupportedChargeItemCases ───────────────────────────────────────────────

    [Fact]
    public void SupportedChargeItemCases_UnsupportedTypeOfService_ShouldFail()
    {
        var validator = new ChargeItemValidations.SupportedChargeItemCases();
        // NotOwnSales (0x60) is unsupported in PT
        var unsupportedCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.NotOwnSales);
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 23m,
                Amount = 10m,
                Quantity = 1m,
                ftChargeItemCase = unsupportedCase
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].ftChargeItemCase")
            .WithErrorCode("UnsupportedChargeItemServiceType");
    }

    [Fact]
    public void SupportedChargeItemCases_SupportedTypeOfService_ShouldPass()
    {
        var validator = new ChargeItemValidations.SupportedChargeItemCases();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [ValidChargeItem()],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── VatRateCategory ────────────────────────────────────────────────────────

    [Fact]
    public void VatRateCategory_WrongRateForCategory_ShouldFail()
    {
        var validator = new ChargeItemValidations.VatRateCategory();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 21m,
                Amount = 10m,
                Quantity = 1m,
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATRate")
            .WithErrorCode("VatRateMismatch");
    }

    [Fact]
    public void VatRateCategory_CorrectRateForCategory_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatRateCategory();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [ValidChargeItem()],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── VatAmountCheck ─────────────────────────────────────────────────────────

    [Fact]
    public void VatAmountCheck_IncorrectVatAmount_ShouldFail()
    {
        var validator = new ChargeItemValidations.VatAmountCheck();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 23m,
                Amount = 100m,
                VATAmount = 20m,
                Quantity = 1m,
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATAmount")
            .WithErrorCode("VatAmountMismatch");
    }

    [Fact]
    public void VatAmountCheck_CorrectVatAmount_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatAmountCheck();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [ValidChargeItem()],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── ZeroVatExemption ───────────────────────────────────────────────────────

    [Fact]
    public void ZeroVatExemption_ZeroRateWithoutExemption_ShouldFail()
    {
        var validator = new ChargeItemValidations.ZeroVatExemption();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 0m,
                Amount = 10m,
                Quantity = 1m,
                ftChargeItemCase = ChargeItemCase.NotTaxable.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATRate")
            .WithErrorCode("ZeroVatExemption");
    }

    [Fact]
    public void ZeroVatExemption_ZeroRateWithValidExemption_ShouldPass()
    {
        var validator = new ChargeItemValidations.ZeroVatExemption();
        // NotTaxable (0x18) + NatureOfVat M07 (0x0700) → valid exemption
        // NatureOfVat bits are in 0xFF00 of lower 16 bits: (0x0718 & 0xFF00) = 0x0700 → M07
        var exemptCase = (ChargeItemCase) 0x5054_0000_0000_0718L;
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 0m,
                Amount = 10m,
                Quantity = 1m,
                ftChargeItemCase = exemptCase
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── DiscountOrExtraNotPositive ─────────────────────────────────────────────

    [Fact]
    public void DiscountOrExtraNotPositive_PositiveDiscountAmount_ShouldFail()
    {
        var validator = new ChargeItemValidations.DiscountOrExtraNotPositive();
        var discountCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.ExtraOrDiscount);
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Discount",
                VATRate = 23m,
                Amount = 5m,
                Quantity = 1m,
                ftChargeItemCase = discountCase
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Amount")
            .WithErrorCode("PositiveDiscountNotAllowed");
    }

    [Fact]
    public void DiscountOrExtraNotPositive_NegativeDiscountAmount_ShouldPass()
    {
        var validator = new ChargeItemValidations.DiscountOrExtraNotPositive();
        var discountCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.ExtraOrDiscount);
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Discount",
                VATRate = 23m,
                Amount = -5m,
                Quantity = 1m,
                ftChargeItemCase = discountCase
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── DiscountVatRateAndCaseAlignment ────────────────────────────────────────

    [Fact]
    public void DiscountVatRateAndCaseAlignment_MismatchedVatRate_ShouldFail()
    {
        var validator = new ChargeItemValidations.DiscountVatRateAndCaseAlignment();
        var mainCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery);
        var discountCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.ExtraOrDiscount);
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems =
            [
                new ChargeItem
                {
                    Description = "Main Item",
                    VATRate = 23m,
                    Amount = 10m,
                    Quantity = 1m,
                    ftChargeItemCase = mainCase
                },
                new ChargeItem
                {
                    Description = "Discount",
                    VATRate = 6m,
                    Amount = -1m,
                    Quantity = 1m,
                    ftChargeItemCase = discountCase
                }
            ],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[1]")
            .WithErrorCode("DiscountVatRateOrCaseMismatch");
    }

    [Fact]
    public void DiscountVatRateAndCaseAlignment_AlignedVatRate_ShouldPass()
    {
        var validator = new ChargeItemValidations.DiscountVatRateAndCaseAlignment();
        var mainCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery);
        var discountCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.ExtraOrDiscount);
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems =
            [
                new ChargeItem
                {
                    Description = "Main Item",
                    VATRate = 23m,
                    Amount = 10m,
                    Quantity = 1m,
                    ftChargeItemCase = mainCase
                },
                new ChargeItem
                {
                    Description = "Discount",
                    VATRate = 23m,
                    Amount = -1m,
                    Quantity = 1m,
                    ftChargeItemCase = discountCase
                }
            ],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
