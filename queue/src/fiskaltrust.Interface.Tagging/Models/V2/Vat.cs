using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [CaseExtensions(OnType = typeof(ChargeItem), OnField = nameof(ChargeItem.ftChargeItemCase), Mask = 0x000F, CaseName = "Vat")]
    public enum Vat : long
    {
        VatUnknown = 0x0,
        VatNormal = 0x3,
        VatDiscounted1 = 0x1,
        VatDiscounted2 = 0x2,
        VatSpecial1 = 0x4,
        VatSpecial2 = 0x5,
        VatZero = 0x7,
        VatNotTaxable = 0x8,
    }
}