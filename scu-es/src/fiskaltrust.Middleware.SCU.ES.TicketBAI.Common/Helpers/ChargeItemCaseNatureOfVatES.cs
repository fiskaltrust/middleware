using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;

/// <summary>
/// TicketBAI-owned copy of the Spanish "nature of VAT" classification.
///
/// Deliberately decoupled from <c>fiskaltrust.ifPOS.v2.es.Cases.ChargeItemCaseNatureOfVatES</c>:
/// the TicketBAI mapping must not depend on the exempt-reason values shipped in the interface
/// package (they are incomplete — e.g. they lack NN[60]/NN[80] — and not authoritative here).
///
/// The numeric values are the on-the-wire <c>ftChargeItemCase</c> nature byte (mask <c>0xFF00</c>)
/// produced by queue-es and therefore MUST stay in sync with that contract — they are not a free
/// choice. <see cref="ForeignTaxApplies"/> (NN[60]) and <see cref="ExcludedThirdParty"/> (NN[80])
/// are defined for completeness but are not yet translated to a TicketBAI branch
/// (see <see cref="TicketBaiNatureOfVatMapping"/>).
/// </summary>
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
    ReverseCharge = 0x5000,
    ForeignTaxApplies = 0x6000,
    ExcludedThirdParty = 0x8000,
}

public static class ChargeItemCaseNatureOfVatESExt
{
    private const long NatureOfVatMask = 0xFF00;

    public static bool IsNatureOfVatES(this ChargeItemCase self, ChargeItemCaseNatureOfVatES nature)
        => ((long) self & NatureOfVatMask) == (long) nature;

    public static ChargeItemCase WithNatureOfVatES(this ChargeItemCase self, ChargeItemCaseNatureOfVatES nature)
        => (ChargeItemCase) ((ulong) self & 0xFFFF_FFFF_FFFF_00FF | (ulong) nature);

    public static ChargeItemCaseNatureOfVatES NatureOfVatES(this ChargeItemCase self)
        => (ChargeItemCaseNatureOfVatES) ((long) self & NatureOfVatMask);
}
