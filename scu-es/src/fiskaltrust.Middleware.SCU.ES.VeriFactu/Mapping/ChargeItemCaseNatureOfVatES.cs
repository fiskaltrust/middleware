using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Mapping;

/// <summary>
/// VeriFactu-owned "nature of VAT" classification for Spain.
///
/// The numeric values are the on-the-wire <c>ftChargeItemCase</c> nature byte (mask <c>0xFF00</c>),
/// and they follow the <b>NN</b> code scheme from the market-es spec
/// (<see href="https://github.com/fiskaltrust/market-es/issues/95">market-es#95</see>): the NN code
/// in the spec table <i>is</i> the nature byte, e.g. NN[10] (exports) = <c>0x1000</c>, NN[20]
/// (not subject — localisation) = <c>0x2000</c>.
///
/// This enum is <b>deliberately decoupled</b> from
/// <c>fiskaltrust.ifPOS.v2.es.Cases.ChargeItemCaseNatureOfVatES</c>: that interface-package enum
/// numbers its members by Spanish-law article (art. 21 → <c>0x3100</c>, …) instead of by the NN
/// scheme, so it lacks the export family (<c>0x10</c>–<c>0x14</c>) entirely and mislabels
/// <c>0x20</c>/<c>0x21</c>/<c>0x31</c>. The interface values are wrong for our purposes and must not
/// be used here — this copy is authoritative for the VeriFactu mapping
/// (see <see cref="VeriFactuMapping"/>). It mirrors the equivalent TicketBAI-owned copy; the two
/// (plus the queue-es copy) are candidates for consolidation into a shared package.
/// </summary>
public enum ChargeItemCaseNatureOfVatES : long
{
    UsualVatApplies = 0x0000,

    Exports = 0x1000,                      // NN [10] — L8A=02, OperacionExenta E2
    IntraCommunityDelivery = 0x1100,       // NN [11] — L8A=01, OperacionExenta E5
    TransactionsTreatedAsExports = 0x1300, // NN [13] — L8A=02, OperacionExenta E3
    CustomsAndTaxExemptions = 0x1400,      // NN [14] — L8A=02, OperacionExenta E4

    NotSubjectLocationRules = 0x2000,      // NN [20] — L8A=01, CalificacionOperacion N2
    NotSubjectArticle7and14 = 0x2100,      // NN [21] — L8A=01, CalificacionOperacion N1

    ExemptedDomestic = 0x3000,             // NN [30] — L8A=01, OperacionExenta E1
    OtherExemptions = 0x3100,              // NN [31] — L8A=01, OperacionExenta E6

    ReverseCharge = 0x5000,                // NN [50] — L8A=01, CalificacionOperacion S2

    // NN [60]/[80]: VeriFactu has no dedicated L9/L10 key; routed via L1 (Impuesto) and reported as
    // not-subject "others" (N1). See VeriFactuMapping.MapNatureOfVatToOperacion.
    ForeignTaxApplies = 0x6000,            // NN [60]
    ExcludedThirdParty = 0x8000,           // NN [80]
}

public static class ChargeItemCaseNatureOfVatESExt
{
    private const long NatureOfVatMask = 0xFF00;

    public static ChargeItemCaseNatureOfVatES NatureOfVatES(this ChargeItemCase self)
        => (ChargeItemCaseNatureOfVatES) ((long) self & NatureOfVatMask);
}
