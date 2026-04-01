using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.ValidationFV.Rules;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Validation;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.QueuePT.ValidationFV;

public class ReceiptValidator : MarketValidator
{
    private readonly FVReceiptReferenceProvider _receiptReferenceProvider;
    private readonly DocumentStatusProvider _documentStatusProvider;
    private readonly VoidValidator _voidValidator;
    private readonly RefundValidator _refundValidator;

    public ReceiptValidator(
        FVReceiptReferenceProvider receiptReferenceProvider,
        DocumentStatusProvider documentStatusProvider,
        VoidValidator voidValidator,
        RefundValidator refundValidator)
        : base(receiptReferenceProvider)
    {
        _receiptReferenceProvider = receiptReferenceProvider;
        _documentStatusProvider = documentStatusProvider;
        _voidValidator = voidValidator;
        _refundValidator = refundValidator;
    }

    protected override IEnumerable<IValidator<ReceiptRequest>> GetMarketValidators(ReceiptResponse? response = null, object? numberSeries = null)
    {
        yield return new ChargeItemValidations();
        yield return new PayItemValidations();
        yield return new UserValidations();
        yield return new CustomerValidations();
        yield return new ReceiptValidations(_receiptReferenceProvider, _documentStatusProvider, _voidValidator, _refundValidator, response, numberSeries);
    }

}
