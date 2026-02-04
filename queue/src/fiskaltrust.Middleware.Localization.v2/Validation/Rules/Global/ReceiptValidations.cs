using fiskaltrust.ifPOS.v2;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;

public class ReceiptValidations : AbstractValidator<ReceiptRequest>
{
    public ReceiptValidations()
    {
        // Sum of ChargeItem amounts must equal cbReceiptAmount
        RuleFor(x => x.cbReceiptAmount)
            .Must((request, receiptAmount) =>
            {
                if (!receiptAmount.HasValue) return true;
                if (request.cbChargeItems == null || !request.cbChargeItems.Any()) return true;

                var sum = request.cbChargeItems.Sum(item => item.Amount);
                return sum == receiptAmount.Value;
            })
            .WithMessage(request =>
            {
                var sum = request.cbChargeItems?.Sum(item => item.Amount) ?? 0;
                return $"Sum of ChargeItem amounts ({sum}) does not match cbReceiptAmount ({request.cbReceiptAmount})";
            })
            .WithErrorCode("ChargeItemsSumMismatch")
            .When(x => x.cbReceiptAmount.HasValue);
    }
}
