using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.QueueES.ValidationFV.Rules;

public class CustomerValidations : AbstractValidator<ReceiptRequest>
{
    public CustomerValidations()
    {
        Include(new fiskaltrust.Middleware.Localization.v2.Validation.Rules.Atoms.Customer.CustomerRequiredForInvoice());
        Include(new CustomerTaxId());
        Include(new CustomerMandatoryFields());
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

                    if (customer.CustomerCountry != "ES" && !string.IsNullOrEmpty(customer.CustomerCountry))
                        return;

                    if (!TaxIdValidation.IsValidSpanishNif(customer.CustomerVATId))
                    {
                        context.AddFailure("cbCustomer.CustomerVATId",
                            $"Invalid Spanish tax ID (NIF): '{customer.CustomerVATId}'");
                    }
                });
        }
    }

    public class CustomerMandatoryFields : AbstractValidator<ReceiptRequest>
    {
        public CustomerMandatoryFields()
        {
            When(x => x.cbCustomer != null, () =>
            {
                RuleFor(x => x)
                    .Custom((request, context) =>
                    {
                        var customer = request.GetCustomerOrNull();
                        if (customer == null)
                            return;

                        if (string.IsNullOrEmpty(customer.CustomerName))
                            context.AddFailure("cbCustomer.CustomerName", "Customer name must not be empty");

                        if (string.IsNullOrEmpty(customer.CustomerZip))
                            context.AddFailure("cbCustomer.CustomerZip", "Customer zip code must not be empty");

                        if (string.IsNullOrEmpty(customer.CustomerStreet))
                            context.AddFailure("cbCustomer.CustomerStreet", "Customer street must not be empty");
                    });
            });
        }
    }
}
