using System.Globalization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using static fiskaltrust.Middleware.SCU.FR.InfoCert.Signing.InfoCertStringFormat;

namespace fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;

/// <summary>The string an entry signs, chosen by its receipt case (market-fr#221, decision on the InfoCert signed content).</summary>
internal enum InfoCertStringKind
{
    /// <summary>The 25-field data set of the LNE and self-certified SCUs (archive-v2 §5): chain E.</summary>
    DataSet,
    /// <summary>R13 v4.2 §7, note and ticket.</summary>
    NoteTicket,
    /// <summary>R13 v4.2 §7, payment proof.</summary>
    PaymentProof,
    /// <summary>R12 v4.2 §7.1, invoice, credit note, down-payment invoice, pro forma.</summary>
    Invoice,
    /// <summary>R13 v4.2 §7 or R12 v4.2 §7.2, duplicate.</summary>
    Duplicate,
    /// <summary>R13 v4.2 §7 / R12 v4.2 §7.3, grand totals.</summary>
    GrandTotals,
    /// <summary>R13 v4.2 §7 / R12 v4.2 §7.4, fiscal archive.</summary>
    Archive,
    /// <summary>R19 v4.2, JET event.</summary>
    Jet,
}

/// <summary>
/// Builds the "chaîne de caractères avant hash" (R13 <c>ENC-TIK-ARG</c>) the Infocert SCU signs as the payload of its
/// ES256 JWS: one string per kind of justificatif, as R13 v4.2 §7, R12 v4.2 §7 and R19 v4.2 define them, and the
/// 25-field data set on chain E (market-fr#108, #221).
/// </summary>
/// <remarks>
/// <para>
/// Every R13/R12/R19 string ends with the start-of-chain flag (<c>TAG-*-OEN</c>, <c>O</c>/<c>N</c>) and the previous
/// signature of the chain (<c>TAG-*-SIG</c>, the previous JWS signature segment, empty on the first entry) (ADR 0001).
/// The chain counter is signed through the identification field, which is <c>ftReceiptIdentification</c>
/// (<c>&lt;chain&gt;&lt;counter&gt;</c>).
/// </para>
/// <para>
/// A training entry (chain X) signs the string of the type it simulates; the operation or document type carries
/// <c>FORMATION-</c>, and on the strings without such a field the <c>X</c> of the identification marks it.
/// </para>
/// </remarks>
internal static class InfoCertSignedString
{
    public const string TrainingMarker = "FORMATION-";

    public static string Build(ProcessRequest request, string? previousSignature, InfoCertConfiguration configuration)
    {
        var kind = KindOf(request.ReceiptRequest.ftReceiptCase);
        // Only a technical event (a non-training 0x3001) is a JET entry; anything else on chain L signs the data set.
        if (kind == InfoCertStringKind.Jet && !FRJetEvent.IsJetEvent(request.ReceiptRequest))
        {
            kind = InfoCertStringKind.DataSet;
        }
        return kind switch
        {
            InfoCertStringKind.DataSet => InfoCertDataSetBuilder.Build(request, previousSignature, configuration),
            InfoCertStringKind.NoteTicket => NoteTicket(request.ReceiptRequest, request.ReceiptResponse, previousSignature),
            InfoCertStringKind.PaymentProof => PaymentProof(request.ReceiptRequest, request.ReceiptResponse, previousSignature),
            InfoCertStringKind.Invoice => Invoice(request.ReceiptRequest, request.ReceiptResponse, previousSignature),
            InfoCertStringKind.Duplicate => Duplicate(request.ReceiptRequest, request.ReceiptResponse, previousSignature),
            InfoCertStringKind.GrandTotals => GrandTotals(request, previousSignature),
            InfoCertStringKind.Archive => Archive(request.ReceiptResponse, previousSignature),
            InfoCertStringKind.Jet => Jet(request.ReceiptRequest, request.ReceiptResponse, previousSignature),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    /// <summary>
    /// The string of a receipt case (its low word; the training flag does not change it). 0x0007 (Proforma invoice) is on
    /// chain B and signs R12. Zero, one, lifecycle, SCU-switch and migration receipts, 0x3002 (Protocol (audit/accounting
    /// event)) and training 0x3000/0x3003 sign the data set.
    /// </summary>
    public static InfoCertStringKind KindOf(ReceiptCase receiptCase) => ((long) receiptCase & 0xFFFF) switch
    {
        0x0000 or 0x0001 or 0x0004 => InfoCertStringKind.NoteTicket,
        0x0003 or 0x0005 or 0x0006 or 0x3004 => InfoCertStringKind.NoteTicket,
        0x0002 or 0x3005 => InfoCertStringKind.PaymentProof,
        >= 0x1000 and <= 0x1003 or 0x0007 => InfoCertStringKind.Invoice,
        0x3010 => InfoCertStringKind.Duplicate,
        >= 0x2010 and <= 0x2013 => InfoCertStringKind.GrandTotals,
        0x3011 => InfoCertStringKind.Archive,
        0x3001 => InfoCertStringKind.Jet,
        _ => InfoCertStringKind.DataSet,
    };

    /// <summary>
    /// R13 note/ticket, 7 fields: VAT breakdown, total, timestamp, number (<c>TAG-TIK-NUM</c> = <c>ftReceiptIdentification</c>),
    /// operation type, flag, previous signature.
    /// </summary>
    private static string NoteTicket(ReceiptRequest request, ReceiptResponse response, string? previousSignature) => Join(
    [
        Breakdown(request.cbChargeItems),
        Money(DocumentAmount(request.cbChargeItems)),
        Timestamp(response.ftReceiptMoment),
        Text(response.ftReceiptIdentification),
        Training(request, Operation(request)),
        .. Chain(previousSignature),
    ]);

    /// <summary>
    /// R13 payment proof, 6 fields: id (<c>ftReceiptIdentification</c>), amount paid, timestamp, the original document's
    /// number, flag, previous signature. The original's number is its fiscal number when the queue handed it over
    /// (<c>ftStateData.FR.OriginReceiptIdentification</c>), else the POS reference as sent; several origins are joined with
    /// <c>;</c> (market-fr#221).
    /// </summary>
    private static string PaymentProof(ReceiptRequest request, ReceiptResponse response, string? previousSignature) => Join(
    [
        Text(response.ftReceiptIdentification),
        Money((request.cbPayItems ?? new List<PayItem>()).Sum(x => x.Amount)),
        Timestamp(response.ftReceiptMoment),
        Text(FRStateData.Text(response, FRStateData.OriginReceiptIdentificationProperty) ?? References(request.cbPreviousReceiptReference)),
        .. Chain(previousSignature),
    ]);

    /// <summary>
    /// R12 invoice, 10 fields: VAT breakdown, total, timestamp, number, document type, customer name, postcode, customer
    /// VAT number (empty when absent, R12 fn 1), flag, previous signature.
    /// </summary>
    private static string Invoice(ReceiptRequest request, ReceiptResponse response, string? previousSignature)
    {
        return Join(
        [
            Breakdown(request.cbChargeItems),
            Money(DocumentAmount(request.cbChargeItems)),
            Timestamp(response.ftReceiptMoment),
            Text(response.ftReceiptIdentification),
            Training(request, InvoiceType(request)),
            Text(FRRequestValues.Property(request.cbCustomer, "CustomerName")),
            Text(FRRequestValues.Property(request.cbCustomer, "CustomerZip")),
            Text(FRRequestValues.Property(request.cbCustomer, "CustomerVATId")),
            .. Chain(previousSignature),
        ]);
    }

    /// <summary>
    /// A duplicate, chosen by the copied document's type (<c>ftStateData.FR.CopyType</c>): R12 §7.2 (8 fields) for an
    /// invoice or a pro forma, R13's ticket duplicate (8 fields) for a ticket, R13's note/payment-proof duplicate (9 fields,
    /// with the reason <c>TAG-DUP-ORI-MOT</c>) for every other document. Without a copy type (a training copy, or an original
    /// not stored by this queue) the ticket duplicate is signed.
    /// </summary>
    private static string Duplicate(ReceiptRequest request, ReceiptResponse response, string? previousSignature)
    {
        var copyType = FRStateData.Text(response, FRStateData.CopyTypeProperty);
        var (type, withReason) = FRStateData.Number(response, FRStateData.CopiedReceiptCaseProperty) is { } copiedCase
            ? CopiedType(copiedCase, copyType)
            : copyType switch
            {
                "invoice" => ("FACTURE", false),
                DownPaymentInvoice => ("ACOMPTE", false),
                "pro-forma" => ("PROFORMA", false),
                "payment-proof" => ("JUSTIFICATIF", true),
                "note" or "order" or "delivery-note" or "receipt-without-obligation" => ("NOTE", true),
                _ => ("TICKET", false),
            };
        var fields = new List<string>
        {
            Text(response.ftReceiptIdentification),
            Training(request, type),
            FRStateData.CopyNumber(response)?.ToString(CultureInfo.InvariantCulture) ?? "",
            Text(FRRequestValues.Operator(request.cbUser)),
            Timestamp(response.ftReceiptMoment),
            Text(FRStateData.Text(response, FRStateData.CopiedReceiptIdentificationProperty) ?? References(request.cbPreviousReceiptReference)),
        };
        if (withReason)
        {
            fields.Add(Text(CopyReason(request)));
        }
        fields.AddRange(Chain(previousSignature));
        return Join(fields);
    }

    /// <summary>
    /// R13 grand totals, one block per period the closing signs, then flag and previous signature: VAT breakdown, period
    /// total, perpetual total, timestamp, period id <c>&lt;ftReceiptIdentification&gt;:&lt;start&gt;-&lt;end&gt;</c>. The
    /// period start is <c>ftStateData.FR.Closing.Periods</c>' start of that period; the end is the receipt's moment.
    /// </summary>
    /// <remarks>
    /// One block per period the closing reports (<see cref="FRPeriodTotals.Reported"/>, as field 21 of the data set): a
    /// monthly closing signs the day and the month, a yearly closing the day, the month and the year (market-fr#221, P08).
    /// The breakdown is per VAT rate (P12c).
    /// </remarks>
    private static string GrandTotals(ProcessRequest request, string? previousSignature)
    {
        var response = request.ReceiptResponse;
        var totals = request.PeriodTotals ?? new FRPeriodTotals();
        var starts = FRStateData.ClosedPeriods(response);
        var fields = new List<string>();
        foreach (var period in totals.Reported())
        {
            var start = starts.FirstOrDefault(x => x.Period == period.Period)?.Start;
            fields.Add(RateBreakdown(period.Totals));
            fields.Add(Money(period.Totals.Totalizer));
            fields.Add(Money(totals.Perpetual.Totalizer));
            fields.Add(Timestamp(response.ftReceiptMoment));
            fields.Add(Text($"{response.ftReceiptIdentification}:{(start is { } s ? Timestamp(s) : "")}-{Timestamp(response.ftReceiptMoment)}"));
        }
        fields.AddRange(Chain(previousSignature));
        return Join(fields);
    }

    /// <summary>
    /// R13 archive: <c>TAG-ARC</c> as 27 values (identification, the archive boundaries, the day, month and year totals),
    /// flag and previous archive receipt's signature: 29 fields. The receipt signs the boundaries, never the archive's
    /// bytes (ADR 0002 amendment 2026-11-09). A training archive receipt has no boundaries: the values stay empty.
    /// </summary>
    private static string Archive(ReceiptResponse response, string? previousSignature)
    {
        var archive = FRStateData.Archive(response);
        var fields = new List<string> { Text(response.ftReceiptIdentification) };
        fields.AddRange(archive is null
            ? Enumerable.Repeat("", 8)
            : new[]
            {
                Id(archive.PreviousArchiveQueueItemId),
                Id(archive.FirstContainedReceiptQueueItemId),
                Moment(archive.FirstContainedReceiptMoment),
                Id(archive.LastContainedReceiptQueueItemId),
                Moment(archive.LastContainedReceiptMoment),
                Id(archive.LastReceiptJournalId),
                Id(archive.LastActionJournalId),
                Id(archive.LastJournalFRId),
            });
        foreach (var period in archive is null ? new FRTotals?[] { null, null, null } : new FRTotals?[] { archive.Day, archive.Month, archive.Year })
        {
            fields.AddRange(period is null
                ? Enumerable.Repeat("", 6)
                : new[] { RateBreakdown(period), Money(period.Totalizer), Money(period.PICash), Money(period.PINonCash), Money(period.PIInternal), Money(period.PIUnknown) });
        }
        fields.AddRange(Chain(previousSignature));
        return Join(fields);
    }

    /// <summary>
    /// R19 JET, 8 fields: id (<c>ftReceiptIdentification</c>), code, contextual information (JET-INF, not the label),
    /// timestamp, operator, cash-register code (<c>ftCashBoxIdentification</c>), flag, previous signature.
    /// </summary>
    private static string Jet(ReceiptRequest request, ReceiptResponse response, string? previousSignature)
    {
        if (!FRJetEvent.TryRead(request, out var jetEvent, out var error))
        {
            throw new FRValidationException($"The technical event cannot be signed: {error}");
        }
        return Join(
        [
            Text(response.ftReceiptIdentification),
            jetEvent!.Code.ToString(CultureInfo.InvariantCulture),
            Text(jetEvent.Information),
            Timestamp(response.ftReceiptMoment),
            Text(jetEvent.Operator),
            Text(response.ftCashBoxIdentification),
            .. Chain(previousSignature),
        ]);
    }

    /// <summary><c>VENTE</c>, <c>ANNULATION</c> (Void 0x0004) or <c>RETOUR</c> (Refund 0x0100).</summary>
    private static string Operation(ReceiptRequest request)
        => request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) ? "ANNULATION"
            : request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) ? "RETOUR"
            : "VENTE";

    /// <summary>
    /// R12 <c>TAG-FAC-TYP</c>: <c>PROFORMA</c> for 0x0007 (Proforma invoice), <c>AVOIR</c> for a refund or a void,
    /// <c>ACOMPTE</c> when every charge item is a down payment, else <c>FACTURE</c>.
    /// </summary>
    private static string InvoiceType(ReceiptRequest request)
    {
        if (((long) request.ftReceiptCase & 0xFFFF) == 0x0007)
        {
            return "PROFORMA";
        }
        if (request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) || request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
        {
            return "AVOIR";
        }
        var items = request.cbChargeItems ?? new List<ChargeItem>();
        return items.Count > 0 && items.All(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Downpayment)) ? "ACOMPTE" : "FACTURE";
    }

    /// <summary>
    /// The copy type a queue may hand over (<c>ftStateData.FR.CopyType</c>) for an invoice whose charge items are all down
    /// payments: no receipt-case flag tells an ACOMPTE (R12 §7.2) from a FACTURE.
    /// </summary>
    public const string DownPaymentInvoice = "down-payment-invoice";

    /// <summary>
    /// <c>TAG-DUP-TYP</c> and whether the reason is signed, from the copied receipt's case and flags
    /// (<c>ftStateData.FR.CopiedReceiptCase</c>): 0x0007 (Proforma invoice) is <c>PROFORMA</c>; an invoice 0x1000–0x1003 is
    /// <c>AVOIR</c> when refunded or voided, <c>ACOMPTE</c> when the copy type says <see cref="DownPaymentInvoice"/>, else
    /// <c>FACTURE</c>. R12 duplicates sign no reason; R13's note/payment-proof duplicate does.
    /// </summary>
    private static (string Type, bool WithReason) CopiedType(long copiedCase, string? copyType)
    {
        var receiptCase = (ReceiptCase) copiedCase;
        switch (copiedCase & 0xFFFF)
        {
            case 0x0007:
                return ("PROFORMA", false);
            case >= 0x1000 and <= 0x1003:
                return receiptCase.IsFlag(ReceiptCaseFlags.Void) || receiptCase.IsFlag(ReceiptCaseFlags.Refund) ? ("AVOIR", false)
                    : copyType == DownPaymentInvoice ? ("ACOMPTE", false)
                    : ("FACTURE", false);
            case 0x0002 or 0x3005:
                return ("JUSTIFICATIF", true);
            case 0x0003 or 0x0005 or 0x0006 or 0x3004:
                return ("NOTE", true);
            default:
                return ("TICKET", false);
        }
    }

    private static string Training(ReceiptRequest request, string value)
        => Text(request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training) ? TrainingMarker + value : value);

    /// <summary>The flag and the previous signature, the last two fields of every string (ADR 0001).</summary>
    private static string[] Chain(string? previousSignature)
        => [FRChaining.Flag(previousSignature).ToCode(), previousSignature ?? ""];

    /// <summary>
    /// The breakdown of accumulated totals per VAT rate (market-fr#221, P12c): <see cref="FRTotals.ByRate"/>, highest rate
    /// first, each rate on four digits; the v1 unknown class, which has no rate, last under <c>UNKN</c>. Only non-zero
    /// rates. Totals produced without per-rate amounts are broken down by class, each under its French rate (ReducedS as
    /// 2.1 %).
    /// </summary>
    private static string RateBreakdown(FRTotals totals)
    {
        var byRate = totals.ByRate.Count > 0 ? totals.ByRate : ClassRates(totals);
        var known = byRate
            .Where(x => x.Value != 0 && x.Key != FRTotals.UnknownRateKey)
            .Select(x => (Rate: decimal.Parse(x.Key, NumberStyles.Number, CultureInfo.InvariantCulture), x.Value))
            .GroupBy(x => Rate(x.Rate))
            .OrderByDescending(x => x.Key, StringComparer.Ordinal)
            .Select(x => (x.Key, x.Sum(item => item.Value)));
        var unknown = byRate.TryGetValue(FRTotals.UnknownRateKey, out var amount) && amount != 0
            ? new[] { (FRTotals.UnknownRateKey, amount) }
            : Array.Empty<(string, decimal)>();
        return Breakdown(known.Concat(unknown));
    }

    private static Dictionary<string, decimal> ClassRates(FRTotals totals) => new (string Key, decimal Amount)[]
    {
        ("20", totals.CINormal), ("10", totals.CIReduced2), ("5.5", totals.CIReduced1), ("2.1", totals.CIReducedS),
        ("0", totals.CIZero), (FRTotals.UnknownRateKey, totals.CIUnknown),
    }.Where(x => x.Amount != 0).ToDictionary(x => x.Key, x => x.Amount);

    private static string References(cbPreviousReceiptReference? reference)
        => reference is null ? "" : reference.IsSingle ? reference.SingleValue ?? "" : string.Join(";", reference.GroupValue ?? []);

    private static string? CopyReason(ReceiptRequest request) => FRRequestValues.CaseData(request.ftReceiptCaseData, "CopyReason");

    private static string Id(Guid? id) => id?.ToString("D") ?? "";

    private static string Moment(DateTime? moment) => moment is { } m ? Timestamp(m) : "";
}
