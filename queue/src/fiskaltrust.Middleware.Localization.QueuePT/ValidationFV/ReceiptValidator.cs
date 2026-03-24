using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.ValidationFV.Rules;
using fiskaltrust.Middleware.Localization.v2.Validation;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.QueuePT.ValidationFV;

public class ReceiptValidator : MarketValidator
{
    private readonly ReceiptReferenceProvider _receiptReferenceProvider;

    public ReceiptValidator(ReceiptReferenceProvider receiptReferenceProvider)
        : base(receiptReferenceProvider)
    {
        _receiptReferenceProvider = receiptReferenceProvider;
    }

    protected override IEnumerable<IValidator<ReceiptRequest>> GetMarketValidators(ReceiptResponse? response = null)
    {
        yield return new ChargeItemValidations();
        yield return new PayItemValidations();
        yield return new UserValidations();
        yield return new CustomerValidations();
        yield return new ReceiptValidations(_receiptReferenceProvider, response);
    }
}
