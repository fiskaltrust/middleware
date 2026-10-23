using System;
using System.Collections.Generic;
using System.Globalization;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

/// <summary>
/// A single POSNET protocol command: the mnemonic plus ordered two-letter parameters. Values are
/// already protocol-encoded (amounts as integer grosze, booleans as 0/1, quantities with a dot
/// separator) — the factories in <see cref="PosNetCommands"/> own that encoding.
/// </summary>
public sealed class PosNetCommand
{
    public PosNetCommand(string mnemonic, IReadOnlyList<KeyValuePair<string, string>>? parameters = null)
    {
        Mnemonic = mnemonic;
        Parameters = parameters ?? [];
    }

    public string Mnemonic { get; }

    public IReadOnlyList<KeyValuePair<string, string>> Parameters { get; }
}

public static class PosNetCommands
{
    /// <summary>
    /// Decimal places the protocol carries for a trline quantity. The invariant a sale line has to
    /// satisfy — price x quantity = line value — is checked against the quantity as it goes on the
    /// wire, so the format and the number of places are declared once here and shared.
    /// </summary>
    public const int QuantityDecimals = 3;

    private const string QuantityFormat = "0.000";

    public static PosNetCommand Trinit() => new("trinit", [new("bm", "0")]);

    /// <summary>
    /// A sale line, carrying the rabat/narzut granted on that line if one was passed. The line value
    /// (<c>wa</c>) is price x quantity — the value <em>before</em> the discount (POT-I-DEV-37 p.301:
    /// "wa Kwota total (cena x ilość)"); the register subtracts <c>rw</c> from it and totalizes the
    /// difference.
    /// </summary>
    /// <param name="isReversal">
    /// Sends the line as a storno (<c>st1</c>, "Flaga stornowania"): it repeats a position the
    /// register already printed and takes the stated quantity and value off it. The device verifies
    /// both against what was sold (errors 2851 ERR_STORNO_QNT and 2852 ERR_STORNO_AMT), so a
    /// reversal always states its value explicitly.
    /// </param>
    public static PosNetCommand Trline(string name, int vatSlotIndex, long unitPriceGrosze, decimal quantity, long totalGrosze, PosNetModifier? modifier = null, bool isReversal = false)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("na", name),
            new("vt", vatSlotIndex.ToString(CultureInfo.InvariantCulture)),
            new("pr", unitPriceGrosze.ToString(CultureInfo.InvariantCulture)),
        };
        if (isReversal)
        {
            // Where the specification's own trline examples put it: after the price, before the value.
            parameters.Add(new("st", "1"));
        }
        if (quantity != 1m)
        {
            parameters.Add(new("il", quantity.ToString(QuantityFormat, CultureInfo.InvariantCulture)));
        }
        // The line value is what the device measures the discount against — "the discount may not
        // exceed the value of the goods" (POT-I-DEV-05 p.219) — so a line that carries one states
        // its value explicitly instead of leaving the device to derive it from price x quantity.
        if (quantity != 1m || modifier is not null || isReversal)
        {
            parameters.Add(new("wa", totalGrosze.ToString(CultureInfo.InvariantCulture)));
        }
        if (modifier is not null)
        {
            parameters.Add(new("rd", modifier.IsDiscount ? "1" : "0"));
            if (!string.IsNullOrWhiteSpace(modifier.Name))
            {
                parameters.Add(new("rn", modifier.Name!));
            }
            parameters.Add(new("rw", modifier.AmountGrosze.ToString(CultureInfo.InvariantCulture)));
        }
        return new PosNetCommand("trline", parameters);
    }

    /// <summary>
    /// A rabat/narzut od podsumy: the register applies it to the subtotal of the open receipt and
    /// distributes it over the PTU rates itself (POT-I-DEV-05 p.226), which is why the command
    /// carries no rate. It cannot be reversed — only an opposing markup offsets it.
    /// </summary>
    public static PosNetCommand Trdiscntsubtot(PosNetModifier modifier)
    {
        var parameters = new List<KeyValuePair<string, string>>();
        if (!string.IsNullOrWhiteSpace(modifier.Name))
        {
            parameters.Add(new("na", modifier.Name!));
        }
        parameters.Add(new("rd", modifier.IsDiscount ? "1" : "0"));
        parameters.Add(new("rw", modifier.AmountGrosze.ToString(CultureInfo.InvariantCulture)));
        return new PosNetCommand("trdiscntsubtot", parameters);
    }

    public static PosNetCommand Trpayment(int paymentType, long amountGrosze, bool isChange, string? name = null)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("ty", paymentType.ToString(CultureInfo.InvariantCulture)),
            new("wa", amountGrosze.ToString(CultureInfo.InvariantCulture)),
        };
        if (!string.IsNullOrWhiteSpace(name))
        {
            parameters.Add(new("na", name));
        }
        parameters.Add(new("re", isChange ? "1" : "0"));
        return new PosNetCommand("trpayment", parameters);
    }

    /// <param name="endFooter">
    /// False keeps the footer open for additional lines (<c>fe0</c>): the receipt is then finished
    /// with <see cref="Trftrend"/> after the <see cref="Trftrln"/> lines. True (the default) lets the
    /// register end the footer itself.
    /// </param>
    public static PosNetCommand Trend(long totalGrosze, long paymentsGrosze, long changeGrosze, bool endFooter = true)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("to", totalGrosze.ToString(CultureInfo.InvariantCulture)),
        };
        if (changeGrosze > 0)
        {
            parameters.Add(new("re", changeGrosze.ToString(CultureInfo.InvariantCulture)));
        }
        parameters.Add(new("fp", paymentsGrosze.ToString(CultureInfo.InvariantCulture)));
        if (!endFooter)
        {
            parameters.Add(new("fe", "0"));
        }
        return new PosNetCommand("trend", parameters);
    }

    /// <summary>
    /// An additional line after the receipt (POT-I-DEV-37 pp. 371–373): id 25 is the plain line
    /// without a keyword (text right-aligned behind a 15-character column), 0–40 are keyword lines
    /// (the register's label left, the text right), 41 prints the text left-aligned in normal size;
    /// sw/sh double the character width/height (not for 41). Only valid after a trend with fe0.
    /// </summary>
    public static PosNetCommand Trftrln(string text, bool doubleWidth = false, bool doubleHeight = false, int lineId = 25)
    {
        var parameters = new List<KeyValuePair<string, string>> { new("id", lineId.ToString(CultureInfo.InvariantCulture)), new("na", text) };
        if (doubleWidth)
        {
            parameters.Add(new("sw", "1"));
        }
        if (doubleHeight)
        {
            parameters.Add(new("sh", "1"));
        }
        return new PosNetCommand("trftrln", parameters);
    }

    /// <summary>Ends the footer of a receipt that was closed with trend fe0.</summary>
    public static PosNetCommand Trftrend() => new("trftrend");

    /// <summary>
    /// Prepares a QR code for the next printout (POT-I-DEV-37 p. 273). The content travels in hex
    /// mode so any character can be encoded; the register prints it where the footer configuration
    /// (<see cref="Ftrcfg"/>) places it, then invalidates it.
    /// </summary>
    public static PosNetCommand Qrcode(string data, int pixelSize, int errorCorrection)
        => new("qrcode",
        [
            new("px", pixelSize.ToString(CultureInfo.InvariantCulture)),
            new("el", errorCorrection.ToString(CultureInfo.InvariantCulture)),
            new("hx", "1"),
            new("tx", Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(data))),
        ]);

    /// <summary>
    /// The printout footer configuration for the next receipt (POT-I-DEV-37 pp. 68–69): the cashier
    /// name (<c>cc</c>, up to 32) and the register number (<c>cn</c>, up to 8) in the footer line,
    /// the 1D code (<c>bc</c>, up to 30 characters), where the prepared 2D code is printed (<c>bb</c>:
    /// 0 not at all, 1 above, 2 under the 1D code), and up to three info lines (<c>ln</c>, LF-separated,
    /// formatting codes as in ftrinfoset). <c>ca0</c>/<c>lb0</c>: everything holds for the next printout
    /// only. A value that is too long is refused by the register with error 10 (PROTO_ERR_DATA_LENGTH).
    /// </summary>
    public static PosNetCommand Ftrcfg(string? barcode, int code2dPosition, string? cashier = null, string? register = null, IReadOnlyList<string>? infoLines = null)
    {
        var parameters = new List<KeyValuePair<string, string>>();
        if (cashier is not null)
        {
            parameters.Add(new("cc", cashier));
        }
        if (register is not null)
        {
            parameters.Add(new("cn", register));
        }
        if (cashier is not null || register is not null)
        {
            parameters.Add(new("ca", "0"));
        }
        if (barcode is not null)
        {
            parameters.Add(new("bc", barcode));
        }
        parameters.Add(new("bb", code2dPosition.ToString(CultureInfo.InvariantCulture)));
        if (infoLines is { Count: > 0 })
        {
            parameters.Add(new("ln", string.Join("\n", infoLines)));
            parameters.Add(new("lb", "0"));
        }
        return new PosNetCommand("ftrcfg", parameters);
    }

    /// <summary>Prints the buyer's NIP with the receipt footer (paragon z NIP); valid inside an open receipt.</summary>
    public static PosNetCommand Trnipset(string buyerNip) => new("trnipset", [new("ni", buyerNip)]);

    /// <summary>
    /// Binds the <em>next</em> fiscal document to an e-receipt customer identifier (IDZ) — the
    /// printer then emits it as an eDokument (paperless when delivery is confirmed) instead of a
    /// plain paper receipt. Valid only on a fiscalized device (?2034 otherwise); the confirmation
    /// carries <c>ha</c>, the unique eDokument id.
    /// </summary>
    public static PosNetCommand EparagonIdzNext(string customerId) => new("eparagonidznext", [new("id", customerId)]);

    /// <summary>Reads one eDokument buffer record by its unique id (the <c>ha</c> from the binding).</summary>
    public static PosNetCommand EparagonBufferGet(uint eDocumentId) => new("eparagonbufferget", [new("hd", eDocumentId.ToString(CultureInfo.InvariantCulture))]);

    /// <summary>Clears a pending eDokument binding armed by <c>eparagonidznext</c> (spec p. 253).</summary>
    public static PosNetCommand EparagonIdzCancel() => new("eparagonidzcancel");

    public static PosNetCommand Scomm() => new("scomm");

    public static PosNetCommand Scnt() => new("scnt");

    /// <summary>The protected-memory document type of a receipt (paragon) for <see cref="Eclastdocnoget"/>.</summary>
    public const int ReceiptDocumentType = 0;

    /// <summary>
    /// The number of the last document of a type in the protected memory (POT-I-DEV-37 p.102, <c>di</c>;
    /// <c>di0</c> when there is none). One numbering across all document types, so it is not the receipt number.
    /// </summary>
    public static PosNetCommand Eclastdocnoget(int documentType)
        => new("eclastdocnoget", [new("ty", documentType.ToString(CultureInfo.InvariantCulture))]);

    /// <summary>
    /// The register's clock (POT-I-DEV-37 p.18): <c>tm</c> in ISO 8601 with offset, and <c>da</c> as
    /// <c>yyyy-MM-dd;HH:mm</c> — the only field older firmware answers (POT-I-DEV-05).
    /// </summary>
    public static PosNetCommand Rtcget() => new("rtcget");

    /// <summary>The fiscal memory status — among it the PTU rate table as programmed on the register (<c>va..vg</c>).</summary>
    public static PosNetCommand Sfsk() => new("sfsk");

    /// <summary>
    /// The goods return (zwrot towaru): a non-fiscal printout of the amount handed back
    /// (POT-I-DEV-05 p.254, <c>kw</c> in grosze). A return is not a fiscal document on a Polish
    /// register — the returned positions are kept in the taxpayer's returns register, the printout
    /// documents the payout.
    /// </summary>
    public static PosNetCommand Stocash(long amountGrosze)
        => new("stocash", [new("kw", amountGrosze.ToString(CultureInfo.InvariantCulture))]);

    /// <summary>
    /// The daily (Z) report. The date is validated against the register's clock and confirms which
    /// day is being closed; without it the operator would have to confirm the date on the keyboard.
    /// </summary>
    public static PosNetCommand Dailyrep(DateOnly date)
        => new("dailyrep", [new("da", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))]);

    /// <summary>
    /// The monthly report: a periodic report over the month of <paramref name="month"/> (spec p. 220).
    /// The register only reads the year and month, but the date must still be a full valid date.
    /// It reads fiscal memory and changes no counter, so it may be repeated.
    /// </summary>
    public static PosNetCommand Monthlyrep(DateOnly month)
        => new("monthlyrep", [new("da", month.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))]);

    /// <summary>
    /// A periodic report over the daily reports between two dates, both included (spec p. 219). Like
    /// the monthly report it only reads fiscal memory. Not to be confused with the settlement report
    /// (raport rozliczeniowy), which ends the device's fiscal life and is never sent by this SCU.
    /// </summary>
    public static PosNetCommand Periodicrepbydates(DateOnly from, DateOnly to)
        => new("periodicrepbydates",
        [
            new("fd", from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new("td", to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        ]);

    /// <summary>The mechanism status in asynchronous mode: answered at once, even while a print blocks the queue (p. 16).</summary>
    public static PosNetCommand SprnAsync() => new("!sprn");

    /// <summary>
    /// The general status in asynchronous mode (p. 16, 391): <c>ds</c> 0 ready / 3 waiting for the
    /// operator after an error, <c>qe</c> whether the command queue is empty.
    /// </summary>
    public static PosNetCommand SdevAsync() => new("!sdev");

    public static PosNetCommand Prncancel() => new("prncancel");
}
