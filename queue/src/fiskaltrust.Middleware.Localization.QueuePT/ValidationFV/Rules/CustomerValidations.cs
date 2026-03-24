using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.QueuePT.ValidationFV.Rules;

public class CustomerValidations : AbstractValidator<ReceiptRequest>
{
    public CustomerValidations()
    {
        Include(new CustomerTaxId());
    }

    public class CustomerTaxId : AbstractValidator<ReceiptRequest>
    {
        public CustomerTaxId()
        {
            RuleFor(x => x)
                .Custom((request, context) =>
                {
                    var customer = request.GetCustomerOrNull();
                    if (customer == null)
                        return;

                    if (string.IsNullOrWhiteSpace(customer.CustomerVATId))
                        return;

                    if (!TaxIdValidation.IsValidPortugueseNif(customer.CustomerVATId))
                    {
                        context.AddFailure("cbCustomer.CustomerVATId",
                            $"Invalid Portuguese tax ID (NIF): '{customer.CustomerVATId}'");
                    }
                })
                .When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten));
        }
    }
}
