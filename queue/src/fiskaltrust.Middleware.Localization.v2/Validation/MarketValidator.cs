using fiskaltrust.ifPOS.v2;
using FluentValidation;
using FluentValidation.Results;
using GlobalValidations = fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;

namespace fiskaltrust.Middleware.Localization.v2.Validation;

public abstract class MarketValidator
{
    private readonly ReceiptReferenceProvider _receiptReferenceProvider;

    protected MarketValidator(ReceiptReferenceProvider receiptReferenceProvider)
    {
        _receiptReferenceProvider = receiptReferenceProvider;
    }

    protected virtual IEnumerable<IValidator<ReceiptRequest>> GetGlobalValidators()
    {
        yield return new GlobalValidations.ReceiptValidations(_receiptReferenceProvider);
        yield return new GlobalValidations.ChargeItemValidations();
    }

    protected virtual IEnumerable<IValidator<ReceiptRequest>> GetMarketValidators() => [];

    public async Task<ValidationResult> ValidateAsync(ReceiptRequest request)
    {
        var result = new ValidationResult();

        foreach (var validator in GetGlobalValidators())
        {
            result.Errors.AddRange((await validator.ValidateAsync(request)).Errors);
        }

        foreach (var validator in GetMarketValidators())
        {
            result.Errors.AddRange((await validator.ValidateAsync(request)).Errors);
        }

        return result;
    }
}
