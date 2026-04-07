using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueES.Validation;
using fiskaltrust.Middleware.Localization.QueueES.ValidationFV.Rules;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Validation;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.QueueES.ValidationFV;

public class ReceiptValidator : MarketValidator
{
    private readonly v2.Helpers.ReceiptReferenceProvider _receiptReferenceProvider;
    private readonly VoidValidator _voidValidator;

    public ReceiptValidator(
        v2.Helpers.ReceiptReferenceProvider receiptReferenceProvider,
        VoidValidator voidValidator)
        : base(receiptReferenceProvider)
    {
        _receiptReferenceProvider = receiptReferenceProvider;
        _voidValidator = voidValidator;
    }

    protected override IEnumerable<IValidator<ReceiptRequest>> GetMarketValidators(ReceiptResponse? response = null, object? numberSeries = null)
    {
        yield return new ChargeItemValidations();
        yield return new CustomerValidations();
        yield return new ReceiptValidations(_receiptReferenceProvider, _voidValidator);
    }
}
