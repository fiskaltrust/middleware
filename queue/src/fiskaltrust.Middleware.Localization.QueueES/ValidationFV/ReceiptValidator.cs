using fiskaltrust.Middleware.Localization.v2.Validation;

namespace fiskaltrust.Middleware.Localization.QueueES.ValidationFV;

/// <summary>
/// Validates ReceiptRequest using Global + ES-specific rules.
/// </summary>
public class ReceiptValidator : MarketValidator
{
    protected override string RuleSetName => RuleSetNames.ES;
}
