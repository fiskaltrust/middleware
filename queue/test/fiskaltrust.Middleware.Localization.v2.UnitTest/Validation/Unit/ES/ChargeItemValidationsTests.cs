using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueES.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueES.ValidationFV.Rules;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation;

public class ESChargeItemValidationsTests
{
    private static ReceiptCase EsReceiptCase => ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("ES");
    private static ReceiptCase PtReceiptCase => ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");

    // ─── VatAmountRequired ─────────────────────────────────────────────────────

    [Fact]
    public void VatAmountRequired_NullVatAmount_ShouldFail()
    {
        var validator = new ChargeItemValidations.VatAmountRequired();
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 21m,
                Amount = 10m,
                VATAmount = null
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATAmount")
            .WithErrorCode("ChargeItemVATAmountMissing");
    }

    [Fact]
    public void VatAmountRequired_VatAmountSet_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatAmountRequired();
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 21m,
                Amount = 121m,
                VATAmount = 21m
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void VatAmountRequired_NonEsReceipt_ShouldSkip()
    {
        var validator = new ChargeItemValidations();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 19m,
                Amount = 10m,
                VATAmount = null
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor("cbChargeItems[0].VATAmount");
    }

    // ─── SupportedVatRates ─────────────────────────────────────────────────────

    [Fact]
    public void SupportedVatRates_UnknownServiceVat_ShouldFail()
    {
        var validator = new ChargeItemValidations.SupportedVatRates();
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 0m,
                Amount = 10m,
                ftChargeItemCase = ChargeItemCase.UnknownService.WithCountry("ES")
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].ftChargeItemCase")
            .WithErrorCode("UnsupportedVatRate");
    }

    [Fact]
    public void SupportedVatRates_ParkingVatRate_ShouldFail()
    {
        var validator = new ChargeItemValidations.SupportedVatRates();
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 5m,
                Amount = 10m,
                ftChargeItemCase = ChargeItemCase.ParkingVatRate.WithCountry("ES")
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
            ftReceiptCase = EsReceiptCase,
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

    // ─── VatRateCategory ───────────────────────────────────────────────────────

    [Fact]
    public void VatRateCategory_NormalVatRateWith21Percent_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatRateCategory();
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsReceiptCase,
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

    [Fact]
    public void VatRateCategory_NormalVatRateWithWrongPercent_ShouldFail()
    {
        var validator = new ChargeItemValidations.VatRateCategory();
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 19m,
                Amount = 10m,
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("ES")
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATRate")
            .WithErrorCode("VatRateMismatch");
    }

    [Fact]
    public void VatRateCategory_DiscountedVatRate1With10Percent_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatRateCategory();
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 10m,
                Amount = 10m,
                ftChargeItemCase = ChargeItemCase.DiscountedVatRate1.WithCountry("ES")
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void VatRateCategory_SuperReducedVatRate1With4Percent_ShouldPass()
    {
        var validator = new ChargeItemValidations.VatRateCategory();
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 4m,
                Amount = 10m,
                ftChargeItemCase = ChargeItemCase.SuperReducedVatRate1.WithCountry("ES")
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── ZeroVatNature ─────────────────────────────────────────────────────────

    [Fact]
    public void ZeroVatNature_ZeroVatWithUsualVatApplies_ShouldFail()
    {
        var validator = new ChargeItemValidations.ZeroVatNature();
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 0m,
                Amount = 10m,
                ftChargeItemCase = ChargeItemCase.ZeroVatRate.WithCountry("ES")
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbChargeItems[0].VATRate")
            .WithErrorCode("ZeroVatNature");
    }

    [Fact]
    public void ZeroVatNature_ZeroVatWithValidNature_ShouldPass()
    {
        var validator = new ChargeItemValidations.ZeroVatNature();

        var natureValue = (long) ChargeItemCaseNatureOfVatES.ExteptArticle20;
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsReceiptCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 0m,
                Amount = 10m,
                ftChargeItemCase = (ChargeItemCase) (0x4553_0000_0000_0007L | natureValue)
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ZeroVatNature_NonZeroVatRate_ShouldPass()
    {
        var validator = new ChargeItemValidations.ZeroVatNature();
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsReceiptCase,
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
