using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules.PT;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation.Unit.PT;

public class UserValidationsTests
{
    #region UserStructure

    [Fact]
    public void ValidUser_ShouldPass()
    {
        var validator = new UserValidations.UserStructure();
        var request = new ReceiptRequest { cbUser = "Operator1" };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UserExactly3Chars_ShouldPass()
    {
        var validator = new UserValidations.UserStructure();
        var request = new ReceiptRequest { cbUser = "Bob" };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UserTooShort_ShouldFail()
    {
        var validator = new UserValidations.UserStructure();
        var request = new ReceiptRequest { cbUser = "AB" };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void NullUser_ShouldFail()
    {
        var validator = new UserValidations.UserStructure();
        var request = new ReceiptRequest { cbUser = null };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError();
    }

    #endregion
}
