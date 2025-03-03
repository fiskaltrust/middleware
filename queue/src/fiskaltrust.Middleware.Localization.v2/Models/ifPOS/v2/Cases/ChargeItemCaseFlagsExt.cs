namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

public enum ChargeItemCaseFlags : long
{
    Void = 0x0001_0000,
    Refund = 0x0002_0000,
}

public static class ChargeItemCaseFlagsExt
{
    public static ChargeItemCase WithFlag(this ChargeItemCase self, ChargeItemCaseFlags flag) => (ChargeItemCase) ((long) self | (long) flag);

    // HasFlag would be a nicer name but that method does alrady exist for all enums
    public static bool IsFlag(this ChargeItemCase self, ChargeItemCaseFlags flag) => ((long) self & (long) flag) == (long) flag;
}