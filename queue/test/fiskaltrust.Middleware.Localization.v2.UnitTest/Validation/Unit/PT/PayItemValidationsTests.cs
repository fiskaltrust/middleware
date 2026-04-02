using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.ValidationFV.Rules;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation;

public class PTPayItemValidationsTests
{
    private static ReceiptCase PtReceiptCase => ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");

    // PT pay item cases
    private static PayItemCase CashPaymentCase => PayItemCase.CashPayment.WithCountry("PT");
    private static PayItemCase CardPaymentCase => PayItemCase.NonCash.WithCountry("PT");

    // ─── CashPaymentLimit ───────────────────────────────────────────────────────

    [Fact]
    public void CashPaymentLimit_CashExceedsLimit_ShouldFail()
    {
        var validator = new PayItemValidations.CashPaymentLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [],
            cbPayItems = [new PayItem
            {
                Amount = 3000.01m,
                ftPayItemCase = CashPaymentCase
            }]
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPayItems)
            .WithErrorCode("CashPaymentExceedsLimit");
    }

    [Fact]
    public void CashPaymentLimit_CashWithinLimit_ShouldPass()
    {
        var validator = new PayItemValidations.CashPaymentLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [],
            cbPayItems = [new PayItem
            {
                Amount = 3000m,
                ftPayItemCase = CashPaymentCase
            }]
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CashPaymentLimit_NoCashPayItems_ShouldPass()
    {
        var validator = new PayItemValidations.CashPaymentLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [],
            cbPayItems = [new PayItem
            {
                Amount = 5000m,
                ftPayItemCase = CardPaymentCase
            }]
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CashPaymentLimit_EmptyPayItems_ShouldSkip()
    {
        var validator = new PayItemValidations.CashPaymentLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtReceiptCase,
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
