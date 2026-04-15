using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using FluentValidation;
using fiskaltrust.Middleware.Localization.v2.Validation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;

public class ReceiptValidations : AbstractValidator<ReceiptRequest>
{
    public ReceiptValidations(ReceiptReferenceProvider receiptReferenceProvider, ftQueue? queue = null)
    {
        Include(new MandatoryCollections());
        Include(new ReceiptBalance());
        Include(new RefundReference());
        Include(new PaymentTransferReference());

        Include(new PreviousReceiptMustNotBeVoided(receiptReferenceProvider));
        Include(new VoidMustNotAlreadyExist(receiptReferenceProvider));

        Include(new RefundMustUseSingleReference());
        Include(new PartialRefundMustUseSingleReference());
        Include(new VoidMustUseSingleReference());

        Include(new CountryConsistency(queue));
    }

    public class MandatoryCollections : AbstractValidator<ReceiptRequest>
    {
        public MandatoryCollections()
        {
            RuleFor(x => x.cbChargeItems)
                .NotNull()
                .WithMessage("cbChargeItems must not be null.")
                .WithErrorCode("ChargeItemsMissing")
                .WithState(_ => new ValidationHelp("Always send cbChargeItems. Use [] if empty."));

            RuleFor(x => x.cbPayItems)
                .NotNull()
                .WithMessage("cbPayItems must not be null.")
                .WithErrorCode("PayItemsMissing")
                .WithState(_ => new ValidationHelp("Always send cbPayItems. Use [] if empty."));
        }
    }

    public class ReceiptBalance : AbstractValidator<ReceiptRequest>
    {
        private const decimal RoundingTolerance = 0.01m;

        public ReceiptBalance()
        {
            When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten)
                   && !x.ftReceiptCase.IsCase((ReceiptCase) 0x0006)
                   && !x.ftReceiptCase.IsCase((ReceiptCase) 0x0007), () =>
            {
                RuleFor(x => x)
                    .Must(request =>
                    {
                        var chargeItemsSum = request.cbChargeItems?.Sum(chargeItem => chargeItem.Amount) ?? 0m;
                        var payItemsSum = request.cbPayItems?.Sum(payItem => payItem.Amount) ?? 0m;
                        return Math.Abs(chargeItemsSum - payItemsSum) <= RoundingTolerance;
                    })
                    .WithMessage(request =>
                    {
                        var chargeItemsSum = request.cbChargeItems?.Sum(chargeItem => chargeItem.Amount) ?? 0m;
                        var payItemsSum = request.cbPayItems?.Sum(payItem => payItem.Amount) ?? 0m;
                        var difference = Math.Abs(chargeItemsSum - payItemsSum);
                        return $"Receipt is not balanced: charge items sum ({chargeItemsSum:F2}) does not match pay items sum ({payItemsSum:F2}), difference: {difference:F2}";
                    })
                    .WithErrorCode("ReceiptNotBalanced")
                    .WithState(_ => new ValidationHelp("The sum of cbChargeItems.Amount must equal the sum of cbPayItems.Amount (tolerance 0.01€)."));
            });
        }
    }

    public class RefundReference : AbstractValidator<ReceiptRequest>
    {
        public RefundReference()
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .NotNull()
                .When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten)
                        && x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
                .WithMessage("Refund receipt must have cbPreviousReceiptReference.")
                .WithErrorCode("RefundMissingPreviousReceiptReference")
                .WithState(_ => new ValidationHelp("Set cbPreviousReceiptReference to the original receipt's cbReceiptReference."));
        }
    }

    public class PaymentTransferReference : AbstractValidator<ReceiptRequest>
    {
        public PaymentTransferReference()
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .NotNull()
                .When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten)
                        && x.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002))
                .WithMessage("PaymentTransfer receipt must have cbPreviousReceiptReference.")
                .WithErrorCode("PaymentTransferMissingPreviousReceiptReference")
                .WithState(_ => new ValidationHelp("Set cbPreviousReceiptReference to the original invoice's cbReceiptReference."));
        }
    }

    public class PreviousReceiptMustNotBeVoided : AbstractValidator<ReceiptRequest>
    {
        public PreviousReceiptMustNotBeVoided(ReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .MustAsync((previousRef, _) => previousRef!.MatchAsync(
                    async single => !await receiptReferenceProvider.HasExistingVoidAsync(single),
                    async group =>
                    {
                        foreach (var reference in group)
                        {
                            if (await receiptReferenceProvider.HasExistingVoidAsync(reference))
                            { return false; }
                        }
                        return true;
                    }
                ))
                .When(x => x.cbPreviousReceiptReference != null
                    && !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten)
                    && !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
                .WithMessage("The referenced receipt has already been voided.")
                .WithErrorCode("PreviousReceiptIsVoided");
        }
    }

    public class VoidMustNotAlreadyExist : AbstractValidator<ReceiptRequest>
    {
        public VoidMustNotAlreadyExist(ReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .MustAsync(async (previousRef, _) =>
                    !await receiptReferenceProvider.HasExistingVoidAsync(previousRef!.SingleValue))
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) && x.cbPreviousReceiptReference != null && x.cbPreviousReceiptReference.IsSingle)
                .WithMessage("A void for this receipt already exists.")
                .WithErrorCode("VoidAlreadyExists");
        }
    }

    public class RefundMustUseSingleReference : AbstractValidator<ReceiptRequest>
    {
        public RefundMustUseSingleReference()
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .Must(pref => !pref!.IsGroup)
                .When(x => x.ftReceiptCase.Country() != "GR"
                    && x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund)
                    && x.cbPreviousReceiptReference != null)
                .WithMessage("Refunding a receipt is only supported with single references.")
                .WithErrorCode("RefundGroupReferenceNotSupported")
                .WithState(_ => new ValidationHelp("Set cbPreviousReceiptReference to a single string, not an array."));
        }
    }

    public class PartialRefundMustUseSingleReference : AbstractValidator<ReceiptRequest>
    {
        public PartialRefundMustUseSingleReference()
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .Must(pref => !pref!.IsGroup)
                .When(x => x.ftReceiptCase.Country() != "GR"
                    && x.IsPartialRefundReceipt()
                    && x.cbPreviousReceiptReference != null)
                .WithMessage("Partial refunding a receipt is only supported with single references.")
                .WithErrorCode("PartialRefundGroupReferenceNotSupported")
                .WithState(_ => new ValidationHelp("Set cbPreviousReceiptReference to a single string, not an array."));
        }
    }

    public class VoidMustUseSingleReference : AbstractValidator<ReceiptRequest>
    {
        public VoidMustUseSingleReference()
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .Must(pref => !pref!.IsGroup)
                .When(x => x.ftReceiptCase.Country() != "GR"
                    && x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void)
                    && x.cbPreviousReceiptReference != null)
                .WithMessage("Voiding a receipt is only supported with single references.")
                .WithErrorCode("VoidGroupReferenceNotSupported")
                .WithState(_ => new ValidationHelp("Set cbPreviousReceiptReference to a single string, not an array."));
        }
    }

    public class CountryConsistency : AbstractValidator<ReceiptRequest>
    {
        public CountryConsistency(ftQueue? queue)
        {
            When(_ => queue != null, () =>
            {
                RuleFor(x => x)
                    .Must(request => request.ftReceiptCase.Country() == queue!.CountryCode)
                    .WithMessage(request =>
                        $"Receipt case country '{request.ftReceiptCase.Country()}' does not match queue country '{queue!.CountryCode}'.")
                    .WithErrorCode("ReceiptCaseCountryMismatch")
                    .WithState(_ => new ValidationHelp("Use ftReceiptCase values that match the country code configured for this queue."));
            });
        }
    }
}
