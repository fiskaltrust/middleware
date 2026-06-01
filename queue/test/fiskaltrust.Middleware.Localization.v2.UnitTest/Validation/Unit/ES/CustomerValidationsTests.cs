using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.Localization.QueueES.ValidationFV.Rules;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation;

public class ESCustomerValidationsTests
{
    // ES receipt cases
    private static ReceiptCase EsPosCase => ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("ES");
    private static ReceiptCase EsInvoiceCase => ReceiptCase.InvoiceB2C0x1001.WithCountry("ES");
    private static ReceiptCase PtPosCase => ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");

    private static object SerializeCustomer(MiddlewareCustomer customer) => customer;

    // ─── CustomerRequiredForInvoice ────────────────────────────────────────────

    [Fact]
    public void CustomerRequiredForInvoice_InvoiceWithoutCustomer_ShouldFail()
    {
        var validator = new CustomerValidations();
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsInvoiceCase,
            cbCustomer = null,
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbCustomer)
            .WithErrorCode("CustomerRequiredForInvoice");
    }

    [Fact]
    public void CustomerRequiredForInvoice_InvoiceWithCustomer_ShouldPass()
    {
        var validator = new CustomerValidations();
        var customer = new MiddlewareCustomer
        {
            CustomerName = "Test Customer",
            CustomerZip = "28001",
            CustomerStreet = "Calle Mayor 1",
            CustomerVATId = "B83975577"
        };
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsInvoiceCase,
            cbCustomer = SerializeCustomer(customer),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.cbCustomer);
    }

    [Fact]
    public void CustomerRequiredForInvoice_NonInvoiceWithoutCustomer_ShouldPass()
    {
        var validator = new CustomerValidations();
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsPosCase,
            cbCustomer = null,
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.cbCustomer);
    }

    // ─── CustomerTaxId ─────────────────────────────────────────────────────────

    [Fact]
    public void CustomerTaxId_InvalidSpanishNif_ShouldFail()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        var customer = new MiddlewareCustomer
        {
            CustomerVATId = "INVALID123",
            CustomerCountry = "ES"
        };
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsPosCase,
            cbCustomer = SerializeCustomer(customer),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbCustomer.CustomerVATId")
            .WithErrorCode("InvalidSpanishTaxId");
    }

    [Fact]
    public void CustomerTaxId_ValidSpanishNif_ShouldPass()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        // B83975577 is a valid Spanish NIF
        var customer = new MiddlewareCustomer
        {
            CustomerVATId = "B83975577",
            CustomerCountry = "ES"
        };
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsPosCase,
            cbCustomer = SerializeCustomer(customer),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CustomerTaxId_NoCustomer_ShouldPass()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsPosCase,
            cbCustomer = null,
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CustomerTaxId_ForeignCountryCustomer_ShouldSkip()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        var customer = new MiddlewareCustomer
        {
            CustomerVATId = "NOTANIF",
            CustomerCountry = "DE"
        };
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsPosCase,
            cbCustomer = SerializeCustomer(customer),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── CustomerMandatoryFields ───────────────────────────────────────────────

    [Fact]
    public void CustomerMandatoryFields_EmptyName_ShouldFail()
    {
        var validator = new CustomerValidations.CustomerMandatoryFields();
        var customer = new MiddlewareCustomer
        {
            CustomerName = "",
            CustomerZip = "28001",
            CustomerStreet = "Calle Mayor 1"
        };
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsPosCase,
            cbCustomer = SerializeCustomer(customer),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbCustomer.CustomerName")
            .WithErrorCode("CustomerNameMissing");
    }

    [Fact]
    public void CustomerMandatoryFields_EmptyZip_ShouldFail()
    {
        var validator = new CustomerValidations.CustomerMandatoryFields();
        var customer = new MiddlewareCustomer
        {
            CustomerName = "Test",
            CustomerZip = "",
            CustomerStreet = "Calle Mayor 1"
        };
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsPosCase,
            cbCustomer = SerializeCustomer(customer),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbCustomer.CustomerZip")
            .WithErrorCode("CustomerZipMissing");
    }

    [Fact]
    public void CustomerMandatoryFields_EmptyStreet_ShouldFail()
    {
        var validator = new CustomerValidations.CustomerMandatoryFields();
        var customer = new MiddlewareCustomer
        {
            CustomerName = "Test",
            CustomerZip = "28001",
            CustomerStreet = ""
        };
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsPosCase,
            cbCustomer = SerializeCustomer(customer),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbCustomer.CustomerStreet")
            .WithErrorCode("CustomerStreetMissing");
    }

    [Fact]
    public void CustomerMandatoryFields_AllFieldsSet_ShouldPass()
    {
        var validator = new CustomerValidations.CustomerMandatoryFields();
        var customer = new MiddlewareCustomer
        {
            CustomerName = "Test Customer",
            CustomerZip = "28001",
            CustomerStreet = "Calle Mayor 1"
        };
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsPosCase,
            cbCustomer = SerializeCustomer(customer),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CustomerMandatoryFields_NoCustomer_ShouldPass()
    {
        var validator = new CustomerValidations.CustomerMandatoryFields();
        var request = new ReceiptRequest
        {
            ftReceiptCase = EsPosCase,
            cbCustomer = null,
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
