using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using FluentValidation;
using FluentValidation.Results;
using fiskaltrust.storage.V0;
using GlobalValidations = fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;

namespace fiskaltrust.Middleware.Localization.v2.Validation;

public class MarketValidator : IMarketValidator
{
    private readonly ReceiptReferenceProvider _receiptReferenceProvider;

    public ValidationResult LastResult { get; private set; }

    public MarketValidator(ReceiptReferenceProvider receiptReferenceProvider)
    {
        _receiptReferenceProvider = receiptReferenceProvider;
    }

    protected IEnumerable<IValidator<ReceiptRequest>> GetGlobalValidators(ftQueue? queue = null)
    {
        yield return new GlobalValidations.ReceiptValidations(_receiptReferenceProvider, queue);
        yield return new GlobalValidations.PayItemValidations(queue);
        yield return new GlobalValidations.ChargeItemValidations(queue);
    }

    protected virtual IEnumerable<IValidator<ReceiptRequest>> GetMarketValidators(ReceiptResponse? response = null, object? numberSeries = null) => [];

    /// <summary>
    /// The error codes of global rules a market does not apply. Defaults to none; a market lists a
    /// code only where its own rules deliberately differ.
    /// </summary>
    protected virtual IReadOnlyCollection<string> ExemptGlobalErrorCodes => [];

    public async Task<ValidationResult> ValidateAsync(ReceiptRequest request, ftQueue? queue = null, ReceiptResponse? response = null, object? numberSeries = null)
    {
        var result = new ValidationResult();

        foreach (var validator in GetGlobalValidators(queue))
        {
            result.Errors.AddRange((await validator.ValidateAsync(request)).Errors.Where(error => !ExemptGlobalErrorCodes.Contains(error.ErrorCode)));
        }

        foreach (var validator in GetMarketValidators(response, numberSeries))
        {
            result.Errors.AddRange((await validator.ValidateAsync(request)).Errors);
        }

        LastResult = result;
        return result;
    }
}
