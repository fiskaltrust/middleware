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

        // v1.2 balances a bill (v0 0x08) like every other receipt. The shared ReceiptBalance rule
        // skips table checks and pro formas, so France checks them here, with the same tolerance.
        RuleFor(x => x)
            .Must(request => Math.Abs((request.cbChargeItems?.Sum(x => x.Amount) ?? 0m) - (request.cbPayItems?.Sum(x => x.Amount) ?? 0m)) <= BalanceTolerance)
            .When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten)
                    && (x.ftReceiptCase.IsCase((ReceiptCase) 0x0006) || x.ftReceiptCase.IsCase((ReceiptCase) 0x0007)))
            .WithMessage(request => $"Receipt is not balanced: charge items sum ({request.cbChargeItems?.Sum(x => x.Amount) ?? 0m:F2}) does not match pay items sum ({request.cbPayItems?.Sum(x => x.Amount) ?? 0m:F2}).")
            .WithErrorCode("ReceiptNotBalanced");

        // v1.2 takes a bill (v0 0x08) with exactly one pay item, of case v0 0x11, which the PosSystem
        // API reaches from v2 0x0D. Table checks, pro formas and orders are bills; sales are not.
        RuleFor(x => x.cbPayItems)
            .Must(items => items is { Count: 1 } && items[0].ftPayItemCase.IsCase(BillPayItemCase))
            .When(x => IsBill(x.ftReceiptCase))
            .WithMessage(request => $"The receipt case 0x{(long) request.ftReceiptCase.Case():X4} must have exactly one pay item, of case 0x{(long) BillPayItemCase:X4}.")
            .WithErrorCode("BillRequiresOneInternalPayItem");
    }

    private const PayItemCase BillPayItemCase = (PayItemCase) 0x000D;

    /// <summary>The cases the PosSystem API sends v1.2 as a bill (v0 0x08): table check, pro forma, order.</summary>
    private static bool IsBill(ReceiptCase receiptCase) => receiptCase.IsCase((ReceiptCase) 0x0006)
        || receiptCase.IsCase((ReceiptCase) 0x0007)
        || receiptCase.IsCase(ReceiptCase.Order0x3004);

    private const decimal BalanceTolerance = 0.01m;

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

    protected override IEnumerable<IValidator<ReceiptRequest>> GetMarketValidators(ReceiptResponse? response = null, object? numberSeries = null)
    {
        yield return new ReceiptValidationsFR();
    }
}
