using fiskaltrust.ifPOS.v2;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.ES;

public class ChargeItemValidations : AbstractValidator<ReceiptRequest>
{
    public ChargeItemValidations()
    {
        Include(new VatAmountRequired());
    }

    public class VatAmountRequired : AbstractValidator<ReceiptRequest>
    {
        public VatAmountRequired()
        {
            RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
            {
                chargeItem.RuleFor(x => x.VATAmount).NotNull();
            });
        }
    }
}
