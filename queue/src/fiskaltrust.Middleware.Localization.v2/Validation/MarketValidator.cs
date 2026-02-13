using fiskaltrust.ifPOS.v2;
using FluentValidation;
using FluentValidation.Results;
using GlobalValidations = fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;

namespace fiskaltrust.Middleware.Localization.v2.Validation;

public abstract class MarketValidator
{
    protected virtual IEnumerable<IValidator<ReceiptRequest>> GetGlobalValidators()
    {
        yield return new GlobalValidations.ReceiptValidations();
        yield return new GlobalValidations.ChargeItemValidations();
    }

    protected virtual IEnumerable<IValidator<ReceiptRequest>> GetMarketValidators() => [];

    public ValidationResult Validate(ReceiptRequest request)
    {
        var result = new ValidationResult();

        foreach (var validator in GetGlobalValidators())
        {
            result.Errors.AddRange(validator.Validate(request).Errors);
        }

        foreach (var validator in GetMarketValidators())
        {
            result.Errors.AddRange(validator.Validate(request).Errors);
        }

        return result;
    }
}
