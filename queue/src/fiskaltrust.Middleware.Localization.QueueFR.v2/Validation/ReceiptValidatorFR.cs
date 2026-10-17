using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Validation;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Validation;

/// <summary>
/// FR market validations. The queue currency for France is EUR (rfcs/0705-queue-single-currency):
/// a queue totalizes in exactly one currency, so every receipt - and every charge/pay item on it -
/// must carry EUR. Foreign-currency sales are recorded as EUR with the conversion done by the POS.
/// </summary>
public class ReceiptValidationsFR : AbstractValidator<ReceiptRequest>
{
    public ReceiptValidationsFR()
    {
        RuleFor(x => x.Currency)
            .Equal(Currency.EUR)
            .WithMessage(request => $"Expected currency EUR for this queue but received '{request.Currency}'.")
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

public class ReceiptValidatorFR : MarketValidator
{
    public ReceiptValidatorFR(ReceiptReferenceProvider receiptReferenceProvider) : base(receiptReferenceProvider) { }

    protected override IEnumerable<IValidator<ReceiptRequest>> GetMarketValidators(ReceiptResponse? response = null, object? numberSeries = null)
    {
        yield return new ReceiptValidationsFR();
    }
}
