using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;

public enum ChargeItemCaseNatureOfVatGR
{
    UsualVatApplies = 0x0000,
    ExtemptEndOfClimateCrises = 0x1100
}

public static class ChargeItemCaseNatureOfVatGRExt
{
    public static bool IsNatureOfVat(this ChargeItemCase self, ChargeItemCaseNatureOfVatGR natureOfVatGR) => ((long) self & 0xFF00) == (long) natureOfVatGR;
    public static ChargeItemCase WithNatureOfVat(this ChargeItemCase self, ChargeItemCaseNatureOfVatGR natureOfVatGR) => (ChargeItemCase) (((ulong) self & 0xFFFF_FFFF_FFFF_00FF) | (ulong) natureOfVatGR);
    public static ChargeItemCaseNatureOfVatGR NatureOfVat(this ChargeItemCase self) => (ChargeItemCaseNatureOfVatGR) ((long) self & 0xFF00);
}