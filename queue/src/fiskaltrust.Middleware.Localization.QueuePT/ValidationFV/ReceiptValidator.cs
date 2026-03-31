using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.ValidationFV.Rules;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.storage.V0;
using FluentValidation;
using GlobalValidations = fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;

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

    protected override IEnumerable<IValidator<ReceiptRequest>> GetGlobalValidators(ftQueue? queue = null)
    {
        yield return new PTGlobalReceiptValidations(_receiptReferenceProvider, queue);
        yield return new GlobalValidations.ChargeItemValidations(queue);
    }
    
    protected override IEnumerable<IValidator<ReceiptRequest>> GetMarketValidators(ReceiptResponse? response = null, object? numberSeries = null)
    {
        yield return new ChargeItemValidations();
        yield return new PayItemValidations();
        yield return new UserValidations();
        yield return new CustomerValidations();
        yield return new ReceiptValidations(_receiptReferenceProvider, _documentStatusProvider, _voidValidator, _refundValidator, response, numberSeries);
    }

    private class PTGlobalReceiptValidations : AbstractValidator<ReceiptRequest>
    {
        public PTGlobalReceiptValidations(FVReceiptReferenceProvider receiptReferenceProvider, ftQueue? queue = null)
        {
            Include(new GlobalValidations.ReceiptValidations.MandatoryCollections());
            Include(new GlobalValidations.ReceiptValidations.CurrencyMustBeEur());
            Include(new GlobalValidations.ReceiptValidations.ReceiptBalance());
            Include(new GlobalValidations.ReceiptValidations.RefundReference());
            Include(new GlobalValidations.ReceiptValidations.PaymentTransferReference());
            Include(new GlobalValidations.ReceiptValidations.RefundMustUseSingleReference());
            Include(new GlobalValidations.ReceiptValidations.PartialRefundMustUseSingleReference());
            Include(new GlobalValidations.ReceiptValidations.VoidMustUseSingleReference());
            Include(new GlobalValidations.ReceiptValidations.CountryConsistency(queue));
            Include(new GlobalValidations.ReceiptValidations.PayItemCaseCountryConsistency(queue));
        }
    }
}
