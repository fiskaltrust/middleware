using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules.PT;
using FluentValidation.TestHelper;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation.Unit.PT;

public class PayItemValidationsTests
{
    #region CashPaymentLimit

    [Fact]
    public void CashPayment_UnderLimit_ShouldPass()
    {
        var validator = new PayItemValidations.CashPaymentLimit();
        var request = new ReceiptRequest
        {
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 2000m, ftPayItemCase = (PayItemCase)((long)PayItemCase.CashPayment) }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CashPayment_OverLimit_ShouldFail()
    {
        var validator = new PayItemValidations.CashPaymentLimit();
        var request = new ReceiptRequest
        {
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 3500m, ftPayItemCase = (PayItemCase)((long)PayItemCase.CashPayment) }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError().WithErrorCode("CashPaymentExceedsLimit");
    }

    [Fact]
    public void CashPayment_ExactLimit_ShouldPass()
    {
        var validator = new PayItemValidations.CashPaymentLimit();
        var request = new ReceiptRequest
        {
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 3000m, ftPayItemCase = (PayItemCase)((long)PayItemCase.CashPayment) }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NonCashPayment_OverLimit_ShouldPass()
    {
        var validator = new PayItemValidations.CashPaymentLimit();
        var request = new ReceiptRequest
        {
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 5000m, ftPayItemCase = (PayItemCase)((long)PayItemCase.DebitCardPayment) }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NullPayItems_ShouldPass()
    {
        var validator = new PayItemValidations.CashPaymentLimit();
        var request = new ReceiptRequest { cbPayItems = null };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MultipleCashPayments_CombinedOverLimit_ShouldFail()
    {
        var validator = new PayItemValidations.CashPaymentLimit();
        var request = new ReceiptRequest
        {
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 2000m, ftPayItemCase = (PayItemCase)((long)PayItemCase.CashPayment) },
                new PayItem { Amount = 1500m, ftPayItemCase = (PayItemCase)((long)PayItemCase.CashPayment) }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError().WithErrorCode("CashPaymentExceedsLimit");
    }

    #endregion
}
