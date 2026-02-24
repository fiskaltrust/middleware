using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules.ES;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation.Unit.ES;

public class CustomerValidationsTests
{
    #region CustomerRequiredForInvoice

    [Fact]
    public void Invoice_WithCustomer_ShouldPass()
    {
        var validator = new CustomerValidations.CustomerRequiredForInvoice();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbCustomer = new MiddlewareCustomer { CustomerName = "Test" }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Invoice_WithoutCustomer_ShouldFail()
    {
        var validator = new CustomerValidations.CustomerRequiredForInvoice();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbCustomer = null
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbCustomer)
              .WithErrorCode("CustomerRequiredForInvoice");
    }

    [Fact]
    public void NonInvoice_WithoutCustomer_ShouldPass()
    {
        var validator = new CustomerValidations.CustomerRequiredForInvoice();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbCustomer = null
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region CustomerTaxId

    [Fact]
    public void ValidSpanishNif_ShouldPass()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        var customer = new MiddlewareCustomer { CustomerVATId = "12345678Z", CustomerCountry = "ES" };
        var request = new ReceiptRequest { cbCustomer = customer };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void InvalidSpanishNif_ShouldFail()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        var customer = new MiddlewareCustomer { CustomerVATId = "INVALID", CustomerCountry = "ES" };
        var request = new ReceiptRequest { cbCustomer = customer };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void NoCustomer_ShouldPass()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        var request = new ReceiptRequest { cbCustomer = null };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyVatId_ShouldPass()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        var customer = new MiddlewareCustomer { CustomerVATId = "", CustomerCountry = "ES" };
        var request = new ReceiptRequest { cbCustomer = customer };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region CustomerMandatoryFields

    [Fact]
    public void AllFieldsPresent_ShouldPass()
    {
        var validator = new CustomerValidations.CustomerMandatoryFields();
        var customer = new MiddlewareCustomer
        {
            CustomerName = "Test Customer",
            CustomerZip = "28001",
            CustomerStreet = "Calle Mayor 1"
        };
        var request = new ReceiptRequest { cbCustomer = customer };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MissingName_ShouldFail()
    {
        var validator = new CustomerValidations.CustomerMandatoryFields();
        var customer = new MiddlewareCustomer
        {
            CustomerName = null,
            CustomerZip = "28001",
            CustomerStreet = "Calle Mayor 1"
        };
        var request = new ReceiptRequest { cbCustomer = customer };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void MandatoryFields_NoCustomer_ShouldPass()
    {
        var validator = new CustomerValidations.CustomerMandatoryFields();
        var request = new ReceiptRequest { cbCustomer = null };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
