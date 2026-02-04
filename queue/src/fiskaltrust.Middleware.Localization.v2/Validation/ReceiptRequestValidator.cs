using fiskaltrust.ifPOS.v2;
using FluentValidation;
using GlobalValidations = fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;
using ESValidations = fiskaltrust.Middleware.Localization.v2.Validation.Rules.ES;
using PTValidations = fiskaltrust.Middleware.Localization.v2.Validation.Rules.PT;

namespace fiskaltrust.Middleware.Localization.v2.Validation;

public class ReceiptRequestValidator : AbstractValidator<ReceiptRequest>
{
    public ReceiptRequestValidator()
    {
        // Global rules (apply to all markets)
        Include(new GlobalValidations.ChargeItemValidations());
        Include(new GlobalValidations.ReceiptValidations());

        // ES-specific rules
        RuleSet(RuleSetNames.ES, () =>
        {
            Include(new ESValidations.ChargeItemValidations());
        });

        // PT-specific rules
        RuleSet(RuleSetNames.PT, () =>
        {
            Include(new PTValidations.ChargeItemValidations());
        });
    }
}

public static class RuleSetNames
{
    public const string ES = "ES";
    public const string PT = "PT";
}
