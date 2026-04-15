using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.storage.V0;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;

public class PayItemValidations : AbstractValidator<ReceiptRequest>
{
    public PayItemValidations(ftQueue? queue = null)
    {
        Include(new PayItemCaseCountryConsistency(queue));
    }


    public class PayItemCaseCountryConsistency : AbstractValidator<ReceiptRequest>
    {
        public PayItemCaseCountryConsistency(ftQueue? queue)
        {
            When(x => x.cbPayItems != null, () =>
            {
                RuleForEach(x => x.cbPayItems).ChildRules(payItem =>
                {
                    payItem.RuleFor(x => x.ftPayItemCase)
                        .Must(c => c.Country() == queue!.CountryCode)
                        .WithMessage(item => $"Pay item case country '{item.ftPayItemCase.Country()}' does not match queue country '{queue!.CountryCode}'.")
                        .WithErrorCode("PayItemCaseCountryMismatch")
                        .WithState(_ => new ValidationHelp("Use ftPayItemCase values that match the country code configured for this queue."));
                });
            });
        }
    }
}
