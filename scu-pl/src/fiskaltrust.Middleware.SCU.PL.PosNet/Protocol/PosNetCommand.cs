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
    public static PosNetCommand Trline(string name, int vatSlotIndex, long unitPriceGrosze, decimal quantity, long totalGrosze, PosNetModifier? modifier = null)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("na", name),
            new("vt", vatSlotIndex.ToString(CultureInfo.InvariantCulture)),
            new("pr", unitPriceGrosze.ToString(CultureInfo.InvariantCulture)),
        };
        if (quantity != 1m)
        {
            parameters.Add(new("il", quantity.ToString(QuantityFormat, CultureInfo.InvariantCulture)));
        }
        // The line value is what the device measures the discount against — "the discount may not
        // exceed the value of the goods" (POT-I-DEV-05 p.219) — so a line that carries one states
        // its value explicitly instead of leaving the device to derive it from price x quantity.
        if (quantity != 1m || modifier is not null)
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

    public static PosNetCommand Trend(long totalGrosze, long paymentsGrosze, long changeGrosze)
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
        return new PosNetCommand("trend", parameters);
    }

    /// <summary>Prints the buyer's NIP with the receipt footer (paragon z NIP); valid inside an open receipt.</summary>
    public static PosNetCommand Trnipset(string buyerNip) => new("trnipset", [new("ni", buyerNip)]);

    public static PosNetCommand Scomm() => new("scomm");

    public static PosNetCommand Scnt() => new("scnt");

    public static PosNetCommand Prncancel() => new("prncancel");
}
