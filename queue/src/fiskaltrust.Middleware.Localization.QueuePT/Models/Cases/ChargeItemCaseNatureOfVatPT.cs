using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;

public enum ChargeItemCaseNatureOfVatPT
{
    UsualVatApplies = 0x0000,
}

public static class ChargeItemCaseNatureOfVatPTExt
{
    public static bool IsNatureOfVat(this ChargeItemCase self, ChargeItemCaseNatureOfVatPT natureOfVatPT) => ((long) self & 0xFF00) == (long) natureOfVatPT;
    public static ChargeItemCase WithNatureOfVat(this ChargeItemCase self, ChargeItemCaseNatureOfVatPT natureOfVatPT) => (ChargeItemCase) (((ulong) self & 0xFFFF_FFFF_FFFF_00FF) | (ulong) natureOfVatPT);
    public static ChargeItemCaseNatureOfVatPT NatureOfVat(this ChargeItemCase self) => (ChargeItemCaseNatureOfVatPT) ((long) self & 0xFF00);
}