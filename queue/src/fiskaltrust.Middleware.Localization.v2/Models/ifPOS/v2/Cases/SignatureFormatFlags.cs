namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

public enum SignatureFormatFlags : long
{
    AfterPayItemBlockBeforeFooter = 0x0_0000,
    AfterHeader = 0x1_0000,
    AfterChargeItemBlock = 0x2_0000,
    AfterTotalTaxBlock = 0x3_0000,
    AfterFooter = 0x4_0000,
    BeforeHeader = 0x5_0000,
}

public static class SignatureFormatFlagsExt
{
    public static SignatureFormat WithFlag(this SignatureFormat self, SignatureFormatFlags flag) => (SignatureFormat) ((long) self | (long) flag);
    public static bool IsFlag(this SignatureFormat self, SignatureFormatFlags flag) => ((long) self & (long) flag) == (long) flag;
}