using fiskaltrust.ifPOS.v2;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.PT;

public class ChargeItemValidations : AbstractValidator<ReceiptRequest>
{
    public ChargeItemValidations()
    {
        RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
        {
            chargeItem.RuleFor(x => x.Description)
                .MinimumLength(3)
                .When(x => !string.IsNullOrEmpty(x.Description));
        });
    }
}
