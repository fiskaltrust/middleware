using System;
using fiskaltrust.ifPOS.v2.es.Cases;

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

        ChargeItemCaseNatureOfVatES.ReverseCharge => new(
            IdOperacionesTrascendenciaTributariaType.Item01,
            DesgloseBranch.NoExenta,
            CausaExencion: null,
            TipoNoExenta: TipoOperacionSujetaNoExentaType.S2,
            CausaNoSujeta: null),

        // NN [30] — Exempted domestic transactions
        ChargeItemCaseNatureOfVatES.ExteptArticle20 => new(
            IdOperacionesTrascendenciaTributariaType.Item01,
            DesgloseBranch.Exenta,
            CausaExencion: CausaExencionType.E1,
            TipoNoExenta: null,
            CausaNoSujeta: null),

        // NN [10] — Exports
        ChargeItemCaseNatureOfVatES.ExteptArticle21 => new(
            IdOperacionesTrascendenciaTributariaType.Item02,
            DesgloseBranch.Exenta,
            CausaExencion: CausaExencionType.E2,
            TipoNoExenta: null,
            CausaNoSujeta: null),

        // NN [13] — Transactions treated as exports
        ChargeItemCaseNatureOfVatES.ExteptArticle22 => new(
            IdOperacionesTrascendenciaTributariaType.Item02,
            DesgloseBranch.Exenta,
            CausaExencion: CausaExencionType.E3,
            TipoNoExenta: null,
            CausaNoSujeta: null),

        // NN [14] — Customs / tax-regulation exemptions
        ChargeItemCaseNatureOfVatES.ExteptArticle23And24 => new(
            IdOperacionesTrascendenciaTributariaType.Item02,
            DesgloseBranch.Exenta,
            CausaExencion: CausaExencionType.E4,
            TipoNoExenta: null,
            CausaNoSujeta: null),

        // NN [11] — Intra-community delivery of goods
        ChargeItemCaseNatureOfVatES.ExteptArticle25 => new(
            IdOperacionesTrascendenciaTributariaType.Item01,
            DesgloseBranch.Exenta,
            CausaExencion: CausaExencionType.E5,
            TipoNoExenta: null,
            CausaNoSujeta: null),

        // NN [31] — Other exemptions
        ChargeItemCaseNatureOfVatES.ExteptOthers => new(
            IdOperacionesTrascendenciaTributariaType.Item01,
            DesgloseBranch.Exenta,
            CausaExencion: CausaExencionType.E6,
            TipoNoExenta: null,
            CausaNoSujeta: null),

        // NN [21] — Not subject (Art. 7, 14, others)
        ChargeItemCaseNatureOfVatES.NotSubjectArticle7and14 => new(
            IdOperacionesTrascendenciaTributariaType.Item01,
            DesgloseBranch.NoSujeta,
            CausaExencion: null,
            TipoNoExenta: null,
            CausaNoSujeta: CausaNoSujetaType.OT),

        // NN [20] — Not subject (localisation rules)
        ChargeItemCaseNatureOfVatES.NotSubjectLocationRules => new(
            IdOperacionesTrascendenciaTributariaType.Item01,
            DesgloseBranch.NoSujeta,
            CausaExencion: null,
            TipoNoExenta: null,
            CausaNoSujeta: CausaNoSujetaType.RL),

        _ => throw new NotSupportedException($"NatureOfVat '{natureOfVat}' is not supported by the TicketBAI mapping.")
    };
}
