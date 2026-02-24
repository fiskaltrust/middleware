using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules.PT;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation.Unit.PT;

public class CustomerValidationsTests
{
    #region CustomerTaxId

    [Fact]
    public void ValidPortugueseNif_ShouldPass()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        var customer = new MiddlewareCustomer { CustomerVATId = "999999990" };
        var request = new ReceiptRequest { cbCustomer = customer };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void InvalidPortugueseNif_ShouldFail()
    {
        var validator = new CustomerValidations.CustomerTaxId();
        var customer = new MiddlewareCustomer { CustomerVATId = "123456781" };
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
        var customer = new MiddlewareCustomer { CustomerVATId = "" };
        var request = new ReceiptRequest { cbCustomer = customer };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
