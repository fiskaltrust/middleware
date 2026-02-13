using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;

public class ReceiptValidations : AbstractValidator<ReceiptRequest>
{
    public ReceiptValidations()
    {
        Include(new MandatoryCollections());
        Include(new ChargeItemsAmountSum());
        Include(new ReceiptBalance());
        Include(new RefundReference());
        Include(new PaymentTransferReference());
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

    public class ChargeItemsAmountSum : AbstractValidator<ReceiptRequest>
    {
        public ChargeItemsAmountSum()
        {
            RuleFor(x => x.cbReceiptAmount)
                .Must((request, receiptAmount) =>
                {
                    if (!receiptAmount.HasValue) return true;
                    if (request.cbChargeItems == null || !request.cbChargeItems.Any()) return true;

                    var sum = request.cbChargeItems.Sum(item => item.Amount);
                    return sum == receiptAmount.Value;
                })
                .WithMessage(request =>
                {
                    var sum = request.cbChargeItems?.Sum(item => item.Amount) ?? 0;
                    return $"Sum of ChargeItem amounts ({sum}) does not match cbReceiptAmount ({request.cbReceiptAmount})";
                })
                .WithErrorCode("ChargeItemsSumMismatch")
                .When(x => x.cbReceiptAmount.HasValue);
        }
    }

    public class ReceiptBalance : AbstractValidator<ReceiptRequest>
    {
        private const decimal RoundingTolerance = 0.01m;

        public ReceiptBalance()
        {
            When(x => !x.ftReceiptCase.IsCase((ReceiptCase) 0x0006)
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
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
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
                .When(x => x.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002))
                .WithMessage("PaymentTransfer receipt must have cbPreviousReceiptReference")
                .WithErrorCode("PaymentTransferMissingPreviousReceiptReference");
        }
    }
}
