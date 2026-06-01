using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.ValidationFV.Rules;
using fiskaltrust.Middleware.Localization.v2.Models;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation;

public class PTCustomerValidationsTests
{
    private static ReceiptCase PtReceiptCase => ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");
    private static ReceiptCase PtHandWrittenCase => ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.HandWritten);

    private static object SerializeCustomer(string? vatId, string? country = null) =>
        new MiddlewareCustomer { CustomerVATId = vatId, CustomerCountry = country };

    // ─── CustomerTaxId ──────────────────────────────────────────────────────────

    [Fact]
    public void CustomerTaxId_InvalidPortugueseNif_ShouldFail()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        // 123456780: sum=156, expected check digit=9, actual=0 → invalid
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbCustomer = SerializeCustomer("123456780"),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbCustomer.CustomerVATId")
            .WithErrorCode("InvalidPortugueseTaxId");
    }

    [Fact]
    public void CustomerTaxId_ValidPortugueseNif_ShouldPass()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        // 123456789: sum=156, expected check digit=9, actual=9 → valid
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbCustomer = SerializeCustomer("123456789"),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CustomerTaxId_HandWritten_ShouldSkip()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtHandWrittenCase,
            cbCustomer = SerializeCustomer("InvalidPortugueseTaxId"),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CustomerTaxId_NoCustomer_ShouldSkip()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbCustomer = null,
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CustomerTaxId_CustomerWithoutVatId_ShouldSkip()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbCustomer = SerializeCustomer(null),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CustomerTaxId_ForeignCustomer_ShouldSkipNifValidation()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbCustomer = SerializeCustomer("026883248", "GR"),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CustomerTaxId_NoCountry_InvalidNif_ShouldFail()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbCustomer = SerializeCustomer("123456780"),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbCustomer.CustomerVATId")
            .WithErrorCode("InvalidPortugueseTaxId");
    }
}
