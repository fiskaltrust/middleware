using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using FluentValidation;
using FluentValidation.Results;
using fiskaltrust.storage.V0;
using GlobalValidations = fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;

namespace fiskaltrust.Middleware.Localization.v2.Validation;

public interface IMarketValidator
{
    public Task<ValidationResult> ValidateAsync(ReceiptRequest request, ftQueue? queue = null, ReceiptResponse? response = null, object? numberSeries = null);

    public ValidationResult LastResult { get; }
}
