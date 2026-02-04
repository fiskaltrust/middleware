using fiskaltrust.ifPOS.v2;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.ES;

public class ChargeItemValidations : AbstractValidator<ReceiptRequest>
{
    public ChargeItemValidations()
    {
        RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
        {
            chargeItem.RuleFor(x => x.VATAmount).NotNull();
        });
    }
}
