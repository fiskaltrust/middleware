using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Validation;
using FluentValidation;
using ESValidations = fiskaltrust.Middleware.Localization.v2.Validation.Rules.ES;

namespace fiskaltrust.Middleware.Localization.QueueES.ValidationFV;

public class ReceiptValidator : MarketValidator
{
    protected override IEnumerable<IValidator<ReceiptRequest>> GetMarketValidators()
    {
        yield return new ESValidations.ChargeItemValidations();
    }
}
