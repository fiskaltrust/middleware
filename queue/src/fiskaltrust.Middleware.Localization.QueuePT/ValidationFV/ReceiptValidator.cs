using fiskaltrust.Middleware.Localization.v2.Validation;

namespace fiskaltrust.Middleware.Localization.QueuePT.ValidationFV;

public class ReceiptValidator : MarketValidator
{
    protected override string RuleSetName => RuleSetNames.PT;
}
