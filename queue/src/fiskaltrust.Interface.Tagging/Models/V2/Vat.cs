using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [CaseExtensions(OnType = typeof(ChargeItem), OnField = nameof(ChargeItem.ftChargeItemCase), Mask = 0x000F, CaseName = "ChargeItemCaseVat", Prefix = "V2")]
    public enum Vat : long
    {
        Unknown0x0 = 0x0,
        Normal0x3 = 0x3,
        Discounted10x1 = 0x1,
        Discounted20x2 = 0x2,
        Special10x4 = 0x4,
        Special20x5 = 0x5,
        Zero0x7 = 0x7,
        NotTaxable0x8 = 0x8,
    }
}