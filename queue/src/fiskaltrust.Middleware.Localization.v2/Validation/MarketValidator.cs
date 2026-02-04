using fiskaltrust.ifPOS.v2;
using FluentValidation;
using FluentValidation.Results;

namespace fiskaltrust.Middleware.Localization.v2.Validation;

public abstract class MarketValidator
{
    private readonly ReceiptRequestValidator _validator = new();

    protected abstract string RuleSetName { get; }

    public ValidationResult Validate(ReceiptRequest request)
    {
        return _validator.Validate(request, opts =>
            opts.IncludeRulesNotInRuleSet()          // Global rules
                .IncludeRuleSets(RuleSetName));       // Market rules
    }

    public void ValidateAndThrow(ReceiptRequest request)
    {
        var result = Validate(request);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
    }

    public IEnumerable<string> GetErrorMessages(ReceiptRequest request)
    {
        var result = Validate(request);
        return result.Errors.Select(e => $"[{e.ErrorCode}] {e.PropertyName}: {e.ErrorMessage}");
    }
}
