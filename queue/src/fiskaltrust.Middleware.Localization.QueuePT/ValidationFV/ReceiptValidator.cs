using fiskaltrust.Middleware.Localization.v2.Validation;

namespace fiskaltrust.Middleware.Localization.QueuePT.ValidationFV;

/// <summary>
/// Validates ReceiptRequest using Global + PT-specific rules.
/// </summary>
public class ReceiptValidator : MarketValidator
{
    protected override string RuleSetName => RuleSetNames.PT;
}
