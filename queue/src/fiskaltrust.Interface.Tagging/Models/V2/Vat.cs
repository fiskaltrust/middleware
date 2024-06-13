using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [CaseExtensions(OnType = typeof(ChargeItem), OnField = nameof(ChargeItem.ftChargeItemCase), Mask = 0x000F, CaseName = "ChargeItemCase1", Prefix = "V2")]
    public enum Vat : long
    {
        VatUnknown0x0 = 0x0,
        VatNormal0x3 = 0x3,
        VatDiscounted10x1 = 0x1,
        VatDiscounted20x2 = 0x2,
        VatSpecial10x4 = 0x4,
        VatSpecial20x5 = 0x5,
        VatZero0x7 = 0x7,
        VatNotTaxable0x8 = 0x8,
    }
}