using System;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;

public enum DesgloseBranch
{
    NoExenta,
    Exenta,
    NoSujeta
}

public record TicketBaiNatureOfVatMappingResult(
    IdOperacionesTrascendenciaTributariaType ClaveRegimen,
    DesgloseBranch Branch,
    CausaExencionType? CausaExencion,
    TipoOperacionSujetaNoExentaType? TipoNoExenta,
    CausaNoSujetaType? CausaNoSujeta);

/// <summary>
/// Maps a Spanish "nature of VAT" (<see cref="ChargeItemCaseNatureOfVatES"/>, keyed on the spec
/// NN byte) to the TicketBAI breakdown codes (L9 ClaveRegimen, L10 CausaExencion, L11 TipoNoExenta,
/// L13 CausaNoSujeta) per the market-es#95 table. The DesgloseFactura vs DesgloseTipoOperacion
/// (recipient) and Entrega vs PrestacionServicios (supply type) decisions are orthogonal to this
/// and are made in <see cref="TicketBaiFactory"/>.
/// </summary>
public static class TicketBaiNatureOfVatMapping
{
    public static TicketBaiNatureOfVatMappingResult Map(ChargeItemCaseNatureOfVatES natureOfVat) => natureOfVat switch
    {
        ChargeItemCaseNatureOfVatES.UsualVatApplies => new(
            IdOperacionesTrascendenciaTributariaType.Item01,
            DesgloseBranch.NoExenta,
            CausaExencion: null,
            TipoNoExenta: TipoOperacionSujetaNoExentaType.S1,
            CausaNoSujeta: null),

        // NN [50] — Reverse charge
        ChargeItemCaseNatureOfVatES.ReverseCharge => new(
            IdOperacionesTrascendenciaTributariaType.Item01,
            DesgloseBranch.NoExenta,
            CausaExencion: null,
            TipoNoExenta: TipoOperacionSujetaNoExentaType.S2,
            CausaNoSujeta: null),

        // NN [10] — Exports
        ChargeItemCaseNatureOfVatES.Exports => new(
            IdOperacionesTrascendenciaTributariaType.Item02,
            DesgloseBranch.Exenta,
            CausaExencion: CausaExencionType.E2,
            TipoNoExenta: null,
            CausaNoSujeta: null),

        // NN [11] — Intra-community delivery of goods
        ChargeItemCaseNatureOfVatES.IntraCommunityDelivery => new(
            IdOperacionesTrascendenciaTributariaType.Item01,
            DesgloseBranch.Exenta,
            CausaExencion: CausaExencionType.E5,
            TipoNoExenta: null,
            CausaNoSujeta: null),

        // NN [13] — Transactions treated as exports
        ChargeItemCaseNatureOfVatES.TransactionsTreatedAsExports => new(
            IdOperacionesTrascendenciaTributariaType.Item02,
            DesgloseBranch.Exenta,
            CausaExencion: CausaExencionType.E3,
            TipoNoExenta: null,
            CausaNoSujeta: null),

        // NN [14] — Exemptions from customs and tax regulations
        ChargeItemCaseNatureOfVatES.CustomsAndTaxExemptions => new(
            IdOperacionesTrascendenciaTributariaType.Item02,
            DesgloseBranch.Exenta,
            CausaExencion: CausaExencionType.E4,
            TipoNoExenta: null,
            CausaNoSujeta: null),

        // NN [30] — Exempted domestic transactions
        ChargeItemCaseNatureOfVatES.ExemptedDomestic => new(
            IdOperacionesTrascendenciaTributariaType.Item01,
            DesgloseBranch.Exenta,
            CausaExencion: CausaExencionType.E1,
            TipoNoExenta: null,
            CausaNoSujeta: null),

        // NN [31] — Other exemptions
        ChargeItemCaseNatureOfVatES.OtherExemptions => new(
            IdOperacionesTrascendenciaTributariaType.Item01,
            DesgloseBranch.Exenta,
            CausaExencion: CausaExencionType.E6,
            TipoNoExenta: null,
            CausaNoSujeta: null),

        // NN [20] — Not subject due to localisation
        ChargeItemCaseNatureOfVatES.NotSubjectLocationRules => new(
            IdOperacionesTrascendenciaTributariaType.Item01,
            DesgloseBranch.NoSujeta,
            CausaExencion: null,
            TipoNoExenta: null,
            CausaNoSujeta: CausaNoSujetaType.RL),

        // NN [21] — Not subject due to Art. 7, 14 and others
        ChargeItemCaseNatureOfVatES.NotSubjectArticle7and14 => new(
            IdOperacionesTrascendenciaTributariaType.Item01,
            DesgloseBranch.NoSujeta,
            CausaExencion: null,
            TipoNoExenta: null,
            CausaNoSujeta: CausaNoSujetaType.OT),

        // NN [60] — Foreign tax applies (IPSI/IGIC). L9=08 per reference sample 021; the #95 table
        // lists 01 but notes the regime depends on context. Confirmed to 08 for the current scope.
        ChargeItemCaseNatureOfVatES.ForeignTaxApplies => new(
            IdOperacionesTrascendenciaTributariaType.Item08,
            DesgloseBranch.NoSujeta,
            CausaExencion: null,
            TipoNoExenta: null,
            CausaNoSujeta: CausaNoSujetaType.IE),

        // NN [80] — Excluded (transactions on behalf of third parties)
        ChargeItemCaseNatureOfVatES.ExcludedThirdParty => new(
            IdOperacionesTrascendenciaTributariaType.Item01,
            DesgloseBranch.NoSujeta,
            CausaExencion: null,
            TipoNoExenta: null,
            CausaNoSujeta: CausaNoSujetaType.VT),

        _ => throw new NotSupportedException($"NatureOfVat '{natureOfVat}' is not supported by the TicketBAI mapping.")
    };
}
