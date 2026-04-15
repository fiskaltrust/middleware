using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.Atoms.Customer;

public class CustomerRequiredForInvoice : AbstractValidator<ReceiptRequest>
{
    public CustomerRequiredForInvoice()
    {
        When(x => x.ftReceiptCase.IsType(ReceiptCaseType.Invoice), () =>
        {
            RuleFor(x => x.cbCustomer)
                .NotNull()
                .WithMessage("Customer is required for invoice receipts.")
                .WithErrorCode("CustomerRequiredForInvoice")
                .WithState(_ => new ValidationHelp("Set cbCustomer with at least CustomerName and CustomerVATId."));
        });
    }
}