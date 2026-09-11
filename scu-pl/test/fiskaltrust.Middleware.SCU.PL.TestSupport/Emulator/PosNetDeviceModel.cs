using System.Globalization;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Models;
using fiskaltrust.Middleware.SCU.PL.PosNet;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.TestSupport.Emulator;

/// <summary>
/// A model of what a POSNET Online register does with the commands it is sent, without a socket in
/// sight: it keeps the transaction state (no line outside a transaction, trend and prncancel close
/// one), does the receipt arithmetic the printer does — line values, rabat/narzut, storno, the
/// subtotal discount split over the PTU rates — and verifies the fiscal value a <c>trend</c> claims
/// against its own, answering 2805 like the device. What it totalizes is readable back through the
/// same status commands a printer offers (<c>strns</c>, <c>stot</c>, <c>scnt</c>, <c>sfsk</c>,
/// <c>scomm</c>), in the shape the real device answers them.
/// </summary>
/// <remarks>
/// It is a model, not a printer: it can only ever confirm what the protocol demands, never what a
/// specific device really answers — that is what cassettes are for. Error codes are the ones the
/// POT-I-DEV-05 error table lists for the situation. Where the register's behaviour has been
/// measured (a storno takes off exactly the value it states, rabat included or not) the model
/// follows the measurement; where it has not (how a subtotal discount is rounded across rates) it
/// follows the specification and says so (<see cref="PosNetArithmetic.Distribute"/>).
/// </remarks>
public sealed class PosNetDeviceModel
{
    /// <summary>The slots a POSNET register reports, as the protocol side defines them.</summary>
    public const int SlotCount = PosNetRateTable.SlotCount;

    /// <summary>The receipt document type <c>strns</c> reports in <c>ts</c>.</summary>
    public const int ReceiptDocumentType = 16;

    /// <summary>How an inactive PTU slot is reported in the rate fields.</summary>
    public const decimal InactiveRate = PosNetRateTable.InactiveMarker;

    /// <summary>How the tax-exempt (zw.) PTU slot is reported in the rate fields.</summary>
    public const decimal ExemptRate = PosNetRateTable.ExemptMarker;

    private const string NoSalesMoment = "2000-01-01;00:00";

    private readonly long[] _receiptTotalizers = new long[SlotCount];
    private OpenTransaction? _transaction;
    private CompletedReceipt? _lastReceipt;

    /// <summary>Whether the register reports fiscal mode (<c>fsT</c>). A test device is typically not.</summary>
    public bool Fiscalized { get; set; } = true;

    /// <summary>The numer unikatowy, in the shape a POSNET Online printer answers it.</summary>
    public string UniqueNumber { get; set; } = "ZBF 2101002392";

    /// <summary>
    /// The PTU table as the register reports it: a percentage, <see cref="ExemptRate"/> or
    /// <see cref="InactiveRate"/> per slot A–G. Derived from the SCU's default table rather than
    /// spelled out again, so the slots this register has cannot drift from the slots the SCU sends
    /// when no table is configured.
    /// </summary>
    public decimal[] RateTable { get; } = ToRateFields(PosNetConfiguration.DefaultVatRateTable());

    /// <summary>The rate fields a register whose PTU table holds <paramref name="entries"/> reports.</summary>
    private static decimal[] ToRateFields(List<PLVatRateTableEntry> entries)
    {
        var fields = new decimal[SlotCount];
        Array.Fill(fields, InactiveRate);
        foreach (var entry in entries)
        {
            fields[entry.PtuSlot![0] - 'A'] = entry.IsExempt ? ExemptRate : entry.VatRatePercent!.Value;
        }
        return fields;
    }

    /// <summary>The daily report counter (<c>rd</c>); the next report is <c>rd + 1</c>.</summary>
    public int DailyReportCounter { get; set; }

    /// <summary>
    /// Correctly completed receipts since the last daily report (<c>bn</c>, <c>pn</c>). The office
    /// printer numbers its receipts per day — measured: the daily report set <c>bn</c> and <c>bt</c>
    /// back to 0. Seeded so a fresh model looks like a day in progress.
    /// </summary>
    public int CompletedReceipts { get; private set; } = 84;

    /// <summary>
    /// Documents printed since fiscalization — receipts, canceled receipts, non-fiscal printouts,
    /// reports. The header number <c>hn</c> is the next one and runs across daily reports
    /// (measured: <c>bt18 nf1</c> → <c>hn20</c>, then <c>hn21</c> after the report).
    /// </summary>
    public int Printouts { get; private set; } = 84;

    /// <summary>Non-fiscal printouts since the last daily report (<c>nf</c>), e.g. goods returns.</summary>
    public int NonFiscalPrintouts { get; private set; }

    /// <summary>Canceled receipts (<c>bc</c>, <c>cn</c>).</summary>
    public int CanceledReceipts { get; private set; }

    /// <summary>The value of the canceled receipts (<c>ct</c>), in grosze.</summary>
    public long CanceledTotalGrosze { get; private set; }

    /// <summary>Correctly completed invoices (<c>fn</c>).</summary>
    public int Invoices { get; set; } = 3;

    /// <summary>
    /// The last receipt number (<c>bt</c>): every receipt the register started gets one, completed
    /// or canceled — a recorded device answered <c>bn58 bc2 bt60</c>.
    /// </summary>
    public int LastReceiptNumber => CompletedReceipts + CanceledReceipts;

    /// <summary>The receipt totalizers per PTU slot (<c>pa..pg</c>) since the last daily report, in grosze.</summary>
    public IReadOnlyList<long> ReceiptTotalizersGrosze => _receiptTotalizers;

    public bool TransactionOpen => _transaction is not null;

    /// <summary>
    /// The unique eDokument id (<c>ha</c>) the register assigns to the next eparagonidznext binding —
    /// configurable so a test can assert the value all the way through to the ReceiptResponse.
    /// </summary>
    public uint NextEDocumentId { get; set; } = 3054;

    /// <summary>
    /// The eDokument buffer record eparagonbufferget answers with (the parameters after the mnemonic,
    /// e.g. <c>"hd3054\tprN\tst1\t"</c>); null = a delivered electronic document. Real delivery
    /// states can only be scripted — the model has no hub to deliver to.
    /// </summary>
    public string? EDocumentBufferRecord { get; set; }

    /// <summary>Confirms eparagonidznext WITHOUT the promised <c>ha</c> (an armed but untrackable binding).</summary>
    public bool OmitEDocumentIdOnBind { get; set; }

    // --- printout customization: footer codes (qrcode + ftrcfg) and additional lines (trend fe0 → trftrln → trftrend)
    private bool _footerOpen;
    private readonly List<string> _footerLines = [];
    private string? _prepared2dCode;
    private string? _pendingBarcode;
    private int _pending2dPosition;

    /// <summary>The receipt's footer is open for additional lines (trend fe0 was sent, trftrend not yet).</summary>
    public bool FooterOpen => _footerOpen;

    /// <summary>The additional lines (trftrln na) printed after the last receipt.</summary>
    public IReadOnlyList<string> LastFooterLines => _footerLines;

    /// <summary>The 1D code (ftrcfg bc) printed in the footer of the last receipt, if any.</summary>
    public string? LastReceiptBarcode { get; private set; }

    /// <summary>The prepared 2D code (qrcode tx, hex) printed with the last receipt, if the footer configuration placed it (bb ≠ 0).</summary>
    public string? LastReceipt2dCode { get; private set; }

    /// <summary>Where the 2D code was printed on the last receipt (ftrcfg bb: 0 none, 1 above, 2 under the 1D code).</summary>
    public int LastReceipt2dPosition { get; private set; }

    /// <summary>The model of a register that has not been fiscalized (<c>fsN</c>).</summary>
    public PosNetDeviceModel NonFiscal()
    {
        Fiscalized = false;
        return this;
    }

    /// <summary>Answers a decoded command with the payload a printer would send back.</summary>
    public string Answer(PosNetResponse command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var parameters = command.Parameters;
        return command.CommandId switch
        {
            "scomm" => $"scomm\tfs{Flag(Fiscalized)}\ttzN\tts{(TransactionOpen ? ReceiptDocumentType : 0)}\thrT\tnu{UniqueNumber}\ttdN\t",
            "scnt" => $"scnt\trd{DailyReportCounter}\thn{Printouts + 1}\tbn{CompletedReceipts}\tfn{Invoices}\tnu{UniqueNumber}\tbc{CanceledReceipts}\tbt{LastReceiptNumber}\tfc0\t",
            "stot" => Stot(),
            "strns" => Strns(),
            "sfsk" => $"sfsk\tfs{Flag(Fiscalized)}\tcl0\trd{DailyReportCounter}\tvt1\t{RateTableFields()}rw{NoSalesMoment}\tnu{UniqueNumber}\t",
            "trinit" => Trinit(),
            // The NIP is printed, not totalized: the model only holds it to the transaction it belongs in.
            "trnipset" => InTransaction(command.CommandId, _ => Ok(command.CommandId)),
            "trline" => InTransaction(command.CommandId, transaction => Trline(transaction, parameters)),
            "trdiscntsubtot" => InTransaction(command.CommandId, transaction => Trdiscntsubtot(transaction, parameters)),
            "trpayment" => InTransaction(command.CommandId, transaction => Trpayment(transaction, parameters)),
            "trend" => InTransaction(command.CommandId, transaction => Trend(transaction, parameters)),
            "prncancel" => Prncancel(),
            "dailyrep" => Dailyrep(parameters),
            "stocash" => Stocash(parameters),
            // e-paragon (eDokument): only a fiscalized register binds documents (?2034 otherwise —
            // observed on non-fiscal lab printers); the buffer readback answers the scripted record.
            "eparagonidznext" => !Fiscalized
                ? Error("eparagonidznext", PosNetErrors.NoFiscalMode)
                : OmitEDocumentIdOnBind ? Ok("eparagonidznext") : $"eparagonidznext\tha{NextEDocumentId}\t",
            "eparagonbufferget" => $"eparagonbufferget\t{EDocumentBufferRecord ?? $"hd{NextEDocumentId}\tprN\tst1\t"}",
            // Printout customization: a 2D code is prepared, the footer configuration places it (and a
            // 1D code) on the next receipt; additional lines only exist while a receipt's footer is open.
            "qrcode" or "azteccode" or "dmcode" or "pdf417code" => Prepare2dCode(command.CommandId, parameters),
            "ftrcfg" => Ftrcfg(parameters),
            "trftrln" => Trftrln(parameters),
            "trftrend" => Trftrend(),
            _ => Ok(command.CommandId),
        };
    }

    private static string Ok(string mnemonic) => $"{mnemonic}\t";

    private static string Error(string mnemonic, int code) => $"{mnemonic}\t?{code}\t";

    private static string Flag(bool value) => value ? "T" : "N";

    private string InTransaction(string mnemonic, Func<OpenTransaction, string> handle)
        => _transaction is { } transaction
            ? handle(transaction)
            : Error(mnemonic, PosNetErrors.NoTransactionMode);

    private string Trinit()
    {
        if (_transaction is not null || _footerOpen)
        {
            // A receipt whose footer is still open (trend fe0 without trftrend) is not finished either.
            return Error("trinit", PosNetErrors.TransactionMode);
        }
        _transaction = new OpenTransaction();
        return Ok("trinit");
    }

    private string Trline(OpenTransaction transaction, IReadOnlyDictionary<string, string> p)
    {
        if (!TryInt(p, "vt", out var slot) || slot < 0 || slot >= SlotCount || RateTable[slot] == InactiveRate)
        {
            return Error("trline", PosNetErrors.VatField);
        }
        if (!TryLong(p, "pr", out var unitPrice) || unitPrice <= 0)
        {
            return Error("trline", PosNetErrors.PriceField);
        }
        var quantity = 1m;
        if (p.TryGetValue("il", out var il) && (!decimal.TryParse(il, NumberStyles.Number, CultureInfo.InvariantCulture, out quantity) || quantity <= 0m))
        {
            return Error("trline", PosNetErrors.QuantityField);
        }
        var lineValue = (long)Math.Round(unitPrice * quantity, 0, MidpointRounding.AwayFromZero);
        if (p.TryGetValue("wa", out var wa) && (!long.TryParse(wa, NumberStyles.None, CultureInfo.InvariantCulture, out var statedValue) || statedValue != lineValue))
        {
            // The register prints price, quantity and value and insists that they agree.
            return Error("trline", PosNetErrors.TotalField);
        }
        if (lineValue == 0)
        {
            return Error("trline", PosNetErrors.TotalZero);
        }

        long netValue;
        if (p.ContainsKey("rd") || p.ContainsKey("rw"))
        {
            if (!TryLong(p, "rw", out var modifierAmount) || modifierAmount <= 0)
            {
                return Error("trline", PosNetErrors.DiscountVerify);
            }
            var isDiscount = p.GetValueOrDefault("rd") != "0";
            if (isDiscount && modifierAmount > lineValue)
            {
                // "The discount may not exceed the value of the goods" (POT-I-DEV-05 p.219).
                return Error("trline", PosNetErrors.DiscountVerify);
            }
            netValue = isDiscount ? lineValue - modifierAmount : lineValue + modifierAmount;
        }
        else
        {
            netValue = lineValue;
        }

        var name = p.GetValueOrDefault("na") ?? "";
        if (p.GetValueOrDefault("st") == "1")
        {
            // A storno repeats a position that was printed; the register matches it against what it
            // sold and refuses a quantity or value that does not fit (2851/2852). What it then takes
            // off the receipt is exactly the value the storno states — with the rabat repeated the
            // discounted value, without it the value before the rabat. That is measured behaviour
            // (PosNetReceiptMapper.ResolveReversal), and it is what makes a storno without the rabat
            // fail later, on the trend.
            var sold = transaction.Lines.FirstOrDefault(l => !l.IsReversal && l.Name == name && l.Slot == slot && l.UnitPrice == unitPrice && l.RemainingQuantity > 0m);
            if (sold is null)
            {
                return Error("trline", PosNetErrors.StornoAmount);
            }
            if (quantity > sold.RemainingQuantity)
            {
                return Error("trline", PosNetErrors.StornoQuantity);
            }
            sold.RemainingQuantity -= quantity;
            transaction.PerRate[slot] -= netValue;
            transaction.Lines.Add(new Line(name, slot, unitPrice, quantity, IsReversal: true));
            return Ok("trline");
        }

        transaction.PerRate[slot] += netValue;
        transaction.Lines.Add(new Line(name, slot, unitPrice, quantity, IsReversal: false) { RemainingQuantity = quantity });
        return Ok("trline");
    }

    private static string Trdiscntsubtot(OpenTransaction transaction, IReadOnlyDictionary<string, string> p)
    {
        if (transaction.Lines.Count == 0)
        {
            return Error("trdiscntsubtot", PosNetErrors.BadTransactionState);
        }
        if (!TryLong(p, "rw", out var amount) || amount <= 0)
        {
            // Only the amount form is modelled — the SCU never sends a percentage (PosNetModifier).
            return Error("trdiscntsubtot", PosNetErrors.DiscountVerify);
        }
        var isDiscount = p.GetValueOrDefault("rd") != "0";
        if (isDiscount && amount > transaction.PerRate.Sum())
        {
            return Error("trdiscntsubtot", PosNetErrors.DiscountVerify);
        }

        var shares = PosNetArithmetic.Distribute(transaction.PerRate, amount);
        for (var i = 0; i < SlotCount; i++)
        {
            transaction.PerRate[i] += isDiscount ? -shares[i] : shares[i];
        }
        return Ok("trdiscntsubtot");
    }

    private static string Trpayment(OpenTransaction transaction, IReadOnlyDictionary<string, string> p)
    {
        if (!TryInt(p, "ty", out var type) || type < 0 || type > 8 || !TryLong(p, "wa", out var amount) || amount <= 0)
        {
            return Error("trpayment", PosNetErrors.Parameter);
        }
        if (p.GetValueOrDefault("re") == "1")
        {
            transaction.Change += amount;
        }
        else
        {
            transaction.Payments += amount;
        }
        return Ok("trpayment");
    }

    private string Trend(OpenTransaction transaction, IReadOnlyDictionary<string, string> p)
    {
        var total = transaction.PerRate.Sum();
        if (total <= 0)
        {
            return Error("trend", PosNetErrors.EndValueZero);
        }
        if (!TryLong(p, "to", out var statedTotal))
        {
            return Error("trend", PosNetErrors.Parameter);
        }
        if (statedTotal != total)
        {
            // The fiscal value the SCU claims is verified against the receipt the register printed.
            return Error("trend", PosNetErrors.EndTotalVerify);
        }
        var change = TryLong(p, "re", out var re) ? re : 0;
        var payments = TryLong(p, "fp", out var fp) ? fp : 0;
        if (payments != transaction.Payments)
        {
            return Error("trend", PosNetErrors.EndPaymentVerify);
        }
        if (change != transaction.Change)
        {
            return Error("trend", PosNetErrors.EndChangeVerify);
        }
        if (payments - change != total)
        {
            return Error("trend", PosNetErrors.PaymentsDoNotCover);
        }

        for (var i = 0; i < SlotCount; i++)
        {
            _receiptTotalizers[i] += transaction.PerRate[i];
        }
        CompletedReceipts++;
        Printouts++;
        _lastReceipt = new CompletedReceipt(transaction.PerRate.ToArray(), payments, change);
        _transaction = null;

        // The footer configuration and the prepared 2D code hold for this one printout.
        LastReceiptBarcode = _pendingBarcode;
        LastReceipt2dPosition = _pending2dPosition;
        LastReceipt2dCode = _pending2dPosition != 0 ? _prepared2dCode : null;
        _pendingBarcode = null;
        _pending2dPosition = 0;
        _prepared2dCode = null;
        _footerLines.Clear();
        _footerOpen = p.TryGetValue("fe", out var fe) && fe == "0";
        return Ok("trend");
    }

    /// <summary>
    /// The daily report closes the day: the receipt totalizers and the day's receipt counter start
    /// over, the report counter advances. In fiscal mode two consecutive zero reports are refused
    /// (POT-I-DEV-05 p.146), and the date has to be one the register can read.
    /// </summary>
    private string Dailyrep(IReadOnlyDictionary<string, string> p)
    {
        if (_transaction is not null)
        {
            return Error("dailyrep", PosNetErrors.TransactionMode);
        }
        if (p.TryGetValue("da", out var date) && !DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return Error("dailyrep", PosNetErrors.DateFormat);
        }
        if (Fiscalized && _receiptTotalizers.All(v => v == 0) && CompletedReceipts == 0)
        {
            return Error("dailyrep", PosNetErrors.DailyReportZero);
        }
        // Measured after the report: scnt bn0 bc0 bt0, stot pn0 cn0 ct0 nf0 — only hn and rd run on.
        DailyReportCounter++;
        Array.Clear(_receiptTotalizers);
        CompletedReceipts = 0;
        CanceledReceipts = 0;
        CanceledTotalGrosze = 0;
        NonFiscalPrintouts = 0;
        _lastReceipt = null;
        Printouts++;
        return Ok("dailyrep");
    }

    /// <summary>The goods return: one non-fiscal printout of the amount handed back, outside a transaction.</summary>
    private string Stocash(IReadOnlyDictionary<string, string> p)
    {
        if (_transaction is not null)
        {
            return Error("stocash", PosNetErrors.TransactionMode);
        }
        if (!TryLong(p, "kw", out var amount) || amount <= 0)
        {
            return Error("stocash", PosNetErrors.Parameter);
        }
        NonFiscalPrintouts++;
        Printouts++;
        return Ok("stocash");
    }

    private string Prepare2dCode(string mnemonic, IReadOnlyDictionary<string, string> p)
    {
        if (!p.TryGetValue("tx", out var tx) || tx.Length == 0)
        {
            return Error(mnemonic, PosNetErrors.Parameter);
        }
        var hex = p.TryGetValue("hx", out var hx) && hx == "1";
        if (hex ? tx.Length > 4000 || tx.Length % 2 != 0 : tx.Length > 2000)
        {
            return Error(mnemonic, PosNetErrors.Parameter);
        }
        _prepared2dCode = tx;
        return Ok(mnemonic);
    }

    private string Ftrcfg(IReadOnlyDictionary<string, string> p)
    {
        if (p.TryGetValue("bc", out var bc))
        {
            if (bc.Length > 30)
            {
                return Error("ftrcfg", PosNetErrors.Parameter);
            }
            _pendingBarcode = bc;
        }
        if (p.TryGetValue("bb", out var bbText))
        {
            if (!int.TryParse(bbText, out var bb) || bb is < 0 or > 2)
            {
                return Error("ftrcfg", PosNetErrors.Parameter);
            }
            if (bb != 0 && _prepared2dCode is null)
            {
                // "2d code for printing should be prepared in advance" (POT-I-DEV-37 p. 52).
                return Error("ftrcfg", PosNetErrors.Parameter);
            }
            _pending2dPosition = bb;
        }
        return Ok("ftrcfg");
    }

    private string Trftrln(IReadOnlyDictionary<string, string> p)
    {
        if (!_footerOpen)
        {
            return Error("trftrln", PosNetErrors.BadTransactionState);
        }
        if (_footerLines.Count >= 60)
        {
            return Error("trftrln", PosNetErrors.Parameter);
        }
        var text = p.TryGetValue("na", out var na) ? na : "";
        if (text.Length > 40)
        {
            return Error("trftrln", PosNetErrors.Parameter);
        }
        _footerLines.Add(text);
        Printouts++;
        return Ok("trftrln");
    }

    private string Trftrend()
    {
        if (!_footerOpen)
        {
            return Error("trftrend", PosNetErrors.BadTransactionState);
        }
        _footerOpen = false;
        return Ok("trftrend");
    }

    private string Prncancel()
    {
        if (_transaction is not { } transaction)
        {
            return Error("prncancel", PosNetErrors.NothingToCancel);
        }
        CanceledReceipts++;
        CanceledTotalGrosze += transaction.PerRate.Sum();
        Printouts++;
        _transaction = null;
        return Ok("prncancel");
    }

    private string Strns()
    {
        var open = _transaction;
        var perRate = open?.PerRate ?? _lastReceipt?.PerRateGrosze ?? new long[SlotCount];
        var documentType = open is not null || _lastReceipt is not null ? ReceiptDocumentType : 0;
        var payments = open?.Payments ?? _lastReceipt?.PaymentsGrosze ?? 0;
        var change = open?.Change ?? _lastReceipt?.ChangeGrosze ?? 0;
        return $"strns\tto{(open is null ? 0 : 1)}\tts{documentType}\t{AmountFields('v', perRate)}pp0\tpm0\tre{change}\tfp{payments}\tfe0\t";
    }

    private string Stot()
    {
        var salesMoment = _lastReceipt is null ? NoSalesMoment : DateTime.Now.ToString("yyyy-MM-dd;HH:mm", CultureInfo.InvariantCulture);
        return $"stot\tno{DailyReportCounter + 1}\t{AmountFields('f', new long[SlotCount])}fn{Invoices}\t{AmountFields('p', _receiptTotalizers)}"
            + $"pn{CompletedReceipts}\tct{CanceledTotalGrosze}\tcn{CanceledReceipts}\tcc0\t{RateTableFields()}ds{salesMoment}\tde{salesMoment}\tft0\tfl0\tnf{NonFiscalPrintouts}\t";
    }

    /// <summary>One field per slot, e.g. <c>pa1260 pb0 …</c> — amounts travel as integer grosze, as recorded off the device.</summary>
    private static string AmountFields(char prefix, IReadOnlyList<long> perRate)
        => string.Concat(perRate.Select((value, i) => $"{prefix}{(char)('a' + i)}{value.ToString(CultureInfo.InvariantCulture)}\t"));

    /// <summary>The rate table as the register formats it: <c>va23,00 vb8,00 … ve101,00</c>.</summary>
    private string RateTableFields()
        => string.Concat(RateTable.Select((rate, i) => $"v{(char)('a' + i)}{rate.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',')}\t"));

    private static bool TryInt(IReadOnlyDictionary<string, string> p, string key, out int value)
    {
        value = 0;
        return p.TryGetValue(key, out var text) && int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryLong(IReadOnlyDictionary<string, string> p, string key, out long value)
    {
        value = 0;
        return p.TryGetValue(key, out var text) && long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    private sealed class OpenTransaction
    {
        public List<Line> Lines { get; } = [];

        /// <summary>What the receipt is worth in each PTU slot so far — after line modifiers, stornos and subtotal modifiers.</summary>
        public long[] PerRate { get; } = new long[SlotCount];

        public long Payments { get; set; }

        public long Change { get; set; }
    }

    private sealed record Line(string Name, int Slot, long UnitPrice, decimal Quantity, bool IsReversal)
    {
        /// <summary>How much of a sold position has not been reversed yet.</summary>
        public decimal RemainingQuantity { get; set; }
    }

    private sealed record CompletedReceipt(long[] PerRateGrosze, long PaymentsGrosze, long ChangeGrosze);
}

/// <summary>The POSNET error codes the model answers with, named after the POT-I-DEV-05 error table.</summary>
public static class PosNetErrors
{
    /// <summary>2000 ERR_TR_FLD_VAT — incorrect rate number or inactive rate.</summary>
    public const int VatField = 2000;

    /// <summary>382 ERR_RD_ZERO — a daily report over zero totalizers, right after the previous one.</summary>
    public const int DailyReportZero = 382;

    /// <summary>2034 ERR_NO_FISC_MODE — an eDokument command on a register that is not fiscalized.</summary>
    public const int NoFiscalMode = 2034;

    /// <summary>2005 ERR_NO_TRNS_MODE — a transaction command outside a transaction.</summary>
    public const int NoTransactionMode = 2005;

    /// <summary>2024 ERR_RTC_BAD_FORMAT — a date the register cannot read.</summary>
    public const int DateFormat = 2024;

    /// <summary>2006 ERR_TR_FLD_PRICE.</summary>
    public const int PriceField = 2006;

    /// <summary>2007 ERR_TR_FLD_QUANT.</summary>
    public const int QuantityField = 2007;

    /// <summary>2008 ERR_TR_FLD_TOTAL — the line value does not match price × quantity.</summary>
    public const int TotalField = 2008;

    /// <summary>2009 ERR_TR_FLD_TOTAL_ZERO.</summary>
    public const int TotalZero = 2009;

    /// <summary>2038 ERR_TRNS_MODE — the device is already in transaction mode.</summary>
    public const int TransactionMode = 2038;

    /// <summary>2041 ERR_TR_END_VAL_0 — ending a receipt with a value of 0.</summary>
    public const int EndValueZero = 2041;

    /// <summary>2054 ERR_TR_END_PAYMENT — the payments do not cover the amount or the change.</summary>
    public const int PaymentsDoNotCover = 2054;

    /// <summary>2060 ERR_TR_BAD_STATE.</summary>
    public const int BadTransactionState = 2060;

    /// <summary>2063 ERR_PAR — parameter error.</summary>
    public const int Parameter = 2063;

    /// <summary>2064 ERR_FTR_NO_HDR — no start of a printout or transaction (nothing to cancel).</summary>
    public const int NothingToCancel = 2064;

    /// <summary>2801 ERR_DISCNT_VERIFY.</summary>
    public const int DiscountVerify = 2801;

    /// <summary>2805 ERR_ENDTOT_VERIFY — the fiscal value stated on trend is not the receipt's.</summary>
    public const int EndTotalVerify = 2805;

    /// <summary>2808 ERR_ENDPAYMENT_VERIFY.</summary>
    public const int EndPaymentVerify = 2808;

    /// <summary>2809 ERR_ENDCHANGE_VERIFY.</summary>
    public const int EndChangeVerify = 2809;

    /// <summary>2851 ERR_STORNO_QNT.</summary>
    public const int StornoQuantity = 2851;

    /// <summary>2852 ERR_STORNO_AMT.</summary>
    public const int StornoAmount = 2852;
}
