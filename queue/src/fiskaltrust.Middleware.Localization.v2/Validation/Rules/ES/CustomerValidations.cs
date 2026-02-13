using System.Text.RegularExpressions;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.ES;

public class CustomerValidations : AbstractValidator<ReceiptRequest>
{
    public CustomerValidations()
    {
        Include(new CustomerRequiredForInvoice());
        Include(new CustomerTaxId());
        Include(new CustomerMandatoryFields());
    }

    public class CustomerRequiredForInvoice : AbstractValidator<ReceiptRequest>
    {
        public CustomerRequiredForInvoice()
        {
            When(x => x.ftReceiptCase.IsType(ReceiptCaseType.Invoice), () =>
            {
                RuleFor(x => x.cbCustomer)
                    .NotNull()
                    .WithMessage("Customer is required for Invoice receipts")
                    .WithErrorCode("CustomerRequiredForInvoice");
            });
        }
    }

    public class CustomerTaxId : AbstractValidator<ReceiptRequest>
    {
        private static readonly Regex SpanishNifRegex = new(
            @"^([a-zA-Z]\d{7}[a-zA-Z]|\d{8}[a-zA-Z]|[a-zA-Z]\d{8})$",
            RegexOptions.Compiled);

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

                    if (!SpanishNifRegex.IsMatch(customer.CustomerVATId))
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
