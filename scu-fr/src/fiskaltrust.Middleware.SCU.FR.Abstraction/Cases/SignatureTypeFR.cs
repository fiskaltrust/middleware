using System;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;

/// <summary>
/// FR-localized signature types. The layout follows the v2 convention
/// (<c>0xCCCC_2000_0000_0XXX</c>, "2000" marking the v2 data format); the low word keeps the
/// numbering the v1 QueueFR localization used, so a POS that already renders French signatures
/// keeps recognizing them: 1 = the NF525 token, 3/4/5 = day/month/year totals, 6 = archive
/// totals, 7 = perpetual totals.
/// </summary>
public enum SignatureTypeFR : long
{
    /// <summary>Free-text information (mention légale, "document provisoire", "Duplicata", …).</summary>
    Information = 0x4652_2000_0000_0000,

    /// <summary>The NF525 receipt signature — an ES256 JWS token over the receipt payload.</summary>
    ReceiptSignature = 0x4652_2000_0000_0001,

    /// <summary>Serial number of the certificate the signature was created with.</summary>
    CertificateSerialNumber = 0x4652_2000_0000_0002,

    DayTotals = 0x4652_2000_0000_0003,
    MonthTotals = 0x4652_2000_0000_0004,
    YearTotals = 0x4652_2000_0000_0005,
    ArchiveTotals = 0x4652_2000_0000_0006,
    PerpetualTotals = 0x4652_2000_0000_0007,

    /// <summary>Hash of the previous entry of the same chain — the "inaltérabilité" link.</summary>
    ChainHash = 0x4652_2000_0000_0008,

    /// <summary>SIRET of the establishment the signature creation unit is registered for.</summary>
    Siret = 0x4652_2000_0000_0009,

    /// <summary>The certification body the SCU implements (LNE or Infocert).</summary>
    CertificationBody = 0x4652_2000_0000_000A,

    /// <summary>The receipt was stored but not signed, because no signing SCU is configured.</summary>
    StoredNotSigned = 0x4652_2000_0000_000B,
}

public static class SignatureTypeFRExt
{
    public static T As<T>(this SignatureTypeFR self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);

    public static bool IsType(this SignatureType self, SignatureTypeFR signatureTypeFR) => ((long) self & 0xFFFF) == ((long) signatureTypeFR & 0xFFFF);
    public static SignatureType WithType(this SignatureType self, SignatureTypeFR state) => (SignatureType) ((ulong) self & 0xFFFF_FFFF_FFFF_0000 | (ulong) state & 0xFFFF);
    public static SignatureTypeFR Type(this SignatureType self) => (SignatureTypeFR) ((long) self & 0xFFFF);
}
