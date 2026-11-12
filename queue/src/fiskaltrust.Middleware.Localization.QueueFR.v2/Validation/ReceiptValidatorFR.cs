using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules.Atoms.Customer;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.storage.V0;
using FluentValidation;
using FluentValidation.Results;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Validation;

/// <summary>
/// FR market validations. The queue currency for France is EUR (rfcs/0705-queue-single-currency):
/// a queue totalizes in exactly one currency, so every receipt - and every charge/pay item on it -
/// must carry EUR. Foreign-currency sales are recorded as EUR with the conversion done by the POS.
/// </summary>
public class ReceiptValidationsFR : AbstractValidator<ReceiptRequest>
{
    /// <summary>The <c>CertificationBody</c> the InfoCert SCU reports.</summary>
    public const string InfoCertBody = "Infocert";

    /// <summary>The <c>CertificationBody</c> the LNE SCU reports.</summary>
    public const string LneBody = "LNE";

    /// <param name="certificationBody">
    /// The body that certified the queue's SCU, as it reports it (<c>Infocert</c>, <c>LNE</c>, ...): the référentiel
    /// rules of that body apply on top of the common ones (market-fr#140). Null applies the common rules only.
    /// </param>
    public ReceiptValidationsFR(string? certificationBody = null)
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
            .When(x => CarriesNoItems(x.ftReceiptCase))
            .WithMessage(request => $"The receipt case 0x{(long) request.ftReceiptCase.Case():X4} must not have charge items.")
            .WithErrorCode("ChargeItemsNotAllowed");

        // A drawer move takes either convention (market-fr ADR 0016): pay items only, summing to
        // zero as in v1.2, or a not-taxable cash transfer, receivable or voucher charge item with
        // its pay item, as the public reference tables build it. Nothing else is charged on chain P.
        RuleForEach(x => x.cbChargeItems)
            .Must(IsDrawerMoveChargeItem)
            .When(x => CarriesOnlyDrawerMoveChargeItems(x.ftReceiptCase))
            .WithMessage((request, chargeItem) => $"Charge item '{chargeItem.Description}' (0x{(long) chargeItem.ftChargeItemCase:X16}) is not allowed on receipt case 0x{(long) request.ftReceiptCase.Case():X4}: only a cash transfer (0xA), receivable (0x9) or voucher (0x4) charge item that is not taxable (0x8) is.")
            .WithErrorCode("ChargeItemsNotAllowed");

        RuleFor(x => x.cbPayItems)
            .Must(items => items is null || items.Count == 0)
            .When(x => CarriesNoItems(x.ftReceiptCase))
            .WithMessage(request => $"The receipt case 0x{(long) request.ftReceiptCase.Case():X4} must not have pay items.")
            .WithErrorCode("PayItemsNotAllowed");

        // Contract §1.2 ManagerId (D4): a training receipt names the person responsible for the operator.
        RuleFor(x => x.ftReceiptCaseData)
            .Must(caseData => !string.IsNullOrWhiteSpace(FRCaseData.ManagerId(caseData)))
            .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
            .WithMessage("A training receipt must name the person responsible for the operator in ftReceiptCaseData.FR.ManagerId.")
            .WithErrorCode("TrainingRequiresManagerId");

        // Contract §1.1 cbPreviousReceiptReference (LNE Ex. 4): a void is linked to the receipt it voids.
        RuleFor(x => x.cbPreviousReceiptReference)
            .NotNull()
            .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
            .WithMessage("A void must reference the receipt it voids in cbPreviousReceiptReference.")
            .WithErrorCode("VoidMissingPreviousReceiptReference");

        if (certificationBody == InfoCertBody)
        {
            // R13 ENC-TIK-OPS-NID, R12 FAC-OPS-NID: the operator on tickets and invoices (contract §1.1 cbUser, InfoCert M).
            RuleFor(x => x.cbUser)
                .Must((request, _) => !string.IsNullOrWhiteSpace(request.GetcbUserOrNull()))
                .When(x => x.ResolveChain() is FRReceiptChain.Ticket or FRReceiptChain.Invoice)
                .WithMessage("A ticket or an invoice must name its operator in cbUser.")
                .WithErrorCode("UserRequired");

            // market-fr#140 (2026-10-26, amended): the R12 customer on every invoice of chain I.
            When(x => x.ResolveChain() == FRReceiptChain.Invoice, () =>
            {
                Include(new CustomerRequiredForInvoice());
                Include(new InvoiceCustomerFR());
            });
        }

        if (certificationBody == LneBody)
        {
            // Ref LNE 1.8 Ex. 15: archive operations and logged events carry the terminal (contract §1.1 cbTerminalID).
            RuleFor(x => x.cbTerminalID)
                .NotEmpty()
                .When(x => x.ResolveChain() is FRReceiptChain.Archive or FRReceiptChain.Log)
                .WithMessage("An archive or log receipt must name its terminal in cbTerminalID.")
                .WithErrorCode("TerminalIdRequired");
        }

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
    /// The chain P receipts: v1.2 refuses every charge item on them (a payment proof, pay-in,
    /// pay-out or transfer); v2 also takes the drawer-move charge item of the public reference tables.
    /// </summary>
    private static bool CarriesOnlyDrawerMoveChargeItems(ReceiptCase receiptCase) => receiptCase.Case() is ReceiptCase.PaymentTransfer0x0002
        or ReceiptCase.Pay0x3005;

    /// <summary>A not-taxable (V=8) cash transfer (S=A), receivable (S=9) or voucher (S=4) charge item.</summary>
    internal static bool IsDrawerMoveChargeItem(ChargeItem chargeItem) => chargeItem.ftChargeItemCase.Vat() == ChargeItemCase.NotTaxable
        && chargeItem.ftChargeItemCase.TypeOfService() is ChargeItemCaseTypeOfService.CashTransfer
            or ChargeItemCaseTypeOfService.Receivable
            or ChargeItemCaseTypeOfService.Voucher;
}

public class ReceiptValidatorFR : MarketValidator, IMarketValidator
{
    private readonly Func<Task<FRSSCDInfo?>>? _scuInfo;
    private string? _certificationBody;
    private bool _certificationBodyKnown;

    /// <param name="scuInfo">The queue's SCU, asked once which body certified it (<see cref="ReceiptValidationsFR"/>).</param>
    public ReceiptValidatorFR(ReceiptReferenceProvider receiptReferenceProvider, Func<Task<FRSSCDInfo?>>? scuInfo = null) : base(receiptReferenceProvider)
    {
        _scuInfo = scuInfo;
    }

    /// <summary>
    /// Learns the SCU's certification body before validating. It is asked once; an SCU that cannot be reached is
    /// asked again on the next receipt, and meanwhile only the common rules apply (signing fails anyway).
    /// </summary>
    public new async Task<ValidationResult> ValidateAsync(ReceiptRequest request, ftQueue? queue = null, ReceiptResponse? response = null, object? numberSeries = null)
    {
        if (!_certificationBodyKnown && _scuInfo is not null)
        {
            try
            {
                _certificationBody = FRSSCDInfoHelper.GetCertificationBody(await _scuInfo().ConfigureAwait(false));
                _certificationBodyKnown = true;
            }
            catch (Exception)
            {
                _certificationBody = null;
            }
        }

        return await base.ValidateAsync(request, queue, response, numberSeries).ConfigureAwait(false);
    }

    /// <summary>
    /// v1.2 takes a pay-in, pay-out or transfer (v0 0x0A–0x0C, which the PosSystem API reaches from
    /// 0x0002) without a reference, such as the opening float of the day (market-fr#221).
    /// </summary>
    protected override IReadOnlyCollection<string> ExemptGlobalErrorCodes { get; } = ["PaymentTransferMissingPreviousReceiptReference"];

    /// <summary>
    /// A drawer-move charge item is not taxable (V=8) and carries no sale, so it has no nature of
    /// VAT to give (market-fr ADR 0016).
    /// </summary>
    protected override Func<ChargeItem, bool>? ZeroVatRateNatureNotRequired => ReceiptValidationsFR.IsDrawerMoveChargeItem;

    protected override IEnumerable<IValidator<ReceiptRequest>> GetMarketValidators(ReceiptResponse? response = null, object? numberSeries = null)
    {
        yield return new ReceiptValidationsFR(_certificationBody);
    }
}
