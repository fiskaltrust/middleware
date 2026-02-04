using fiskaltrust.Middleware.Localization.v2.Validation;

namespace fiskaltrust.Middleware.Localization.QueueES.ValidationFV;

public class ReceiptValidator : MarketValidator
{
    protected override string RuleSetName => RuleSetNames.ES;
}
    