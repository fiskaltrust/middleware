using System.Collections.Generic;
using System.Globalization;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Models;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

/// <summary>
/// Reads the PTU rate table out of the fiscal memory status (<c>sfsk</c>): seven fields
/// <c>va..vg</c>, one per slot A–G, each a percentage with a comma (<c>23,00</c>), <c>100,00</c>
/// for the tax-exempt slot (zw.) and <c>101,00</c> for a slot that is not in use
/// (POT-I-DEV-05 p.261). The register owns this table; the SCU only reads it.
/// </summary>
public static class PosNetRateTable
{
    public const decimal ExemptMarker = 100m;
    public const decimal InactiveMarker = 101m;
    private const int SlotCount = 7;

    /// <summary>The active slots of the table the register reports.</summary>
    /// <exception cref="PLValidationException">The status carries no active rate — a register that sells has at least one.</exception>
    public static List<PLVatRateTableEntry> Parse(PosNetResponse fiscalMemoryStatus)
    {
        var table = new List<PLVatRateTableEntry>();
        for (var i = 0; i < SlotCount; i++)
        {
            var slot = ((char)('A' + i)).ToString();
            if (!fiscalMemoryStatus.Parameters.TryGetValue($"v{(char)('a' + i)}", out var text))
            {
                continue;
            }
            if (!decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            {
                throw new PLValidationException($"The register reports '{text}' for PTU slot {slot}, which is not a rate this SCU can read.");
            }
            if (value == InactiveMarker)
            {
                continue;
            }
            table.Add(value == ExemptMarker
                ? new PLVatRateTableEntry { PtuSlot = slot, IsExempt = true }
                : new PLVatRateTableEntry { PtuSlot = slot, VatRatePercent = value });
        }

        if (table.Count == 0)
        {
            throw new PLValidationException("The register reports no active PTU rate (sfsk) — no sale can be mapped to a slot. Configure VatRateTable to override what the device reports.");
        }
        return table;
    }
}
