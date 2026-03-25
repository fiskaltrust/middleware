using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueES.ValidationFV.Rules;
using fiskaltrust.Middleware.Localization.v2.Validation;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.QueueES.ValidationFV;

public class ReceiptValidator : MarketValidator
{
    public ReceiptValidator(ReceiptReferenceProvider receiptReferenceProvider)
        : base(receiptReferenceProvider)
    {
    }

    protected override IEnumerable<IValidator<ReceiptRequest>> GetMarketValidators(ReceiptResponse? response = null, object? numberSeries = null)
    {
        yield return new ChargeItemValidations();
        yield return new CustomerValidations();
    }
}
