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

                    if (!TaxIdValidation.IsValidPortugueseNif(customer.CustomerVATId))
                    {
                        context.AddFailure(new FluentValidation.Results.ValidationFailure(
                            "cbCustomer.CustomerVATId",
                            $"Invalid Portuguese tax ID (NIF): '{customer.CustomerVATId}'")
                        { ErrorCode = "InvalidPortugueseTaxId" });
                    }
                })
                .When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten));
        }
    }
}
