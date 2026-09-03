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

    public static PosNetCommand Trline(string name, int vatSlotIndex, long unitPriceGrosze, decimal quantity, long totalGrosze)
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
            parameters.Add(new("wa", totalGrosze.ToString(CultureInfo.InvariantCulture)));
        }
        return new PosNetCommand("trline", parameters);
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

    /// <summary>
    /// Binds the <em>next</em> fiscal document to an e-receipt customer identifier (IDZ) — the
    /// printer then emits it as an eDokument (paperless when delivery is confirmed) instead of a
    /// plain paper receipt. Valid only on a fiscalized device (?2034 otherwise); the confirmation
    /// carries <c>ha</c>, the unique eDokument id.
    /// </summary>
    public static PosNetCommand EparagonIdzNext(string customerId) => new("eparagonidznext", [new("id", customerId)]);

    /// <summary>Reads one eDokument buffer record by its unique id (the <c>ha</c> from the binding).</summary>
    public static PosNetCommand EparagonBufferGet(uint eDocumentId) => new("eparagonbufferget", [new("hd", eDocumentId.ToString(CultureInfo.InvariantCulture))]);

    public static PosNetCommand Scomm() => new("scomm");

    public static PosNetCommand Scnt() => new("scnt");

    public static PosNetCommand Prncancel() => new("prncancel");
}
