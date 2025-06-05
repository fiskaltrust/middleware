using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

public enum SignatureTypeFlags
{
    ArchivingRequired = 0x0001_0000,

    // These three are actually not flags but a case since 0x3 = 0x1 | 0x2
    VisualizationOptional = 0x0010_0000,
    DontVisualize = 0x0020_0000,
    DigitalReceiptOnly = 0x0030_0000,
}

public static class SignatureTypeFlagsExt
{
    public static SignatureType WithFlag(this SignatureType self, SignatureTypeFlags flag) => (SignatureType) ((long) self | (long) flag);
    public static bool IsFlag(this SignatureType self, SignatureTypeFlags flag) => ((long) self & (long) flag) == (long) flag;
}