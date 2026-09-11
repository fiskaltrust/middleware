using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Validation;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.QueuePL.Validation;

/// <summary>
/// PL market validations. The queue currency for Poland is PLN (rfcs/0705-queue-single-currency):
/// a queue totalizes in exactly one currency, so every receipt — and every charge/pay item on it —
/// must carry PLN. Currency defaults to EUR in the data format, so PosCreators must set it explicitly.
/// </summary>
public class ReceiptValidationsPL : AbstractValidator<ReceiptRequest>
{
    public ReceiptValidationsPL()
    {
        RuleFor(x => x.Currency)
            .Equal(Currency.PLN)
            .WithMessage(request => $"Expected currency PLN for this queue but received '{request.Currency}'.")
            .WithErrorCode("CurrencyMustMatchMarket");

        RuleForEach(x => x.cbChargeItems)
            .Must((request, chargeItem) => chargeItem.Currency == request.Currency)
            .WithMessage((request, chargeItem) => $"Charge item '{chargeItem.Description}' has currency '{chargeItem.Currency}' but the receipt currency is '{request.Currency}'.")
            .WithErrorCode("CurrencyMustMatchMarket");

        RuleForEach(x => x.cbPayItems)
            .Must((request, payItem) => payItem.Currency == request.Currency)
            .WithMessage((request, payItem) => $"Pay item '{payItem.Description}' has currency '{payItem.Currency}' but the receipt currency is '{request.Currency}'.")
            .WithErrorCode("CurrencyMustMatchMarket");
    }
}

public class ReceiptValidatorPL : MarketValidator
{
    public ReceiptValidatorPL(ReceiptReferenceProvider receiptReferenceProvider) : base(receiptReferenceProvider) { }

    protected override IEnumerable<IValidator<ReceiptRequest>> GetMarketValidators(ReceiptResponse? response = null, object? numberSeries = null)
    {
        yield return new ReceiptValidationsPL();
    }
}
