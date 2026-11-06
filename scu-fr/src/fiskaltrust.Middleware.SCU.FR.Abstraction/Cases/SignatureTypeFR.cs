using System;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;

/// <summary>
/// FR-localized signature types. The layout follows the v2 convention
/// (<c>0xCCCC_2000_0000_0XXX</c>, "2000" marking the v2 data format); the low word keeps the
/// numbering the v1 QueueFR localization used, so a POS that already renders French signatures
/// keeps recognizing them: 1 = the NF525 QR code, 2 = shift totals (reserved), 3/4/5 = day/month/year totals, 6 = archive
/// totals, 7 = perpetual totals.
/// </summary>
public enum SignatureTypeFR : long
{
    /// <summary>Free-text information (mention légale, "document provisoire", "Duplicata", …).</summary>
    Information = 0x4652_2000_0000_0000,

    /// <summary>
    /// The receipt QR code (format QR code, caption <c>www.fiskaltrust.fr</c>, as in v1.2): the signed
    /// content and its signature — a compact ES256 JWS for Infocert, <c>&lt;data set&gt;.&lt;signature&gt;</c>
    /// for LNE and the self-certified SCU (<see cref="Helpers.FRReceiptQrCode"/>). The next entry of the
    /// same chain covers the signature after the last <c>.</c> (ADR 0001).
    /// </summary>
    ReceiptSignature = 0x4652_2000_0000_0001,

    /// <summary>
    /// v1.2's Shift Totals item. Not emitted by v2; reserved so the low word keeps its v1.2 meaning.
    /// The certificate serial number has no item of its own: it is part of the signed content in the
    /// QR code, as in v1.2 (<c>csn</c>).
    /// </summary>
    ShiftTotals = 0x4652_2000_0000_0002,

    DayTotals = 0x4652_2000_0000_0003,
    MonthTotals = 0x4652_2000_0000_0004,
    YearTotals = 0x4652_2000_0000_0005,
    ArchiveTotals = 0x4652_2000_0000_0006,
    PerpetualTotals = 0x4652_2000_0000_0007,

    /// <summary>
    /// Reserved: the chain hash item of the first QueueFR.v2 drafts. Not emitted since ADR 0001
    /// (market-fr#122) — an entry chains on the previous <see cref="ReceiptSignature"/> itself.
    /// The value stays allocated so it is never reused for something else.
    /// </summary>
    ChainHash = 0x4652_2000_0000_0008,

    /// <summary>SIRET of the establishment the signature creation unit is registered for.</summary>
    Siret = 0x4652_2000_0000_0009,

    /// <summary>The certification body the SCU implements (LNE or Infocert). The self-certified SCU emits no such item; it reports "SelfCertified" as <c>CertificationBody</c> in its GetInfo <c>InfoData</c>.</summary>
    CertificationBody = 0x4652_2000_0000_000A,

    /// <summary>The receipt was stored but not signed, because no signing SCU is configured.</summary>
    StoredNotSigned = 0x4652_2000_0000_000B,

    /// <summary>
    /// One line of the receipt's VAT summary (Infocert R13 v4.2 <i>Récapitulatif</i>): amount before
    /// VAT, VAT and amount including VAT of one rate. Printed, so a signature item (ADR 0011);
    /// emitted by the Infocert SCU only.
    /// </summary>
    VatSummary = 0x4652_2000_0000_000C,
}

public static class SignatureTypeFRExt
{
    public static T As<T>(this SignatureTypeFR self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);

    public static bool IsType(this SignatureType self, SignatureTypeFR signatureTypeFR) => ((long) self & 0xFFFF) == ((long) signatureTypeFR & 0xFFFF);
    public static SignatureType WithType(this SignatureType self, SignatureTypeFR state) => (SignatureType) ((ulong) self & 0xFFFF_FFFF_FFFF_0000 | (ulong) state & 0xFFFF);
    public static SignatureTypeFR Type(this SignatureType self) => (SignatureTypeFR) ((long) self & 0xFFFF);
}
