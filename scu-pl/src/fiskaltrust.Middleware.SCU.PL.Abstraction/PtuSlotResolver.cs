using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Models;

namespace fiskaltrust.Middleware.SCU.PL.Abstraction;

/// <summary>The statutory Polish VAT rates the ftChargeItemCase VAT nibble maps to.</summary>
public static class PLVatRates
{
    public const decimal Normal = 23m;
    public const decimal Reduced1 = 8m;
    public const decimal Reduced2 = 5m;
    public const decimal Zero = 0m;
}

/// <summary>
/// Resolves the PTU slot for a charge item against the VAT rate table reported by the register.
/// The slot letters are never hardcoded: which slot carries which rate (and which slot is the
/// exempt "zw." one) is owned by the device — only the case→rate mapping is statutory.
/// </summary>
public class PtuSlotResolver
{
    private readonly IReadOnlyList<PLVatRateTableEntry> _vatRateTable;

    public PtuSlotResolver(IEnumerable<PLVatRateTableEntry> deviceVatRateTable)
    {
        _vatRateTable = deviceVatRateTable.ToList();
    }

    public PLVatRateTableEntry Resolve(ChargeItemCase chargeItemCase) => chargeItemCase.Vat() switch
    {
        ChargeItemCase.NormalVatRate => ResolveByRate(PLVatRates.Normal),
        ChargeItemCase.DiscountedVatRate1 => ResolveByRate(PLVatRates.Reduced1),
        ChargeItemCase.DiscountedVatRate2 => ResolveByRate(PLVatRates.Reduced2),
        ChargeItemCase.ZeroVatRate => ResolveByRate(PLVatRates.Zero),
        ChargeItemCase.NotTaxable => ResolveExempt(),
        var vat => throw new PLValidationException($"The VAT case {vat} of ftChargeItemCase 0x{(ulong)chargeItemCase:X} has no Polish PTU mapping."),
    };

    public PLVatRateTableEntry ResolveByRate(decimal vatRatePercent)
        => _vatRateTable.FirstOrDefault(x => !x.IsExempt && x.VatRatePercent == vatRatePercent)
            ?? throw new PLValidationException($"The VAT rate table of the register does not contain a PTU slot for {vatRatePercent}%.");

    public PLVatRateTableEntry ResolveExempt()
        => _vatRateTable.FirstOrDefault(x => x.IsExempt)
            ?? throw new PLValidationException("The VAT rate table of the register does not contain a tax-exempt (zw.) PTU slot.");
}
