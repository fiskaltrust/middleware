using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.PT;

public class ReceiptValidations : AbstractValidator<ReceiptRequest>
{
    public ReceiptValidations(ReceiptReferenceProvider receiptReferenceProvider)
    {
        Include(new RefundMustNotAlreadyExist(receiptReferenceProvider));
        Include(new VoidMustNotAlreadyExist(receiptReferenceProvider));
    }

    public class RefundMustNotAlreadyExist : AbstractValidator<ReceiptRequest>
    {
        public RefundMustNotAlreadyExist(ReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .MustAsync(async (previousRef, _) =>
                    !await receiptReferenceProvider.HasExistingRefundAsync(previousRef!.SingleValue))
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) && x.cbPreviousReceiptReference != null)
                .WithMessage("A refund for this receipt already exists.")
                .WithErrorCode("RefundAlreadyExists");
        }
    }

    public class VoidMustNotAlreadyExist : AbstractValidator<ReceiptRequest>
    {
        public VoidMustNotAlreadyExist(ReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .MustAsync(async (previousRef, _) =>
                    !await receiptReferenceProvider.HasExistingVoidAsync(previousRef!.SingleValue))
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) && x.cbPreviousReceiptReference != null)
                .WithMessage("A void for this receipt already exists.")
                .WithErrorCode("VoidAlreadyExists");
        }
    }
}
