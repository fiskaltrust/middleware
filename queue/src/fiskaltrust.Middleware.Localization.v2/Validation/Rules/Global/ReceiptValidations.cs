using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;

public class ReceiptValidations : AbstractValidator<ReceiptRequest>
{
    public ReceiptValidations(ReceiptReferenceProvider receiptReferenceProvider)
    {
        // Sync rules
        Include(new MandatoryCollections());
        Include(new CurrencyMustBeEur());
        Include(new ChargeItemsAmountSum());
        Include(new ReceiptBalance());
        Include(new RefundReference());
        Include(new PaymentTransferReference());

        // Async rules (need DB lookups)
        Include(new PreviousReceiptMustNotBeVoided(receiptReferenceProvider));
        Include(new VoidMustNotAlreadyExist(receiptReferenceProvider));
        Include(new FullRefundMustMatchOriginal(receiptReferenceProvider));
        Include(new PartialRefundMustMatchOriginal(receiptReferenceProvider));
        Include(new VoidMustMatchOriginal(receiptReferenceProvider));
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

    public class PreviousReceiptMustNotBeVoided : AbstractValidator<ReceiptRequest>
    {
        public PreviousReceiptMustNotBeVoided(ReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .MustAsync(async (previousRef, _) =>
                    !await receiptReferenceProvider.HasExistingVoidAsync(previousRef!.SingleValue))
                .When(x => x.cbPreviousReceiptReference != null
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

    public class FullRefundMustMatchOriginal : AbstractValidator<ReceiptRequest>
    {
        public FullRefundMustMatchOriginal(ReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x)
                .MustAsync(async (request, _) =>
                {
                    var original = await receiptReferenceProvider.LoadOriginalReceiptAsync(request.cbPreviousReceiptReference!.SingleValue);
                    if (original == null) return false;

                    if (request.cbChargeItems == null || original.cbChargeItems == null) return false;
                    if (request.cbChargeItems.Count != original.cbChargeItems.Count) return false;
                    if (request.cbPayItems == null || original.cbPayItems == null) return false;
                    if (request.cbPayItems.Count != original.cbPayItems.Count) return false;

                    if (ReceiptComparisonHelper.CompareReceiptRequest(request, original) != null) return false;

                    for (int i = 0; i < request.cbChargeItems.Count; i++)
                    {
                        if (ReceiptComparisonHelper.CompareChargeItems(request.cbChargeItems[i], original.cbChargeItems[i]) != null)
                            return false;
                    }

                    for (int i = 0; i < request.cbPayItems.Count; i++)
                    {
                        if (ReceiptComparisonHelper.ComparePayItems(request.cbPayItems[i], original.cbPayItems[i]) != null)
                            return false;
                    }

                    return true;
                })
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund)
                    && !x.IsPartialRefundReceipt()
                    && x.cbPreviousReceiptReference != null)
                .WithMessage("Full refund items do not match the original receipt.")
                .WithErrorCode("FullRefundItemsMismatch");
        }
    }

    public class PartialRefundMustMatchOriginal : AbstractValidator<ReceiptRequest>
    {
        public PartialRefundMustMatchOriginal(ReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x)
                .MustAsync(async (request, _) =>
                {
                    var previousRef = request.cbPreviousReceiptReference!.SingleValue;
                    var original = await receiptReferenceProvider.LoadOriginalReceiptAsync(previousRef);
                    if (original == null || original.cbChargeItems == null || request.cbChargeItems == null) return false;

                    if (ReceiptComparisonHelper.CompareReceiptRequest(request, original) != null) return false;

                    var existingRefunds = await receiptReferenceProvider.LoadExistingPartialRefundsAsync(previousRef, request.cbReceiptReference);
                    var chargeItemsAvailable = new List<ChargeItem>(original.cbChargeItems);

                    foreach (var existingRefund in existingRefunds.SelectMany(x => x.cbChargeItems))
                    {
                        var matchingItem = chargeItemsAvailable.FirstOrDefault(item =>
                            (Math.Abs(item.Amount - Math.Abs(existingRefund.Amount)) < 0.01m) &&
                            item.Description == existingRefund.Description &&
                            (Math.Abs(item.VATRate - existingRefund.VATRate) < 0.01m));
                        if (matchingItem != null)
                        {
                            chargeItemsAvailable.Remove(matchingItem);
                        }
                    }

                    for (int i = 0; i < request.cbChargeItems.Count; i++)
                    {
                        var refundItem = request.cbChargeItems[i];
                        var matchingItem = chargeItemsAvailable.FirstOrDefault(item =>
                            (Math.Abs(item.Amount - Math.Abs(refundItem.Amount)) < 0.01m) &&
                            item.Description == refundItem.Description &&
                            (Math.Abs(item.VATRate - refundItem.VATRate) < 0.01m));
                        if (matchingItem == null) return false;

                        if (ReceiptComparisonHelper.CompareChargeItems(refundItem, matchingItem) != null)
                            return false;
                    }

                    return true;
                })
                .When(x => x.IsPartialRefundReceipt()
                    && x.cbPreviousReceiptReference != null
                    && x.cbChargeItems != null)
                .WithMessage("Partial refund items do not match the available items from the original receipt.")
                .WithErrorCode("PartialRefundItemsMismatch");
        }
    }

    public class VoidMustMatchOriginal : AbstractValidator<ReceiptRequest>
    {
        public VoidMustMatchOriginal(ReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x)
                .MustAsync(async (request, _) =>
                {
                    var original = await receiptReferenceProvider.LoadOriginalReceiptAsync(request.cbPreviousReceiptReference!.SingleValue);
                    if (original == null) return false;

                    if (request.cbChargeItems == null || original.cbChargeItems == null) return false;
                    if (request.cbChargeItems.Count != original.cbChargeItems.Count) return false;
                    if (request.cbPayItems == null || original.cbPayItems == null) return false;
                    if (request.cbPayItems.Count != original.cbPayItems.Count) return false;

                    if (ReceiptComparisonHelper.CompareReceiptRequest(request, original) != null) return false;

                    for (int i = 0; i < request.cbChargeItems.Count; i++)
                    {
                        if (ReceiptComparisonHelper.CompareChargeItems(request.cbChargeItems[i], original.cbChargeItems[i]) != null)
                            return false;
                    }

                    for (int i = 0; i < request.cbPayItems.Count; i++)
                    {
                        if (ReceiptComparisonHelper.ComparePayItems(request.cbPayItems[i], original.cbPayItems[i]) != null)
                            return false;
                    }

                    return true;
                })
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) && x.cbPreviousReceiptReference != null)
                .WithMessage("Void receipt items do not match the original receipt.")
                .WithErrorCode("VoidItemsMismatch");
        }
    }
}
