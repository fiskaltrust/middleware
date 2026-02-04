using fiskaltrust.ifPOS.v2;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;

public class ChargeItemValidations : AbstractValidator<ReceiptRequest>
{
    public ChargeItemValidations()
    {
        RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
        {
            chargeItem.RuleFor(x => x.Description).NotEmpty();
            chargeItem.RuleFor(x => x.VATRate).GreaterThanOrEqualTo(0);
            chargeItem.RuleFor(x => x.Amount).NotEqual(0m);
        });
    }
}
