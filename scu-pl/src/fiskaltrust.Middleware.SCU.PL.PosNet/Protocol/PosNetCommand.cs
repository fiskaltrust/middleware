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
    /// An additional line after the receipt (POT-I-DEV-37 p. 274): id 25 is the plain line without a
    /// keyword; sw/sh double the character width/height. Only valid after a trend with fe0.
    /// </summary>
    public static PosNetCommand Trftrln(string text, bool doubleWidth = false, bool doubleHeight = false)
    {
        var parameters = new List<KeyValuePair<string, string>> { new("id", "25"), new("na", text) };
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
    /// The printout footer configuration for the next receipt (POT-I-DEV-37 p. 51): the 1D code
    /// (<c>bc</c>, up to 30 characters) and where the prepared 2D code is printed (<c>bb</c>: 0 not
    /// at all, 1 above, 2 under the 1D code). Without ca1/lb1 the settings hold until the next printout.
    /// </summary>
    public static PosNetCommand Ftrcfg(string? barcode, int code2dPosition)
    {
        var parameters = new List<KeyValuePair<string, string>>();
        if (barcode is not null)
        {
            parameters.Add(new("bc", barcode));
        }
        parameters.Add(new("bb", code2dPosition.ToString(CultureInfo.InvariantCulture)));
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

    public static PosNetCommand Prncancel() => new("prncancel");
}
