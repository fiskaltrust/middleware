using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.ValidationFV.Rules;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation;

public class PTUserValidationsTests
{
    private static ReceiptCase PtReceiptCase => ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");

    // UserValidations reads cbUser directly as a plain string (via GetcbUserOrNull).
    // GetcbUserOrNull tries JSON deserialization first, falls back to raw string.
    private static string PlainUser(string user) => user;

    // ─── UserStructure ──────────────────────────────────────────────────────────

    [Fact]
    public void UserStructure_UserTooShort_ShouldFail()
    {
        var validator = new UserValidations.UserStructure();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbUser = PlainUser("AB"),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbUser")
            .WithErrorCode("UserTooShort");
    }

    [Fact]
    public void UserStructure_NullUser_ShouldFail()
    {
        var validator = new UserValidations.UserStructure();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbUser = null,
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbUser")
            .WithErrorCode("UserTooShort");
    }

    [Fact]
    public void UserStructure_EmptyUser_ShouldFail()
    {
        var validator = new UserValidations.UserStructure();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbUser = "",
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbUser")
            .WithErrorCode("UserTooShort");
    }

    [Fact]
    public void UserStructure_ValidUser_ShouldPass()
    {
        var validator = new UserValidations.UserStructure();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbUser = PlainUser("John"),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UserStructure_ExactlyThreeChars_ShouldPass()
    {
        var validator = new UserValidations.UserStructure();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbUser = PlainUser("ABC"),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
