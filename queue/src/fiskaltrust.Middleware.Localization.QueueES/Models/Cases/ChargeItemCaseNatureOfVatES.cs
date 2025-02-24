using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueES.Models.Cases;

public enum ChargeItemCaseNatureOfVatES : long
{
    UsualVatApplies = 0x0000,

    NotSubjectArticle7and14 = 0x2000,
    NotSubjectLocationRules = 0x2100,

    ExteptArticle20 = 0x3000,
    ExteptArticle21 = 0x3100,
    ExteptArticle22 = 0x3200,
    ExteptArticle23And24 = 0x3300,
    ExteptArticle25 = 0x3400,
    ExteptOthers = 0x3500,

    ReverseCharge = 0x5000
}

public static class ChargeItemCaseNatureOfVatESExt
{
    public static bool IsNatureOfVat(this ChargeItemCase self, ChargeItemCaseNatureOfVatES natureOfVatES) => ((long) self & 0xFF00) == (long) natureOfVatES;
    public static ChargeItemCase WithNatureOfVat(this ChargeItemCase self, ChargeItemCaseNatureOfVatES state) => (ChargeItemCase) (((ulong) self & 0xFFFF_FFFF_FFFF_00FF) | (ulong) state);
    public static ChargeItemCaseNatureOfVatES NatureOfVat(this ChargeItemCase self) => (ChargeItemCaseNatureOfVatES) ((long) self & 0xFF00);
}