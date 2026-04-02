using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;

public class ReceiptValidations : AbstractValidator<ReceiptRequest>
{
    public ReceiptValidations(ReceiptReferenceProvider receiptReferenceProvider, ftQueue? queue = null)
    {
        Include(new MandatoryCollections());
        Include(new CurrencyMustBeEur());
        Include(new ReceiptBalance());
        Include(new RefundReference());
        Include(new PaymentTransferReference());

        Include(new PreviousReceiptMustNotBeVoided(receiptReferenceProvider));
        Include(new VoidMustNotAlreadyExist(receiptReferenceProvider));

        Include(new RefundMustUseSingleReference());
        Include(new PartialRefundMustUseSingleReference());
        Include(new VoidMustUseSingleReference());

        Include(new CountryConsistency(queue));
        Include(new PayItemCaseCountryConsistency(queue));
    }

    public class MandatoryCollections : AbstractValidator<ReceiptRequest>
    {
        public MandatoryCollections()
        {
            RuleFor(x => x.cbChargeItems)
                .NotNull()
                .WithMessage("cbChargeItems must not be null")
                .WithErrorCode("ChargeItemsMissing");

            RuleFor(x => x.cbPayItems)
                .NotNull()
                .WithMessage("cbPayItems must not be null")
                .WithErrorCode("PayItemsMissing");
        }
    }

    public class CurrencyMustBeEur : AbstractValidator<ReceiptRequest>
    {
        public CurrencyMustBeEur()
        {
            RuleFor(x => x.Currency)
                .Equal(Currency.EUR)
                .WithMessage(request => $"Only EUR currency is supported, but received '{request.Currency}'.")
                .WithErrorCode("OnlyEuroCurrencySupported");
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
                    .WithErrorCode("ReceiptNotBalanced");
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
                .WithMessage("Refund receipt must have cbPreviousReceiptReference")
                .WithErrorCode("RefundMissingPreviousReceiptReference");
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
                .WithMessage("PaymentTransfer receipt must have cbPreviousReceiptReference")
                .WithErrorCode("PaymentTransferMissingPreviousReceiptReference");
        }
    }

    public class PreviousReceiptMustNotBeVoided : AbstractValidator<ReceiptRequest>
    {
        public PreviousReceiptMustNotBeVoided(ReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .MustAsync(async (previousRef, _) =>
                {
                    if (previousRef!.IsSingle)
                        return !await receiptReferenceProvider.HasExistingVoidAsync(previousRef.SingleValue!);
                    foreach (var reference in previousRef.GroupValue)
                        if (await receiptReferenceProvider.HasExistingVoidAsync(reference))
                            return false;
                    return true;
                })
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
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) && x.cbPreviousReceiptReference != null)
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
                .WithErrorCode("RefundGroupReferenceNotSupported");
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
                .WithErrorCode("PartialRefundGroupReferenceNotSupported");
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
                .WithErrorCode("VoidGroupReferenceNotSupported");
        }
    }

    public class CountryConsistency : AbstractValidator<ReceiptRequest>
    {
        public CountryConsistency(ftQueue? queue)
        {
            When(_ => queue != null && !string.IsNullOrEmpty(queue.CountryCode), () =>
            {
                RuleFor(x => x)
                    .Must(request => request.ftReceiptCase.Country() == queue!.CountryCode)
                    .WithMessage(request =>
                        $"Receipt case country '{request.ftReceiptCase.Country()}' does not match queue country '{queue!.CountryCode}'.")
                    .WithErrorCode("ReceiptCaseCountryMismatch");
            });
        }
    }

    public class PayItemCaseCountryConsistency : AbstractValidator<ReceiptRequest>
    {
        public PayItemCaseCountryConsistency(ftQueue? queue)
        {
            When(x => queue != null && !string.IsNullOrEmpty(queue.CountryCode)
                   && x.cbPayItems != null, () =>
            {
                RuleForEach(x => x.cbPayItems).ChildRules(payItem =>
                {
                    payItem.RuleFor(x => x.ftPayItemCase)
                        .Must(c => c.Country() == queue!.CountryCode)
                        .WithMessage(item => $"Pay item case country '{item.ftPayItemCase.Country()}' does not match queue country '{queue!.CountryCode}'.")
                        .WithErrorCode("PayItemCaseCountryMismatch");
                });
            });
        }
    }
}
