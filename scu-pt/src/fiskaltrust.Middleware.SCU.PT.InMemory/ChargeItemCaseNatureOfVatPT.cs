using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.PT.Abstraction;

public enum ChargeItemCaseNatureOfVatPT
{
    UsualVatApplies = 0x0000,
    Group0x30 = 0x3000,
    Group0x40 = 0x4000,
}

public static class ChargeItemCaseNatureOfVatPTExt
{
    public static bool IsNatureOfVat(this ChargeItemCase self, ChargeItemCaseNatureOfVatPT natureOfVatPT) => ((long) self & 0xFF00) == (long) natureOfVatPT;
    public static ChargeItemCase WithNatureOfVat(this ChargeItemCase self, ChargeItemCaseNatureOfVatPT natureOfVatPT) => (ChargeItemCase) (((ulong) self & 0xFFFF_FFFF_FFFF_00FF) | (ulong) natureOfVatPT);
    public static ChargeItemCaseNatureOfVatPT NatureOfVat(this ChargeItemCase self) => (ChargeItemCaseNatureOfVatPT) ((long) self & 0xFF00);
}
