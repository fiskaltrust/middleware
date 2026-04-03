
using fiskaltrust.ifPOS.v2;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.Atoms;

public class CurrencyMustBeEur : AbstractValidator<ReceiptRequest>
{
    public CurrencyMustBeEur()
    {
        RuleFor(x => x.Currency)
            .Equal(Currency.EUR)
            .WithMessage(request => $"Only EUR currency is supported, but received '{request.Currency}'.")
            .WithErrorCode("OnlyEuroCurrencySupported");
    }
}