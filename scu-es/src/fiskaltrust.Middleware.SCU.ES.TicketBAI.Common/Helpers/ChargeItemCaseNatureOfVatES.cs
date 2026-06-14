using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;

/// <summary>
/// TicketBAI-owned "nature of VAT" classification for Spain.
///
/// The numeric values are the on-the-wire <c>ftChargeItemCase</c> nature byte (mask <c>0xFF00</c>),
/// and they follow the <b>NN</b> code scheme from the market-es spec
/// (<see href="https://github.com/fiskaltrust/market-es/issues/95">market-es#95</see> /
/// <see href="https://github.com/fiskaltrust/market-es/issues/97">#97</see>): the NN code in the
/// spec table <i>is</i> the nature byte, e.g. NN[10] (exports) = <c>0x1000</c>, NN[20]
/// (not subject — localisation) = <c>0x2000</c>.
///
/// This enum is <b>deliberately decoupled</b> from
/// <c>fiskaltrust.ifPOS.v2.es.Cases.ChargeItemCaseNatureOfVatES</c>: that interface-package enum
/// numbers its members by Spanish-law article (art. 21 → <c>0x3100</c>, …) instead of by the NN
/// scheme, so it lacks the export family (<c>0x10</c>–<c>0x14</c>) entirely and mislabels
/// <c>0x20</c>/<c>0x21</c>/<c>0x31</c>. The interface values are wrong for our purposes and must not
/// be used here — this copy is authoritative for the TicketBAI mapping
/// (see <see cref="TicketBaiNatureOfVatMapping"/>).
/// </summary>
public enum ChargeItemCaseNatureOfVatES : long
{
    UsualVatApplies = 0x0000,

    // NN [10]–[14] — exempt, export family (recorded under DesgloseTipoOperacion when the recipient
    // is non-domestic; see TicketBaiFactory).
    Exports = 0x1000,                      // NN [10] — L9=02, CausaExencion E2
    IntraCommunityDelivery = 0x1100,       // NN [11] — L9=01, CausaExencion E5
    TransactionsTreatedAsExports = 0x1300, // NN [13] — L9=02, CausaExencion E3
    CustomsAndTaxExemptions = 0x1400,      // NN [14] — L9=02, CausaExencion E4

    // NN [20]/[21] — not subject.
    NotSubjectLocationRules = 0x2000,      // NN [20] — L9=01, Causa RL
    NotSubjectArticle7and14 = 0x2100,      // NN [21] — L9=01, Causa OT

    // NN [30]/[31] — exempt, domestic.
    ExemptedDomestic = 0x3000,             // NN [30] — L9=01, CausaExencion E1
    OtherExemptions = 0x3100,              // NN [31] — L9=01, CausaExencion E6

    ReverseCharge = 0x5000,                // NN [50] — L9=01, TipoNoExenta S2
    ForeignTaxApplies = 0x6000,            // NN [60] — L9=08, Causa IE (IPSI/IGIC)
    ExcludedThirdParty = 0x8000,           // NN [80] — L9=01, Causa VT
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
