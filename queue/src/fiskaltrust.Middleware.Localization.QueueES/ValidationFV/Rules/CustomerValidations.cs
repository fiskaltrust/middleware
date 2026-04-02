using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueES.Helpers;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.QueueES.ValidationFV.Rules;

public class CustomerValidations : AbstractValidator<ReceiptRequest>
{
    public CustomerValidations()
    {
        When(x => x.ftReceiptCase.Country() == "ES", () =>
        {
            Include(new fiskaltrust.Middleware.Localization.v2.Validation.Rules.Atoms.Customer.CustomerRequiredForInvoice());
            Include(new CustomerTaxId());
            Include(new CustomerMandatoryFields());
        });
    }

    public class CustomerTaxId : AbstractValidator<ReceiptRequest>
    {
        public CustomerTaxId()
        {
            RuleFor(x => x)
                .Custom((request, context) =>
                {
                    fiskaltrust.Middleware.Localization.v2.Models.MiddlewareCustomer? customer;
                    try
                    {
                        customer = request.GetCustomerOrNull();
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        context.AddFailure(new FluentValidation.Results.ValidationFailure(
                            "cbCustomer", $"cbCustomer format is invalid: {ex.Message}")
                        { ErrorCode = "CustomerInvalid" });
                        return;
                    }

                    if (customer == null)
                        return;

                    if (string.IsNullOrWhiteSpace(customer.CustomerVATId))
                        return;

                    if (customer.CustomerCountry != "ES" && !string.IsNullOrEmpty(customer.CustomerCountry))
                        return;

                    if (!TaxIdValidation.IsValidSpanishNif(customer.CustomerVATId))
                    {
                        context.AddFailure(new FluentValidation.Results.ValidationFailure(
                            "cbCustomer.CustomerVATId",
                            $"Invalid Spanish tax ID (NIF): '{customer.CustomerVATId}'")
                        { ErrorCode = "InvalidSpanishTaxId" });
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
                        fiskaltrust.Middleware.Localization.v2.Models.MiddlewareCustomer? customer;
                        try
                        {
                            customer = request.GetCustomerOrNull();
                        }
                        catch (System.Text.Json.JsonException ex)
                        {
                            context.AddFailure(new FluentValidation.Results.ValidationFailure(
                                "cbCustomer", $"cbCustomer format is invalid: {ex.Message}")
                            { ErrorCode = "CustomerInvalid" });
                            return;
                        }

                        if (customer == null)
                            return;

                        if (string.IsNullOrEmpty(customer.CustomerName))
                            context.AddFailure(new FluentValidation.Results.ValidationFailure(
                                "cbCustomer.CustomerName", "Customer name must not be empty")
                            { ErrorCode = "CustomerNameMissing" });

                        if (string.IsNullOrEmpty(customer.CustomerZip))
                            context.AddFailure(new FluentValidation.Results.ValidationFailure(
                                "cbCustomer.CustomerZip", "Customer zip code must not be empty")
                            { ErrorCode = "CustomerZipMissing" });

                        if (string.IsNullOrEmpty(customer.CustomerStreet))
                            context.AddFailure(new FluentValidation.Results.ValidationFailure(
                                "cbCustomer.CustomerStreet", "Customer street must not be empty")
                            { ErrorCode = "CustomerStreetMissing" });
                    });
            });
        }
    }
}
