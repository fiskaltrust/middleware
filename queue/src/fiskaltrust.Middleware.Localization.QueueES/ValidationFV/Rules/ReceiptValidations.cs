using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueES.Validation;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.QueueES.ValidationFV.Rules;

public class ReceiptValidations : AbstractValidator<ReceiptRequest>
{
    public ReceiptValidations(v2.Helpers.ReceiptReferenceProvider receiptReferenceProvider, VoidValidator voidValidator)
    {
        Include(new ChargeItemCaseCountryConsistency());
        Include(new VoidFieldsMatch(receiptReferenceProvider, voidValidator));
    }

    public class ChargeItemCaseCountryConsistency : AbstractValidator<ReceiptRequest>
    {
        public ChargeItemCaseCountryConsistency()
        {
            RuleFor(x => x.cbChargeItems)
                .Must(items => items!.All(ci => ci.ftChargeItemCase.Country() == "ES"))
                .When(x => x.cbChargeItems != null)
                .WithMessage("All charge items must use the ES country code.")
                .WithErrorCode("ChargeItemCaseCountryMismatch");
        }
    }

    public class VoidFieldsMatch : AbstractValidator<ReceiptRequest>
    {
        public VoidFieldsMatch(v2.Helpers.ReceiptReferenceProvider receiptReferenceProvider, VoidValidator voidValidator)
        {
            RuleFor(x => x)
                .MustAsync(async (request, _) =>
                {
                    var previousRef = request.cbPreviousReceiptReference!.SingleValue!;
                    var original = await receiptReferenceProvider.LoadOriginalReceiptAsync(previousRef);
                    if (original == null)
                        return true;
                    var error = await voidValidator.ValidateVoidAsync(request, original, previousRef);
                    return error == null;
                })
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) && x.cbPreviousReceiptReference != null)
                .WithMessage("Void receipt items do not match the original receipt.")
                .WithErrorCode("VoidItemsMismatch");
        }
    }
}
