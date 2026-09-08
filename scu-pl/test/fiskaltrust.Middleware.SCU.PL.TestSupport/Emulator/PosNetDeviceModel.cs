using System.Globalization;
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
    public const int SlotCount = 7;

    /// <summary>The receipt document type <c>strns</c> reports in <c>ts</c>.</summary>
    public const int ReceiptDocumentType = 16;

    /// <summary>How an inactive PTU slot is reported in the rate fields.</summary>
    public const decimal InactiveRate = 101m;

    /// <summary>How the tax-exempt (zw.) PTU slot is reported in the rate fields.</summary>
    public const decimal ExemptRate = 100m;

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
    /// <see cref="InactiveRate"/> per slot A–G. Programmed like the SCU's default rate table
    /// (PosNetConfiguration.DefaultVatRateTable: A 23%, B 8%, C 5%, D 0%, G exempt), so the slots the
    /// SCU sends by default are the slots this register has — keep the two in step.
    /// </summary>
    public decimal[] RateTable { get; } = [23m, 8m, 5m, 0m, InactiveRate, InactiveRate, ExemptRate];

    /// <summary>The daily report counter (<c>rd</c>); the next report is <c>rd + 1</c>.</summary>
    public int DailyReportCounter { get; set; }

    /// <summary>Correctly completed receipts (<c>bn</c>, <c>pn</c>). Seeded so a fresh model looks like a used device.</summary>
    public int CompletedReceipts { get; private set; } = 84;

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
            "scnt" => $"scnt\trd{DailyReportCounter}\thn{LastReceiptNumber + 1}\tbn{CompletedReceipts}\tfn{Invoices}\tnu{UniqueNumber}\tbc{CanceledReceipts}\tbt{LastReceiptNumber}\tfc0\t",
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
        if (_transaction is not null)
        {
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
        _lastReceipt = new CompletedReceipt(transaction.PerRate.ToArray(), payments, change);
        _transaction = null;
        return Ok("trend");
    }

    private string Prncancel()
    {
        if (_transaction is not { } transaction)
        {
            return Error("prncancel", PosNetErrors.NothingToCancel);
        }
        CanceledReceipts++;
        CanceledTotalGrosze += transaction.PerRate.Sum();
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
            + $"pn{CompletedReceipts}\tct{CanceledTotalGrosze}\tcn{CanceledReceipts}\tcc0\t{RateTableFields()}ds{salesMoment}\tde{salesMoment}\tft0\tfl0\tnf0\t";
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

    /// <summary>2005 ERR_NO_TRNS_MODE — a transaction command outside a transaction.</summary>
    public const int NoTransactionMode = 2005;

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
