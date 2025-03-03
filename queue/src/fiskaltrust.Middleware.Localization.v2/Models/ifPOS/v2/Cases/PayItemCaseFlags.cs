namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

public enum PayItemCaseFlags : long
{
    Void = 0x0001_0000,
    Refund = 0x0002_0000,
    Reserved = 0x0004_0000,
    Downpayment = 0x0008_0000,
    ForeignCurrency = 0x0010_0000,
    Change = 0x0020_0000,
    Tip = 0x0040_0000,
    Electronic = 0x0080_0000,
    Interface = 0x0100_0000,
    ShowInChargeItems = 0x8000_0000,
}

public static class PayItemCaseFlagsExt
{
    public static PayItemCase WithFlag(this PayItemCase self, PayItemCaseFlags flag) => (PayItemCase) ((long) self | (long) flag);

    // HasFlag would be a nicer name but that method does alrady exist for all enums
    public static bool IsFlag(this PayItemCase self, PayItemCaseFlags flag) => ((long) self & (long) flag) == (long) flag;
}

