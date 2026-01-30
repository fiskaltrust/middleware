using fiskaltrust.ifPOS.v2;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation;

public class ValidationRunner
{
    private readonly ReceiptRequestValidator _validator = new();

    public IEnumerable<ValidationResult> Validate(ReceiptRequest request, params string[] ruleSets)
    {
        var fvResult = _validator.Validate(request, opts => opts.IncludeRuleSets(ruleSets));

        foreach (var error in fvResult.Errors)
        {
            yield return ValidationResult.Failed(error.ErrorMessage, error.ErrorCode, error.PropertyName);
        }
    }

    public ValidationResultCollection ValidateAndCollect(ReceiptRequest request, params string[] ruleSets)
    {
        return new ValidationResultCollection(Validate(request, ruleSets).ToList());
    }
}
