namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

public enum SignatureTypeCategory
{
    Uncategorized = 0x0000,
    Information = 0x1000,
    Alert = 0x2000,
    Failure = 0x3000,
}

public static class SignatureTypeCategoryExt
{
    public static SignatureType WithCategory(this SignatureType self, SignatureTypeCategory category) => (SignatureType) (((ulong) self & 0xFFFF_FFFF_FFFF_0FFF) | (ulong) category);
    public static bool IsCategory(this SignatureType self, SignatureTypeCategory category) => ((long) self & 0xF000) == (long) category;
}