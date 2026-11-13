using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;
using fiskaltrust.storage.V0;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation;

public class ChargeItemValidationsTests
{
    // ─── MandatoryFields ───────────────────────────────────────────────────────

    [Fact]
    public void MandatoryFields_EmptyDescription_ShouldFail()
    {
        var validator = new ChargeItemValidations.MandatoryFields();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem { Description = "", VATRate = 19m, Amount = 10m }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void MandatoryFields_NegativeVatRate_ShouldFail()
    {
        var validator = new ChargeItemValidations.MandatoryFields();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem { Description = "Item", VATRate = -1m, Amount = 10m }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void MandatoryFields_ValidItem_ShouldPass()
    {
        var validator = new ChargeItemValidations.MandatoryFields();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem { Description = "Item", VATRate = 19m, Amount = 10m }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MandatoryFields_HandWritten_ShouldSkip()
    {
        var validator = new ChargeItemValidations.MandatoryFields();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.HandWritten),
            cbChargeItems = [new ChargeItem { Description = "", VATRate = -5m, Amount = 0m }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── VatCalculation ────────────────────────────────────────────────────────

    [Fact]
    public void VatCalculation_VatAmountMismatch_ShouldFail()
    {
        var validator = new ChargeItemValidations.VatCalculation();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 19m,
                Amount = 100m,
                VATAmount = 5.00m
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATAmount")
            .WithErrorCode("VatAmountMismatch");
    }

    [Fact]
    public void VatCalculation_CorrectVatAmount_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatCalculation();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 19m,
                Amount = 119m,
                VATAmount = 19.00m
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void VatCalculation_NullVatAmount_ShouldSkip()
    {
        var validator = new ChargeItemValidations.VatCalculation();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 19m,
                Amount = 100m,
                VATAmount = null
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void VatCalculation_HandWritten_ShouldSkip()
    {
        var validator = new ChargeItemValidations.VatCalculation();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.HandWritten),
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 19m,
                Amount = 100m,
                VATAmount = 99.99m
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── NegativeAmounts ───────────────────────────────────────────────────────

    [Fact]
    public void NegativeAmounts_NegativeQuantityOnNonDiscountItem_ShouldFail()
    {
        var validator = new ChargeItemValidations.NegativeAmounts();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 19m,
                Amount = 10m,
                Quantity = -1m,
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Quantity")
            .WithErrorCode("NegativeQuantityNotAllowed");
    }

    [Fact]
    public void NegativeAmounts_NegativeAmountOnNonDiscountItem_ShouldFail()
    {
        var validator = new ChargeItemValidations.NegativeAmounts();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 19m,
                Amount = -10m,
                Quantity = 1m,
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].Amount")
            .WithErrorCode("NegativeAmountNotAllowed");
    }

    [Fact]
    public void NegativeAmounts_PositiveAmounts_ShouldPass()
    {
        var validator = new ChargeItemValidations.NegativeAmounts();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 19m,
                Amount = 10m,
                Quantity = 1m,
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NegativeAmounts_HandWritten_ShouldSkip()
    {
        var validator = new ChargeItemValidations.NegativeAmounts();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.HandWritten),
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 19m,
                Amount = -10m,
                Quantity = -1m,
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NegativeAmounts_RefundFlag_ShouldSkip()
    {
        var validator = new ChargeItemValidations.NegativeAmounts();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund),
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 19m,
                Amount = -10m,
                Quantity = -1m,
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// A negative down-payment line (flag 0x0008) stays refused by the shared rule: France exempts it in its own
    /// validator (market-fr#221, ADR 0016), the other markets keep the rule.
    /// </summary>
    [Fact]
    public void NegativeAmounts_DownpaymentDeduction_StillFails()
    {
        var validator = new ChargeItemValidations.NegativeAmounts();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001.WithCountry("FR"),
            cbChargeItems =
            [
                new ChargeItem { Description = "Item", VATRate = 20m, Amount = 3000m, Quantity = 1m, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery) },
                new ChargeItem { Description = "Down payment received", VATRate = 20m, Amount = -1200m, Quantity = -1m, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR").WithFlag(ChargeItemCaseFlags.Downpayment) },
            ],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[1].Amount").WithErrorCode("NegativeAmountNotAllowed");
    }

    private static ReceiptRequest WithOneItem(ReceiptCase receiptCase, long chargeItemCase, decimal amount) => new()
    {
        ftReceiptCase = receiptCase.WithCountry("FR"),
        cbChargeItems = [new ChargeItem { Description = "Move", VATRate = 0m, Amount = amount, Quantity = 1m, ftChargeItemCase = ((ChargeItemCase) chargeItemCase).WithCountry("FR") }],
        cbPayItems = []
    };

    /// <summary>
    /// Money leaving the drawer (a negative not-taxable cash transfer, receivable or voucher) stays refused by the
    /// shared rule; France exempts it in its own validator (market-fr#221, ADR 0016).
    /// </summary>
    [Theory]
    [InlineData(ReceiptCase.PaymentTransfer0x0002, 0x00A8)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002, 0x0098)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002, 0x0048)]
    [InlineData(ReceiptCase.Pay0x3005, 0x00A8)]
    public void NegativeAmounts_DrawerMoveOnAPaymentTransfer_StillFails(ReceiptCase receiptCase, long chargeItemCase)
        => new ChargeItemValidations.NegativeAmounts().TestValidate(WithOneItem(receiptCase, chargeItemCase, -500m))
            .ShouldHaveValidationErrorFor("cbChargeItems[0].Amount").WithErrorCode("NegativeAmountNotAllowed");

    /// <summary>A market's exemption sees the receipt and the charge item.</summary>
    [Fact]
    public void NegativeAmounts_MarketExemption_SkipsTheItemsItNames()
    {
        var validator = new ChargeItemValidations.NegativeAmounts((request, item) => request.ftReceiptCase.IsCase(ReceiptCase.Pay0x3005) && item.Description == "Move");

        validator.TestValidate(WithOneItem(ReceiptCase.Pay0x3005, 0x00A8, -500m)).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(WithOneItem(ReceiptCase.PointOfSaleReceipt0x0001, 0x00A8, -500m))
            .ShouldHaveValidationErrorFor("cbChargeItems[0].Amount").WithErrorCode("NegativeAmountNotAllowed");
    }

    [Theory]
    [InlineData(ReceiptCase.PaymentTransfer0x0002, 0x00A3)] // taxed cash transfer
    [InlineData(ReceiptCase.PaymentTransfer0x0002, 0x0018)] // not-taxable delivery
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001, 0x00A8)] // cash transfer on a sale
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001, 0x0048)] // voucher redemption on a sale: not decided here (middleware#771)
    public void NegativeAmounts_OtherNegativeItems_StillFail(ReceiptCase receiptCase, long chargeItemCase)
        => new ChargeItemValidations.NegativeAmounts().TestValidate(WithOneItem(receiptCase, chargeItemCase, -500m))
            .ShouldHaveValidationErrorFor("cbChargeItems[0].Amount").WithErrorCode("NegativeAmountNotAllowed");

    // ─── ZeroVatRateMustHaveNature ─────────────────────────────────────────────

    [Fact]
    public void ZeroVatRateMustHaveNature_ZeroVatWithoutNature_ShouldFail()
    {
        var validator = new ChargeItemValidations.ZeroVatRateMustHaveNature();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 0m,
                Amount = 10m,
                ftChargeItemCase = ChargeItemCase.DiscountedVatRate1.WithCountry("PT")
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].ftChargeItemCase")
            .WithErrorCode("ZeroVatRateMissingNature");
    }

    [Fact]
    public void ZeroVatRateMustHaveNature_ZeroVatWithoutNatureButNotRequiredByMarket_ShouldPass()
    {
        var validator = new ChargeItemValidations.ZeroVatRateMustHaveNature(chargeItem => chargeItem.Description == "Exempt elsewhere");
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [
                new ChargeItem
                {
                    Description = "Exempt elsewhere",
                    VATRate = 0m,
                    Amount = 10m,
                    ftChargeItemCase = ChargeItemCase.ZeroVatRate.WithCountry("PT")
                },
                new ChargeItem
                {
                    Description = "Item",
                    VATRate = 0m,
                    Amount = 10m,
                    ftChargeItemCase = ChargeItemCase.ZeroVatRate.WithCountry("PT")
                }
            ],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor("cbChargeItems[0].ftChargeItemCase");
        result.ShouldHaveValidationErrorFor("cbChargeItems[1].ftChargeItemCase")
            .WithErrorCode("ZeroVatRateMissingNature");
    }

    [Fact]
    public void ZeroVatRateMustHaveNature_ZeroVatWithNatureSet_ShouldPass()
    {
        var validator = new ChargeItemValidations.ZeroVatRateMustHaveNature();
        const long withNature = 0x5054_0000_0000_0101L;
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 0m,
                Amount = 10m,
                ftChargeItemCase = (ChargeItemCase) withNature
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ZeroVatRateMustHaveNature_NonZeroVatRate_ShouldPass()
    {
        var validator = new ChargeItemValidations.ZeroVatRateMustHaveNature();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 19m,
                Amount = 10m,
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ZeroVatRateMustHaveNature_HandWritten_ShouldSkip()
    {
        var validator = new ChargeItemValidations.ZeroVatRateMustHaveNature();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.HandWritten),
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 0m,
                Amount = 10m,
                ftChargeItemCase = ChargeItemCase.DiscountedVatRate1.WithCountry("PT")
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── DiscountLimit ─────────────────────────────────────────────────────────

    [Fact]
    public void DiscountLimit_DiscountExceedsArticleAmount_ShouldFail()
    {
        var validator = new ChargeItemValidations.DiscountLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems =
            [
                new ChargeItem
                {
                    Description = "Article",
                    VATRate = 19m,
                    Amount = 10m,
                    Position = 1,
                    ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
                },
                new ChargeItem
                {
                    Description = "Big discount",
                    VATRate = 19m,
                    Amount = -15m,
                    Position = 1,
                    ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.ExtraOrDiscount)
                }
            ],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems")
            .WithErrorCode("DiscountExceedsArticleAmount");
    }

    [Fact]
    public void DiscountLimit_DiscountWithinArticleAmount_ShouldPass()
    {
        var validator = new ChargeItemValidations.DiscountLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems =
            [
                new ChargeItem
                {
                    Description = "Article",
                    VATRate = 19m,
                    Amount = 10m,
                    Position = 1,
                    ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
                },
                new ChargeItem
                {
                    Description = "Small discount",
                    VATRate = 19m,
                    Amount = -5m,
                    Position = 1,
                    ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.ExtraOrDiscount)
                }
            ],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor("cbChargeItems");
    }

    [Fact]
    public void DiscountLimit_HandWritten_ShouldSkip()
    {
        var validator = new ChargeItemValidations.DiscountLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.HandWritten),
            cbChargeItems =
            [
                new ChargeItem
                {
                    Description = "Article",
                    VATRate = 19m,
                    Amount = 10m,
                    Position = 1,
                    ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
                },
                new ChargeItem
                {
                    Description = "Big discount",
                    VATRate = 19m,
                    Amount = -50m,
                    Position = 1,
                    ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.ExtraOrDiscount)
                }
            ],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── CountryConsistency ────────────────────────────────────────────────────

    [Fact]
    public void CountryConsistency_ChargeItemCountryMismatch_ShouldFail()
    {
        var queue = new ftQueue { CountryCode = "PT" };
        var validator = new ChargeItemValidations.CountryConsistency(queue);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 21m,
                Amount = 10m,
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("ES")
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].ftChargeItemCase")
            .WithErrorCode("ChargeItemCaseCountryMismatch");
    }

    [Fact]
    public void CountryConsistency_MatchingCountry_ShouldPass()
    {
        var queue = new ftQueue { CountryCode = "PT" };
        var validator = new ChargeItemValidations.CountryConsistency(queue);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 19m,
                Amount = 10m,
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CountryConsistency_NullQueue_ShouldSkip()
    {
        var validator = new ChargeItemValidations.CountryConsistency(null);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 21m,
                Amount = 10m,
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("ES")
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
