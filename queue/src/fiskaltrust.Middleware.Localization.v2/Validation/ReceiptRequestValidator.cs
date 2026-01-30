using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation;

public class ReceiptRequestValidator : AbstractValidator<ReceiptRequest>
{
    public ReceiptRequestValidator()
    {
        RuleSet(RuleSetNames.Always, () =>
        {
            RuleFor(x => x).Custom((request, context) =>
            {
                ExecuteRule(GlobalRules.ChargeItemsMandatoryFields(request), context);
            });
        });


        RuleSet(RuleSetNames.ES, () =>
        {
            RuleFor(x => x).Custom((request, context) =>
            {
                ExecuteRule(ESRules.ChargeItemsVATAmountRequired(request), context);
            });
        });
    }

    /// Helper to execute a domain rule and add failures to FV context.
    private static void ExecuteRule(IEnumerable<ValidationResult> results, ValidationContext<ReceiptRequest> context)
    {
        foreach (var result in results)
        {
            foreach (var error in result.Errors)
            {
                context.AddFailure(error.Field ?? "", error.Message);
            }
        }
    }
}