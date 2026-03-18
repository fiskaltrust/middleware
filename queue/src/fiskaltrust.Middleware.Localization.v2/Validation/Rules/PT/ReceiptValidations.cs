using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models.Cases.PT;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.PT;

public class ReceiptValidations : AbstractValidator<ReceiptRequest>
{
    public ReceiptValidations(ReceiptReferenceProvider receiptReferenceProvider)
    {
        Include(new RefundMustNotAlreadyExist(receiptReferenceProvider));
        Include(new PartialRefundMustNotContainNonRefundItems());
        Include(new HandwrittenMustNotBeRefundOrVoid());
        Include(new HandwrittenMustHaveSeriesAndNumber());

        Include(new PaymentTransferMustHaveAccountReceivableItem());
        Include(new PaymentTransferMustNotAlreadyExist(receiptReferenceProvider));
        Include(new PaymentTransferOriginalMustBeInvoice(receiptReferenceProvider));
        Include(new PaymentTransferAmountsMustMatch(receiptReferenceProvider));

        Include(new ReceiptMomentMustBeUtc());
        Include(new ReceiptMomentMustNotBeInFuture());
        Include(new ReceiptMomentDeviationLimit());
        Include(new ReceiptMomentTimeDifference());
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

    public class PartialRefundMustNotContainNonRefundItems : AbstractValidator<ReceiptRequest>
    {
        public PartialRefundMustNotContainNonRefundItems()
        {
            RuleFor(x => x.cbChargeItems)
                .Must(chargeItems => !chargeItems!.Any(item => !item.IsRefund()))
                .When(x => x.IsPartialRefundReceipt() && x.cbChargeItems != null)
                .WithMessage("Partial refund contains mixed refund and non-refund items. All charge items must have the refund flag set for partial refunds.")
                .WithErrorCode("PartialRefundMixedItems");
        }
    }

    public class HandwrittenMustNotBeRefundOrVoid : AbstractValidator<ReceiptRequest>
    {
        public HandwrittenMustNotBeRefundOrVoid()
        {
            RuleFor(x => x)
                .Must(request =>
                    !request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund)
                    && !request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void)
                    && !request.IsPartialRefundReceipt())
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
                .WithMessage("Handwritten receipts must not be used in combination with void/refund or other connections.")
                .WithErrorCode("HandwrittenWithRefundOrVoid");
        }
    }

    public class HandwrittenMustHaveSeriesAndNumber : AbstractValidator<ReceiptRequest>
    {
        public HandwrittenMustHaveSeriesAndNumber()
        {
            RuleFor(x => x)
                .Must(request =>
                {
                    if (!request.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayloadPT>(out var data))
                        return false;
                    return data?.PT != null && data.PT.Series != null && data.PT.Number.HasValue;
                })
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
                .WithMessage("When using Handwritten flag, ftReceiptCaseData must contain PT.Series and PT.Number.")
                .WithErrorCode("HandwrittenMissingSeriesOrNumber");
        }
    }

    public class PaymentTransferMustHaveAccountReceivableItem : AbstractValidator<ReceiptRequest>
    {
        public PaymentTransferMustHaveAccountReceivableItem()
        {
            RuleFor(x => x.cbChargeItems)
                .Must(chargeItems => chargeItems != null && chargeItems.Any(ci => ci.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Receivable)))
                .When(x => x.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002))
                .WithMessage("PaymentTransfer receipt must contain at least one AccountReceivable charge item.")
                .WithErrorCode("PaymentTransferMissingReceivableItem");
        }
    }

    public class PaymentTransferMustNotAlreadyExist : AbstractValidator<ReceiptRequest>
    {
        public PaymentTransferMustNotAlreadyExist(ReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .MustAsync(async (previousRef, _) =>
                    !await receiptReferenceProvider.HasExistingPaymentTransferAsync(previousRef!.SingleValue))
                .When(x => x.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002) && x.cbPreviousReceiptReference != null)
                .WithMessage("A payment transfer for this receipt already exists.")
                .WithErrorCode("PaymentTransferAlreadyExists");
        }
    }

    public class PaymentTransferOriginalMustBeInvoice : AbstractValidator<ReceiptRequest>
    {
        private static readonly ReceiptCase[] _invoiceCases =
        [
            ReceiptCase.InvoiceUnknown0x1000,
            ReceiptCase.InvoiceB2C0x1001,
            ReceiptCase.InvoiceB2B0x1002,
            ReceiptCase.InvoiceB2G0x1003
        ];

        public PaymentTransferOriginalMustBeInvoice(ReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x)
                .MustAsync(async (request, _) =>
                {
                    var original = await receiptReferenceProvider.LoadOriginalReceiptAsync(request.cbPreviousReceiptReference!.SingleValue);
                    return original != null && _invoiceCases.Contains(original.ftReceiptCase.Case());
                })
                .When(x => x.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002) && x.cbPreviousReceiptReference != null)
                .WithMessage("The original receipt is not a valid receipt for payment transfer. Only Invoices are allowed.")
                .WithErrorCode("PaymentTransferOriginalNotInvoice");
        }
    }

    public class PaymentTransferAmountsMustMatch : AbstractValidator<ReceiptRequest>
    {
        public PaymentTransferAmountsMustMatch(ReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x)
                .MustAsync(async (request, _) =>
                {
                    var original = await receiptReferenceProvider.LoadOriginalReceiptAsync(request.cbPreviousReceiptReference!.SingleValue);
                    if (original == null || original.cbPayItems == null || request.cbPayItems == null)
                        return false;

                    var originalAccountsReceivable = original.cbPayItems
                        .Where(x => x.ftPayItemCase.Case() == PayItemCase.AccountsReceivable)
                        .Sum(x => x.Amount);
                    var transferTotal = request.cbPayItems.Sum(x => x.Amount);
                    return originalAccountsReceivable == transferTotal;
                })
                .When(x => x.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002) && x.cbPreviousReceiptReference != null)
                .WithMessage("The total amount of pay items in the payment transfer must match the AccountsReceivable amount of the original invoice.")
                .WithErrorCode("PaymentTransferAmountsMismatch");
        }
    }

    public class ReceiptMomentMustBeUtc : AbstractValidator<ReceiptRequest>
    {
        public ReceiptMomentMustBeUtc()
        {
            RuleFor(x => x.cbReceiptMoment)
                .Must(moment => moment.Kind == DateTimeKind.Utc)
                .WithMessage("cbReceiptMoment must be in UTC format.")
                .WithErrorCode("ReceiptMomentNotUtc");
        }
    }

    public class ReceiptMomentMustNotBeInFuture : AbstractValidator<ReceiptRequest>
    {
        public ReceiptMomentMustNotBeInFuture()
        {
            RuleFor(x => x.cbReceiptMoment)
                .Must(moment => moment.ToUniversalTime() <= DateTime.UtcNow)
                .WithMessage("cbReceiptMoment must not be in the future.")
                .WithErrorCode("ReceiptMomentInFuture");
        }
    }

    public class ReceiptMomentDeviationLimit : AbstractValidator<ReceiptRequest>
    {
        private const double MaxAllowedDifferenceMinutes = 10.0;

        public ReceiptMomentDeviationLimit()
        {
            RuleFor(x => x.cbReceiptMoment)
                .Must(moment =>
                {
                    var receiptMomentUtc = moment.ToUniversalTime();
                    var serverTime = DateTime.UtcNow;
                    if (receiptMomentUtc > serverTime) return true; // handled by ReceiptMomentMustNotBeInFuture
                    var timeDifference = Math.Abs((receiptMomentUtc - serverTime).TotalMinutes);
                    return timeDifference <= MaxAllowedDifferenceMinutes;
                })
                .When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
                .WithMessage($"cbReceiptMoment deviates more than {MaxAllowedDifferenceMinutes} minutes from server time.")
                .WithErrorCode("ReceiptMomentDeviationExceeded");
        }
    }

    public class ReceiptMomentTimeDifference : AbstractValidator<ReceiptRequest>
    {
        private const double MaxAllowedDifferenceMinutes = 1.0;

        public ReceiptMomentTimeDifference()
        {
            RuleFor(x => x.cbReceiptMoment)
                .Must(cbReceiptMoment =>
                {
                    var timeDifference = (DateTime.UtcNow - cbReceiptMoment).Duration();
                    return timeDifference <= TimeSpan.FromMinutes(MaxAllowedDifferenceMinutes);
                })
                .When(x => x.cbReceiptMoment.Kind == DateTimeKind.Utc)
                .WithMessage(request =>
                    $"The time difference between cbReceiptMoment ({request.cbReceiptMoment:yyyy-MM-dd HH:mm:ss}) and server time exceeds the maximum allowed difference of {MaxAllowedDifferenceMinutes} minute(s).")
                .WithErrorCode("EEEE_ReceiptMomentTimeDifferenceExceeded");
        }
    }
}
