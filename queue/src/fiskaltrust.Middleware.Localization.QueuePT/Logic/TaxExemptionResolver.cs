using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic;

/// <summary>
/// Resolves the Portuguese tax exemption code of a charge item.
/// An explicit ftChargeItemCaseData.PT.TaxExemptionCode wins; otherwise the code is read from the nature of VAT (NN) of ftChargeItemCase,
/// where the NN byte holds the digits of the M-code (e.g. 0x06 = M06).
/// </summary>
public static class TaxExemptionResolver
{
    /// <summary>
    /// A charge item is exempt when it carries an exemption code, either in the nature of VAT (NN) or in ftChargeItemCaseData.PT.TaxExemptionCode.
    /// For exempt charge items the VAT category (V) of ftChargeItemCase is ignored.
    /// </summary>
    public static bool IsExempt(ChargeItem chargeItem)
        => chargeItem.ftChargeItemCase.NatureOfVat() != ChargeItemCaseNatureOfVatPT.UsualVatApplies || TryGetTaxExemptionCodeOverride(chargeItem, out _);

    /// <summary>
    /// A charge item is not subject to VAT (SAF-T TaxType/TaxCode "NS") when it uses the not taxable VAT category (V = 8) without an exemption code.
    /// With an exemption code (NN or ftChargeItemCaseData.PT.TaxExemptionCode) the charge item is exempt (TaxCode "ISE"), whatever its VAT category.
    /// </summary>
    public static bool IsNotSubject(ChargeItem chargeItem)
        => chargeItem.ftChargeItemCase.Vat() == ChargeItemCase.NotTaxable && !IsExempt(chargeItem);

    public static bool TryGetTaxExemptionCodeOverride(ChargeItem chargeItem, out string? taxExemptionCode)
    {
        taxExemptionCode = null;
        if (chargeItem.ftChargeItemCaseData is null)
        {
            return false;
        }

        try
        {
            var json = chargeItem.ftChargeItemCaseData as string ?? JsonSerializer.Serialize(chargeItem.ftChargeItemCaseData);
            var payload = JsonSerializer.Deserialize<ftChargeItemCaseDataPayload>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            taxExemptionCode = payload?.PT?.TaxExemptionCode;
        }
        catch (Exception)
        {
            return false;
        }

        return taxExemptionCode is not null;
    }

    public static bool TryResolve(ChargeItem chargeItem, out Constants.TaxExemptionCode taxExemptionCode)
    {
        if (TryGetTaxExemptionCodeOverride(chargeItem, out var overrideCode))
        {
            return TryParseTaxExemptionCode(overrideCode, out taxExemptionCode);
        }

        taxExemptionCode = (Constants.TaxExemptionCode) (int) chargeItem.ftChargeItemCase.NatureOfVat();
        return Constants.TaxExemptionDictionary.TaxExemptionTable.ContainsKey(taxExemptionCode);
    }

    /// <summary>
    /// Not subject charge items carry no exemption code themselves, but SAF-T requires one on NS lines: M99 ("Não sujeito ou não tributado").
    /// </summary>
    public static Constants.TaxExemptionInfo? GetTaxExemptionInfo(ChargeItem chargeItem)
    {
        if (IsNotSubject(chargeItem))
        {
            return Constants.TaxExemptionDictionary.TaxExemptionTable[Constants.TaxExemptionCode.M99];
        }

        return TryResolve(chargeItem, out var taxExemptionCode) ? Constants.TaxExemptionDictionary.TaxExemptionTable[taxExemptionCode] : null;
    }

    private static bool TryParseTaxExemptionCode(string? value, out Constants.TaxExemptionCode taxExemptionCode)
    {
        taxExemptionCode = default;
        var normalized = value?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalized) || normalized.Length != 3 || normalized[0] != 'M' || !char.IsDigit(normalized[1]) || !char.IsDigit(normalized[2]))
        {
            return false;
        }

        return Enum.TryParse(normalized, out taxExemptionCode) && Constants.TaxExemptionDictionary.TaxExemptionTable.ContainsKey(taxExemptionCode);
    }
}
