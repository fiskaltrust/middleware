using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using ReceiptCaseFlags = fiskaltrust.ifPOS.v2.Cases.ReceiptCaseFlags;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Validation;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.QueuePT.ValidationFV.Rules;

public class ReceiptValidations : AbstractValidator<ReceiptRequest>
{
    public ReceiptValidations(ReceiptReferenceProvider receiptReferenceProvider, ReceiptResponse? response = null)
    {
        // Guards (match old ReceiptValidator early exits)
        Include(new CurrencyMustBeEur());
        Include(new TrainingModeNotSupported());
        Include(new ChargeItemsMustNotBeNull());
        Include(new PayItemsMustNotBeNull());

        Include(new RefundMustHavePreviousReference());
        Include(new RefundMustNotAlreadyExist(receiptReferenceProvider));
        Include(new PartialRefundMustNotContainNonRefundItems());
        Include(new MixedRefundPayItemsNotAllowed());
        Include(new HandwrittenMustNotBeRefundOrVoid());
        Include(new HandwrittenMustHaveSeriesAndNumber());
        Include(new HandwrittenReceiptOnlyForInvoices());
        Include(new HandwrittenSeriesInvalidCharacter());

        Include(new PaymentTransferMustHaveAccountReceivableItem());
        Include(new PaymentTransferMustNotAlreadyExist(receiptReferenceProvider));
        Include(new PaymentTransferOriginalMustBeInvoice(receiptReferenceProvider));
        Include(new PaymentTransferAmountsMustMatch(receiptReferenceProvider));

        // Non-handwritten rules
        Include(new ReceiptMustBeBalanced());
        Include(new PositionsMustBeSequential());
        Include(new ReceiptMomentMustBeUtc());
        Include(new ReceiptMomentTimeDifference(response));

        When(_ => false, () =>
        {
            Include(new ReceiptMomentMustNotBeInFuture());
            Include(new ReceiptMomentDeviationLimit());
        });
    }

    public class RefundMustHavePreviousReference : AbstractValidator<ReceiptRequest>
    {
        public RefundMustHavePreviousReference()
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .NotNull()
                .When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten)
                        && x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund)
                        && !x.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002))
                .WithMessage("Refund receipt must reference the original receipt.")
                .WithErrorCode("RefundMissingPreviousReceiptReference");
        }
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

    public class MixedRefundPayItemsNotAllowed : AbstractValidator<ReceiptRequest>
    {
        public MixedRefundPayItemsNotAllowed()
        {
            RuleFor(x => x.cbPayItems)
                .Must(payItems => !payItems!.Any(item => !item.IsRefund()))
                .When(x => x.IsPartialRefundReceipt() && x.cbPayItems != null)
                .WithMessage("Partial refund contains mixed refund and non-refund pay items. All pay items must have the refund flag set for partial refunds.")
                .WithErrorCode("PartialRefundMixedPayItems");
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
                    return data?.PT != null
                        && !string.IsNullOrWhiteSpace(data.PT.Series)
                        && data.PT.Number.HasValue
                        && data.PT.Number.Value >= 1;
                })
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
                .WithMessage("When using Handwritten flag, ftReceiptCaseData must contain PT.Series and PT.Number (Number must be >= 1).")
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
                    var previousRef = request.cbPreviousReceiptReference!.SingleValue;
                    var original = await receiptReferenceProvider.LoadOriginalReceiptAsync(previousRef);
                    if (original == null || original.cbPayItems == null || request.cbPayItems == null)
                        return false;

                    var originalAccountsReceivable = original.cbPayItems
                        .Where(x => x.ftPayItemCase.Case() == PayItemCase.AccountsReceivable)
                        .Sum(x => x.Amount);

                    var existingPartialRefunds = await receiptReferenceProvider.LoadExistingPartialRefundsAsync(previousRef, request.cbReceiptReference);
                    var alreadyRefundedAmount = existingPartialRefunds
                        .SelectMany(r => r.cbPayItems ?? [])
                        .Sum(x => Math.Abs(x.Amount));

                    var remainingAmount = originalAccountsReceivable - alreadyRefundedAmount;
                    var transferTotal = request.cbPayItems.Sum(x => x.Amount);
                    return transferTotal <= remainingAmount + 0.01m;
                })
                .When(x => x.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002) && x.cbPreviousReceiptReference != null)
                .WithMessage("The total amount of pay items in the payment transfer exceeds the remaining AccountsReceivable amount of the original invoice.")
                .WithErrorCode("PaymentTransferAmountsMismatch");
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
                    if (receiptMomentUtc > serverTime) return true;
                    var timeDifference = Math.Abs((receiptMomentUtc - serverTime).TotalMinutes);
                    return timeDifference <= MaxAllowedDifferenceMinutes;
                })
                .When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
                .WithMessage($"cbReceiptMoment deviates more than {MaxAllowedDifferenceMinutes} minutes from server time.")
                .WithErrorCode("ReceiptMomentDeviationExceeded");
        }
    }

    public class ReceiptMomentMustBeUtc : AbstractValidator<ReceiptRequest>
    {
        public ReceiptMomentMustBeUtc()
        {
            RuleFor(x => x.cbReceiptMoment)
                .Must(moment => moment.Kind == DateTimeKind.Utc)
                .When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
                .WithMessage("cbReceiptMoment must be in UTC format.")
                .WithErrorCode("ReceiptMomentNotUtc");
        }
    }

    public class ReceiptMomentTimeDifference : AbstractValidator<ReceiptRequest>
    {
        private const double MaxAllowedDifferenceMinutes = 1.0;
        private readonly ReceiptResponse? _response;

        public ReceiptMomentTimeDifference(ReceiptResponse? response = null)
        {
            _response = response;
            RuleFor(x => x.cbReceiptMoment)
                .Must(cbReceiptMoment =>
                {
                    var serverMoment = _response?.ftReceiptMoment ?? DateTime.UtcNow;
                    var timeDifference = (serverMoment - cbReceiptMoment).Duration();
                    return timeDifference <= TimeSpan.FromMinutes(MaxAllowedDifferenceMinutes);
                })
                .When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
                .WithMessage(request =>
                    $"The time difference between cbReceiptMoment ({request.cbReceiptMoment:yyyy-MM-dd HH:mm:ss}) and server time exceeds the maximum allowed difference of {MaxAllowedDifferenceMinutes} minute(s).")
                .WithErrorCode("EEEE_ReceiptMomentTimeDifferenceExceeded");
        }
    }

    public class CurrencyMustBeEur : AbstractValidator<ReceiptRequest>
    {
        public CurrencyMustBeEur()
        {
            RuleFor(x => x.Currency)
                .Must(currency => currency == Currency.EUR)
                .WithMessage("Only EUR currency is supported.")
                .WithErrorCode("OnlyEuroCurrencySupported");
        }
    }

    public class TrainingModeNotSupported : AbstractValidator<ReceiptRequest>
    {
        public TrainingModeNotSupported()
        {
            RuleFor(x => x)
                .Must(request => !request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
                .WithMessage("Training mode is not supported.")
                .WithErrorCode("TrainingModeNotSupported");
        }
    }

    public class ChargeItemsMustNotBeNull : AbstractValidator<ReceiptRequest>
    {
        public ChargeItemsMustNotBeNull()
        {
            RuleFor(x => x.cbChargeItems)
                .NotNull()
                .WithMessage("cbChargeItems must not be null.")
                .WithErrorCode("ChargeItemsMissing");
        }
    }

    public class PayItemsMustNotBeNull : AbstractValidator<ReceiptRequest>
    {
        public PayItemsMustNotBeNull()
        {
            RuleFor(x => x.cbPayItems)
                .NotNull()
                .WithMessage("cbPayItems must not be null.")
                .WithErrorCode("PayItemsMissing");
        }
    }

    public class ReceiptMustBeBalanced : AbstractValidator<ReceiptRequest>
    {
        private const decimal RoundingTolerance = 0.01m;

        public ReceiptMustBeBalanced()
        {
            RuleFor(x => x)
                .Must(request =>
                {
                    var chargeItemsSum = request.cbChargeItems?.Sum(ci => ci.Amount) ?? 0m;
                    var payItemsSum = request.cbPayItems?.Sum(pi => pi.Amount) ?? 0m;
                    return Math.Abs(chargeItemsSum - payItemsSum) <= RoundingTolerance;
                })
                .When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten)
                        && !x.ftReceiptCase.IsCase((ReceiptCase)0x0007)
                        && !x.ftReceiptCase.IsCase((ReceiptCase)0x0006))
                .WithMessage(request =>
                {
                    var chargeItemsSum = request.cbChargeItems?.Sum(ci => ci.Amount) ?? 0m;
                    var payItemsSum = request.cbPayItems?.Sum(pi => pi.Amount) ?? 0m;
                    return $"Receipt is not balanced: chargeItems={chargeItemsSum:F2}, payItems={payItemsSum:F2}, difference={Math.Abs(chargeItemsSum - payItemsSum):F2}.";
                })
                .WithErrorCode("ReceiptNotBalanced");
        }
    }

    public class PositionsMustBeSequential : AbstractValidator<ReceiptRequest>
    {
        public PositionsMustBeSequential()
        {
            RuleFor(x => x.cbChargeItems)
                .Must(items => ArePositionsSequential(items!.Select(i => i.Position).ToList()))
                .When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten)
                        && x.cbChargeItems != null && x.cbChargeItems.Count > 0
                        && x.cbChargeItems.Any(i => i.Position > 0))
                .WithMessage("cbChargeItems positions must start at 1 and be strictly sequential without gaps.")
                .WithErrorCode("InvalidChargeItemPositions");

            RuleFor(x => x.cbPayItems)
                .Must(items => ArePositionsSequential(items!.Select(i => i.Position).ToList()))
                .When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten)
                        && x.cbPayItems != null && x.cbPayItems.Count > 0
                        && x.cbPayItems.Any(i => i.Position > 0))
                .WithMessage("cbPayItems positions must start at 1 and be strictly sequential without gaps.")
                .WithErrorCode("InvalidPayItemPositions");
        }

        private static bool ArePositionsSequential(List<decimal> positions)
        {
            var expected = 1L;
            foreach (var position in positions)
            {
                if (position != expected)
                    return false;
                expected++;
            }
            return true;
        }
    }

    public class HandwrittenReceiptOnlyForInvoices : AbstractValidator<ReceiptRequest>
    {
        private static readonly ReceiptCase[] _invoiceCases =
        [
            ReceiptCase.InvoiceUnknown0x1000,
            ReceiptCase.InvoiceB2C0x1001,
            ReceiptCase.InvoiceB2B0x1002,
            ReceiptCase.InvoiceB2G0x1003
        ];

        public HandwrittenReceiptOnlyForInvoices()
        {
            RuleFor(x => x.ftReceiptCase)
                .Must(c => _invoiceCases.Contains(c.Case()))
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
                .WithMessage("Handwritten receipts are only allowed for invoice receipt cases.")
                .WithErrorCode("HandwrittenReceiptOnlyForInvoices");
        }
    }

    public class HandwrittenSeriesInvalidCharacter : AbstractValidator<ReceiptRequest>
    {
        public HandwrittenSeriesInvalidCharacter()
        {
            RuleFor(x => x)
                .Must(request =>
                {
                    if (!request.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayloadPT>(out var data))
                        return true;
                    if (data?.PT?.Series == null) return true;
                    return !data.PT.Series.Contains(" ");
                })
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
                .WithMessage("Handwritten receipt series must not contain spaces.")
                .WithErrorCode("HandwrittenSeriesInvalidCharacter");
        }
    }
}
