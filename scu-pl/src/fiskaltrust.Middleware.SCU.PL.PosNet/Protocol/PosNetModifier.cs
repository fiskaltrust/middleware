using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

/// <summary>
/// A rabat (discount) or narzut (markup) in the shape the POSNET protocol carries it: a positive
/// amount in grosze plus the direction, because the wire has no sign — the <c>rd</c> flag decides
/// whether the register subtracts or adds the value (POT-I-DEV-05 p.219: "rd Discount(true)/
/// markup(false)").
/// </summary>
/// <remarks>
/// Only the amount form (<c>rw</c>) is expressed here. The protocol also has a percentage form
/// (<c>rp</c>), but a fiskaltrust receipt carries a discount as an amount: deriving a percentage
/// from it would print a rate on the fiscal document that the POS never sent, and where the two
/// fields are sent together the device reads the percentage as the authoritative one.
/// </remarks>
public sealed class PosNetModifier
{
    public PosNetModifier(bool isDiscount, long amountGrosze, string? name = null)
    {
        if (amountGrosze <= 0)
        {
            throw new PLValidationException(
                $"A discount or extra must carry a positive amount (got {amountGrosze} gr) — the direction travels in the rd flag, not in the sign.");
        }
        IsDiscount = isDiscount;
        AmountGrosze = amountGrosze;
        Name = name;
    }

    /// <summary>True for a rabat, false for a narzut — the <c>rd</c> parameter.</summary>
    public bool IsDiscount { get; }

    /// <summary>The amount in grosze, always positive — the <c>rw</c> parameter.</summary>
    public long AmountGrosze { get; }

    /// <summary>What the modifier does to the value it is granted on: minus the amount for a rabat, plus it for a narzut.</summary>
    public long SignedAmountGrosze => IsDiscount ? -AmountGrosze : AmountGrosze;

    /// <summary>The name printed next to the discount line, if any — <c>rn</c> / <c>na</c>.</summary>
    public string? Name { get; }
}
