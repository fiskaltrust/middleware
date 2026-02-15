using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Validation;
using FluentValidation;
using PTValidations = fiskaltrust.Middleware.Localization.v2.Validation.Rules.PT;

namespace fiskaltrust.Middleware.Localization.QueuePT.ValidationFV;

public class ReceiptValidator : MarketValidator
{
    protected override IEnumerable<IValidator<ReceiptRequest>> GetMarketValidators()
    {
        yield return new PTValidations.ChargeItemValidations();
        yield return new PTValidations.PayItemValidations();
        yield return new PTValidations.UserValidations();
        yield return new PTValidations.CustomerValidations();
    }
}
