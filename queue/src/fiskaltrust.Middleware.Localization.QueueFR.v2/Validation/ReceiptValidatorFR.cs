using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Validation;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Validation;

/// <summary>
/// FR market validations. The queue currency for France is EUR (rfcs/0705-queue-single-currency):
/// a queue totalizes in exactly one currency, so every receipt - and every charge/pay item on it -
/// must carry EUR. Foreign-currency sales are recorded as EUR with the conversion done by the POS.
/// </summary>
public class ReceiptValidationsFR : AbstractValidator<ReceiptRequest>
{
    public ReceiptValidationsFR()
    {
        RuleFor(x => x.Currency)
            .Equal(Currency.EUR)
            .WithMessage(request => $"Expected currency EUR for this queue but received '{request.Currency}'.")
            .WithErrorCode("CurrencyMustMatchMarket");

        RuleForEach(x => x.cbChargeItems)
            .Must((request, chargeItem) => chargeItem.Currency == request.Currency)
            .WithMessage((request, chargeItem) => $"Charge item '{chargeItem.Description}' has currency '{chargeItem.Currency}' but the receipt currency is '{request.Currency}'.")
            .WithErrorCode("CurrencyMustMatchMarket");

        RuleForEach(x => x.cbPayItems)
            .Must((request, payItem) => payItem.Currency == request.Currency)
            .WithMessage((request, payItem) => $"Pay item '{payItem.Description}' has currency '{payItem.Currency}' but the receipt currency is '{request.Currency}'.")
            .WithErrorCode("CurrencyMustMatchMarket");

        // market-fr#235: a duplicate is numbered per copied receipt, so it has to name exactly one.
        RuleFor(x => x.cbPreviousReceiptReference)
            .Must(reference => reference is not null && reference.IsSingle && !string.IsNullOrWhiteSpace(reference.SingleValue))
            .When(x => x.ftReceiptCase.Case() == ReceiptCase.CopyReceiptPrintExistingReceipt0x3010)
            .WithMessage("A copy (0x3010) must reference the receipt it copies in cbPreviousReceiptReference, as a single reference.")
            .WithErrorCode("CopyRequiresPreviousReceiptReference");

        // QueueFR v1.2 (workerFR CheckDataConsistency), with each v2 case mapped to the v0 case the
        // PosSystem API sends v1.2 (market-fr#221): the receipts that carry no sale have no items.
        RuleFor(x => x.cbChargeItems)
            .Must(items => items is null || items.Count == 0)
            .When(x => CarriesNoItems(x.ftReceiptCase) || CarriesNoChargeItems(x.ftReceiptCase))
            .WithMessage(request => $"The receipt case 0x{(long) request.ftReceiptCase.Case():X4} must not have charge items.")
            .WithErrorCode("ChargeItemsNotAllowed");

        RuleFor(x => x.cbPayItems)
            .Must(items => items is null || items.Count == 0)
            .When(x => CarriesNoItems(x.ftReceiptCase))
            .WithMessage(request => $"The receipt case 0x{(long) request.ftReceiptCase.Case():X4} must not have pay items.")
            .WithErrorCode("PayItemsNotAllowed");

        // Table checks, pro formas and orders follow the shared v2 rules (market-fr ADR 0016): no
        // FR balance rule on 0x0006/0x0007, which the shared ReceiptBalance skips, and no internal
        // pay item required on a bill, as v1.2 required of its v0 0x08 bill.
    }

    /// <summary>
    /// v1.2 refuses charge and pay items on closings, zero, start, stop, audit, archive and copy
    /// receipts. The v2 zero receipt, the reserved 0x2001 and the SCU switch reach v1.2 as a zero
    /// receipt, the accounting event as an audit receipt.
    /// </summary>
    private static bool CarriesNoItems(ReceiptCase receiptCase) => receiptCase.Case() is ReceiptCase.ZeroReceipt0x2000
        or ReceiptCase.OneReceipt0x2001
        or ReceiptCase.ShiftClosing0x2010
        or ReceiptCase.DailyClosing0x2011
        or ReceiptCase.MonthlyClosing0x2012
        or ReceiptCase.YearlyClosing0x2013
        or ReceiptCase.ProtocolAccountingEvent0x3002
        or ReceiptCase.CopyReceiptPrintExistingReceipt0x3010
        or (ReceiptCase) 0x3011 /* Archive */
        or ReceiptCase.InitialOperationReceipt0x4001
        or ReceiptCase.OutOfOperationReceipt0x4002
        or ReceiptCase.InitSCUSwitch0x4011
        or ReceiptCase.FinishSCUSwitch0x4012;

    /// <summary>
    /// v1.2 refuses charge items on a payment proof, a pay-in, a pay-out and a payment transfer:
    /// the v2 payment transfer reaches v1.2 as one of the last three, the pay protocol as the first.
    /// </summary>
    private static bool CarriesNoChargeItems(ReceiptCase receiptCase) => receiptCase.Case() is ReceiptCase.PaymentTransfer0x0002
        or ReceiptCase.Pay0x3005;
}

public class ReceiptValidatorFR : MarketValidator
{
    public ReceiptValidatorFR(ReceiptReferenceProvider receiptReferenceProvider) : base(receiptReferenceProvider) { }

    /// <summary>
    /// v1.2 takes a pay-in, pay-out or transfer (v0 0x0A–0x0C, which the PosSystem API reaches from
    /// 0x0002) without a reference, such as the opening float of the day (market-fr#221).
    /// </summary>
    protected override IReadOnlyCollection<string> ExemptGlobalErrorCodes { get; } = ["PaymentTransferMissingPreviousReceiptReference"];

    protected override IEnumerable<IValidator<ReceiptRequest>> GetMarketValidators(ReceiptResponse? response = null, object? numberSeries = null)
    {
        yield return new ReceiptValidationsFR();
    }
}
