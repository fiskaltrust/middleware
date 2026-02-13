using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.PT;

public class PayItemValidations : AbstractValidator<ReceiptRequest>
{
    public PayItemValidations()
    {
        Include(new CashPaymentLimit());
    }

    public class CashPaymentLimit : AbstractValidator<ReceiptRequest>
    {
        private const decimal Limit = 3000m;

        public CashPaymentLimit()
        {
            When(x => x.cbPayItems != null && x.cbPayItems.Count > 0, () =>
            {
                RuleFor(x => x.cbPayItems)
                    .Must(payItems =>
                    {
                        var totalCashAmount = payItems!
                            .Where(payItem => payItem.ftPayItemCase.Case() == PayItemCase.CashPayment)
                            .Sum(payItem => payItem.Amount);
                        return totalCashAmount <= Limit;
                    })
                    .WithMessage(request =>
                    {
                        var totalCashAmount = request.cbPayItems!
                            .Where(payItem => payItem.ftPayItemCase.Case() == PayItemCase.CashPayment)
                            .Sum(payItem => payItem.Amount);
                        return $"Cash payment amount ({totalCashAmount:F2}) exceeds limit of {Limit:F2}€";
                    })
                    .WithErrorCode("CashPaymentExceedsLimit");
            });
        }
    }
}
