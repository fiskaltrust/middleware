using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using ReceiptCaseFlags = fiskaltrust.ifPOS.v2.Cases.ReceiptCaseFlags;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Validation;
using FluentValidation;
using Currency = fiskaltrust.ifPOS.v2.Currency;

namespace fiskaltrust.Middleware.Localization.QueuePT.ValidationFV.Rules;

public class ReceiptValidations : AbstractValidator<ReceiptRequest>
{
    public ReceiptValidations(
        FVReceiptReferenceProvider receiptReferenceProvider,
        DocumentStatusProvider documentStatusProvider,
        VoidValidator voidValidator,
        RefundValidator refundValidator,
        ReceiptResponse? response = null,
        object? numberSeries = null)
    {
        // DEMO: always fail in FV to show shadow mismatch 
        RuleFor(x => x.cbReceiptReference)
            .Must(_ => false)
            .WithMessage("Shadow validation demo — FV intentionally failing.")
            .WithErrorCode("DEMO_ShadowMismatch");

        Include(new TrainingModeNotSupported());
        Include(new ReceiptReferenceAlreadyUsed(receiptReferenceProvider));

        Include(new RefundMustHavePreviousReference());
        Include(new RefundMustNotAlreadyExist(receiptReferenceProvider));
        Include(new PartialRefundMustNotContainNonRefundItems());
        Include(new MixedRefundPayItemsNotAllowed());
        Include(new PartialRefundPreviousReceiptMustNotBeVoided(receiptReferenceProvider));
        Include(new PartialRefundMustNotHaveExistingRefund(receiptReferenceProvider));
        Include(new HandwrittenMustNotBeRefundOrVoid());
        Include(new HandwrittenMustHaveSeriesAndNumber());
        Include(new HandwrittenReceiptOnlyForInvoices());
        Include(new HandwrittenSeriesInvalidCharacter());
        Include(new HandwrittenSeriesNumberAlreadyLinked(receiptReferenceProvider));
        Include(new PreviousReceiptMustNotBeVoided(receiptReferenceProvider));

        Include(new PaymentTransferMustHaveAccountReceivableItem());
        Include(new PaymentTransferMustNotAlreadyExist(receiptReferenceProvider));
        Include(new PaymentTransferOriginalMustBeInvoice(receiptReferenceProvider));
        Include(new PaymentTransferAmountsMustMatch(receiptReferenceProvider));

        Include(new VoidDocumentStatusCheck(receiptReferenceProvider, documentStatusProvider));
        Include(new VoidFieldsMatch(receiptReferenceProvider, voidValidator));
        Include(new FullRefundFieldsMatch(receiptReferenceProvider, refundValidator));
        Include(new PartialRefundFieldsMatch(receiptReferenceProvider, refundValidator));
        Include(new PaymentTransferForRefundedReceipt(receiptReferenceProvider));
        Include(new PaymentTransferCustomerMismatch(receiptReferenceProvider));
        Include(new PaymentTransferOnlyOneReference());
        Include(new PreviousReceiptLineItemsMatch(receiptReferenceProvider));

        // Non-handwritten rules
        Include(new PositionsMustBeSequential());
        Include(new ReceiptMomentMustBeUtc());
        Include(new ReceiptMomentTimeDifference(response));

        When(_ => numberSeries != null, () =>
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
        public RefundMustNotAlreadyExist(FVReceiptReferenceProvider receiptReferenceProvider)
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
        public PaymentTransferMustNotAlreadyExist(FVReceiptReferenceProvider receiptReferenceProvider)
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

        public PaymentTransferOriginalMustBeInvoice(FVReceiptReferenceProvider receiptReferenceProvider)
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
        public PaymentTransferAmountsMustMatch(FVReceiptReferenceProvider receiptReferenceProvider)
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
                .WithErrorCode("ReceiptMomentTimeDifferenceExceeded");
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

    public class ReceiptReferenceAlreadyUsed : AbstractValidator<ReceiptRequest>
    {
        public ReceiptReferenceAlreadyUsed(FVReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x.cbReceiptReference)
                .MustAsync(async (cbReceiptReference, _) =>
                    !await receiptReferenceProvider.HasExistingSuccessfulReceiptReferenceAsync(cbReceiptReference))
                .WithMessage(request => $"Receipt reference '{request.cbReceiptReference}' has already been used in a successful receipt.")
                .WithErrorCode("ReceiptReferenceAlreadyUsed");
        }
    }

    public class PartialRefundPreviousReceiptMustNotBeVoided : AbstractValidator<ReceiptRequest>
    {
        public PartialRefundPreviousReceiptMustNotBeVoided(FVReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .MustAsync(async (previousRef, _) =>
                    !await receiptReferenceProvider.HasExistingVoidAsync(previousRef!.SingleValue!))
                .When(x => x.cbChargeItems != null && x.IsPartialRefundReceipt() && x.cbPreviousReceiptReference != null)
                .WithMessage("The original receipt has already been voided and cannot be partially refunded.")
                .WithErrorCode("PreviousReceiptIsVoided");
        }
    }

    public class PartialRefundMustNotHaveExistingRefund : AbstractValidator<ReceiptRequest>
    {
        public PartialRefundMustNotHaveExistingRefund(FVReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .MustAsync(async (previousRef, _) =>
                    !await receiptReferenceProvider.HasExistingRefundAsync(previousRef!.SingleValue!))
                .When(x => x.cbChargeItems != null && x.IsPartialRefundReceipt() && x.cbPreviousReceiptReference != null)
                .WithMessage("A full refund for this receipt already exists.")
                .WithErrorCode("RefundAlreadyExists");
        }
    }

    public class HandwrittenSeriesNumberAlreadyLinked : AbstractValidator<ReceiptRequest>
    {
        public HandwrittenSeriesNumberAlreadyLinked(FVReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x)
                .MustAsync(async (request, _) =>
                {
                    if (!request.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayloadPT>(out var data) ||
                        data?.PT?.Series is null || !data.PT.Number.HasValue)
                        return true;
                    var series = data.PT.Series;
                    var number = data.PT.Number.Value;
                    return !await receiptReferenceProvider.HasMatchingQueueItemAsync(r =>
                        r.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) &&
                        r.cbReceiptReference != request.cbReceiptReference &&
                        r.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayloadPT>(out var d) &&
                        d?.PT?.Series == series &&
                        d.PT.Number.HasValue &&
                        d.PT.Number.Value == number);
                })
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
                .WithMessage(request =>
                {
                    request.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayloadPT>(out var data);
                    return $"Handwritten receipt with series '{data?.PT?.Series}' and number '{data?.PT?.Number}' has already been linked.";
                })
                .WithErrorCode("HandwrittenSeriesNumberAlreadyLinked");
        }
    }

    public class PreviousReceiptMustNotBeVoided : AbstractValidator<ReceiptRequest>
    {
        public PreviousReceiptMustNotBeVoided(FVReceiptReferenceProvider receiptReferenceProvider)
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
                .When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) && x.cbPreviousReceiptReference != null)
                .WithMessage("The referenced receipt has already been voided.")
                .WithErrorCode("PreviousReceiptIsVoided");
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

    public class VoidDocumentStatusCheck : AbstractValidator<ReceiptRequest>
    {
        public VoidDocumentStatusCheck(FVReceiptReferenceProvider receiptReferenceProvider, DocumentStatusProvider documentStatusProvider)
        {
            RuleFor(x => x)
                .MustAsync(async (request, _) =>
                {
                    var original = await receiptReferenceProvider.LoadOriginalReceiptWithResponseAsync(request.cbPreviousReceiptReference!.SingleValue!);
                    if (original == null)
                        return true;
                    var status = await documentStatusProvider.GetDocumentStatusStateAsync(original.Value);
                    return status.Status != DocumentStatus.Voided;
                })
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) && x.cbPreviousReceiptReference != null)
                .WithMessage(request => $"The receipt '{request.cbPreviousReceiptReference!.SingleValue}' has already been voided.")
                .WithErrorCode("VoidAlreadyExists");

            RuleFor(x => x)
                .MustAsync(async (request, _) =>
                {
                    var original = await receiptReferenceProvider.LoadOriginalReceiptWithResponseAsync(request.cbPreviousReceiptReference!.SingleValue!);
                    if (original == null)
                        return true;
                    var status = await documentStatusProvider.GetDocumentStatusStateAsync(original.Value);
                    return status.Status != DocumentStatus.Invoiced;
                })
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) && x.cbPreviousReceiptReference != null)
                .WithMessage(request => $"Cannot void receipt '{request.cbPreviousReceiptReference!.SingleValue}' because it has already been invoiced.")
                .WithErrorCode("CannotVoidInvoicedDocument");

            RuleFor(x => x)
                .MustAsync(async (request, _) =>
                {
                    var original = await receiptReferenceProvider.LoadOriginalReceiptWithResponseAsync(request.cbPreviousReceiptReference!.SingleValue!);
                    if (original == null)
                        return true;
                    var status = await documentStatusProvider.GetDocumentStatusStateAsync(original.Value);
                    return status.Status != DocumentStatus.Refunded;
                })
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) && x.cbPreviousReceiptReference != null)
                .WithMessage(request => $"Cannot void receipt '{request.cbPreviousReceiptReference!.SingleValue}' because it has already been refunded.")
                .WithErrorCode("CannotVoidRefundedDocument");

            RuleFor(x => x)
                .MustAsync(async (request, _) =>
                {
                    var original = await receiptReferenceProvider.LoadOriginalReceiptWithResponseAsync(request.cbPreviousReceiptReference!.SingleValue!);
                    if (original == null)
                        return true;
                    var status = await documentStatusProvider.GetDocumentStatusStateAsync(original.Value);
                    return status.Status != DocumentStatus.PartiallyRefunded;
                })
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) && x.cbPreviousReceiptReference != null)
                .WithMessage(request => $"Cannot void receipt '{request.cbPreviousReceiptReference!.SingleValue}' because it has already been partially refunded.")
                .WithErrorCode("CannotVoidPartiallyRefundedDocument");
        }
    }

    public class VoidFieldsMatch : AbstractValidator<ReceiptRequest>
    {
        public VoidFieldsMatch(FVReceiptReferenceProvider receiptReferenceProvider, VoidValidator voidValidator)
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
                .WithMessage("Void items mismatch with the original receipt.")
                .WithErrorCode("VoidItemsMismatch");
        }
    }

    public class FullRefundFieldsMatch : AbstractValidator<ReceiptRequest>
    {
        public FullRefundFieldsMatch(FVReceiptReferenceProvider receiptReferenceProvider, RefundValidator refundValidator)
        {
            RuleFor(x => x)
                .MustAsync(async (request, _) =>
                {
                    var previousRef = request.cbPreviousReceiptReference!.SingleValue!;
                    var original = await receiptReferenceProvider.LoadOriginalReceiptAsync(previousRef);
                    if (original == null)
                        return true;
                    var error = await refundValidator.ValidateFullRefundAsync(request, original, previousRef);
                    return error == null;
                })
                .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund)
                        && x.cbChargeItems != null
                        && !x.IsPartialRefundReceipt()
                        && x.cbPreviousReceiptReference != null)
                .WithMessage("Full refund items mismatch with the original receipt.")
                .WithErrorCode("FullRefundItemsMismatch");
        }
    }

    public class PartialRefundFieldsMatch : AbstractValidator<ReceiptRequest>
    {
        public PartialRefundFieldsMatch(FVReceiptReferenceProvider receiptReferenceProvider, RefundValidator refundValidator)
        {
            RuleFor(x => x)
                .MustAsync(async (request, _) =>
                {
                    var previousRef = request.cbPreviousReceiptReference!.SingleValue!;
                    var original = await receiptReferenceProvider.LoadOriginalReceiptAsync(previousRef);
                    if (original == null)
                        return true;
                    var error = await refundValidator.ValidatePartialRefundAsync(request, original, previousRef);
                    return error == null;
                })
                .When(x => x.cbChargeItems != null && x.IsPartialRefundReceipt() && x.cbPreviousReceiptReference != null)
                .WithMessage("Partial refund items mismatch with the original receipt.")
                .WithErrorCode("PartialRefundItemsMismatch");
        }
    }

    public class PaymentTransferForRefundedReceipt : AbstractValidator<ReceiptRequest>
    {
        public PaymentTransferForRefundedReceipt(FVReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .MustAsync(async (previousRef, _) =>
                    !await receiptReferenceProvider.HasExistingRefundAsync(previousRef!.SingleValue!))
                .When(x => x.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002) && x.cbPreviousReceiptReference != null)
                .WithMessage(request => $"Cannot create a payment transfer for receipt '{request.cbPreviousReceiptReference!.SingleValue}' because it has already been refunded.")
                .WithErrorCode("PaymentTransferForRefundedReceipt");
        }
    }

    public class PaymentTransferCustomerMismatch : AbstractValidator<ReceiptRequest>
    {
        public PaymentTransferCustomerMismatch(FVReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x)
                .MustAsync(async (request, _) =>
                {
                    var original = await receiptReferenceProvider.LoadOriginalReceiptAsync(request.cbPreviousReceiptReference!.SingleValue!);
                    if (original == null)
                        return true;
                    var (matches, _) = RefundValidator.CustomersMatch(original.GetCustomerOrNull(), request.GetCustomerOrNull());
                    return matches;
                })
                .When(x => x.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002) && x.cbPreviousReceiptReference != null)
                .WithMessage("Customer data mismatch between payment transfer and original invoice.")
                .WithErrorCode("PaymentTransferCustomerMismatch");
        }
    }

    public class PaymentTransferOnlyOneReference : AbstractValidator<ReceiptRequest>
    {
        public PaymentTransferOnlyOneReference()
        {
            RuleFor(x => x.cbPreviousReceiptReference)
                .Must(previousRef => previousRef!.IsSingle)
                .When(x => x.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002) && x.cbPreviousReceiptReference != null)
                .WithMessage("Payment transfer must reference exactly one receipt.")
                .WithErrorCode("PaymentTransferMultipleReferences");
        }
    }

    public class PreviousReceiptLineItemsMatch : AbstractValidator<ReceiptRequest>
    {
        public PreviousReceiptLineItemsMatch(FVReceiptReferenceProvider receiptReferenceProvider)
        {
            RuleFor(x => x)
                .MustAsync(async (request, _) =>
                {
                    var previousRef = request.cbPreviousReceiptReference!.IsSingle
                        ? request.cbPreviousReceiptReference.SingleValue!
                        : request.cbPreviousReceiptReference.GroupValue[0];
                    var original = await receiptReferenceProvider.LoadOriginalReceiptAsync(previousRef);
                    if (original == null)
                        return true;
                    return HasConnectableChargeItem(request, original);
                })
                .When(x => x.cbPreviousReceiptReference != null
                        && !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund)
                        && !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void)
                        && !x.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002)
                        && x.cbChargeItems != null
                        && !x.IsPartialRefundReceipt())
                .WithMessage("No matching product line items found between the current receipt and the referenced original receipt.")
                .WithErrorCode("PreviousReceiptLineItemMismatch");
        }

        private static bool HasConnectableChargeItem(ReceiptRequest currentRequest, ReceiptRequest originalRequest)
        {
            if (currentRequest.cbChargeItems is null || currentRequest.cbChargeItems.Count == 0 ||
                originalRequest.cbChargeItems is null || originalRequest.cbChargeItems.Count == 0)
                return false;

            var currentIdentifiers = currentRequest.cbChargeItems
                .Where(IsProductChargeItem)
                .Select(SaftExporter.GenerateUniqueProductIdentifier)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToHashSet(StringComparer.Ordinal);

            if (currentIdentifiers.Count == 0)
                return false;

            foreach (var originalItem in originalRequest.cbChargeItems.Where(IsProductChargeItem))
            {
                var originalIdentifier = SaftExporter.GenerateUniqueProductIdentifier(originalItem);
                if (!string.IsNullOrEmpty(originalIdentifier) && currentIdentifiers.Contains(originalIdentifier))
                    return true;
            }

            return false;
        }

        private static bool IsProductChargeItem(ChargeItem chargeItem)
        {
            if (chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.ExtraOrDiscount) ||
                chargeItem.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Receivable))
                return false;
            return true;
        }
    }
}
