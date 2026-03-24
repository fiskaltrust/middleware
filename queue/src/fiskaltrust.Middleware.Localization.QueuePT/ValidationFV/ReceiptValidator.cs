using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.ValidationFV.Rules;
using fiskaltrust.Middleware.Localization.v2.Validation;
using FluentValidation;
using ReceiptReferenceProvider = fiskaltrust.Middleware.Localization.v2.Validation.ReceiptReferenceProvider;

namespace fiskaltrust.Middleware.Localization.QueuePT.ValidationFV;

public class ReceiptValidator : MarketValidator
{
    private readonly ReceiptReferenceProvider _receiptReferenceProvider;
    private readonly DocumentStatusProvider _documentStatusProvider;
    private readonly VoidValidator _voidValidator;
    private readonly RefundValidator _refundValidator;

    public ReceiptValidator(
        ReceiptReferenceProvider receiptReferenceProvider,
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

    protected override IEnumerable<IValidator<ReceiptRequest>> GetMarketValidators(ReceiptResponse? response = null)
    {
        yield return new ChargeItemValidations();
        yield return new PayItemValidations();
        yield return new UserValidations();
        yield return new CustomerValidations();
        yield return new ReceiptValidations(_receiptReferenceProvider, _documentStatusProvider, _voidValidator, _refundValidator, response);
    }
}
