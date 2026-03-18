namespace fiskaltrust.Middleware.Localization.v2.Models.Cases.PT;

public enum TaxExemptionCodePT
{
    M01 = 0x0100,
    M02 = 0x0200,
    M04 = 0x0400,
    M05 = 0x0500,
    M06 = 0x0600,
    M07 = 0x0700,
    M09 = 0x0900,
    M10 = 0x1000,
    M11 = 0x1100,
    M12 = 0x1200,
    M13 = 0x1300,
    M14 = 0x1400,
    M15 = 0x1500,
    M16 = 0x1600,
    M19 = 0x1900,
    M20 = 0x2000,
    M21 = 0x2100,
    M25 = 0x2500,
    M26 = 0x2600,
    M30 = 0x3000,
    M31 = 0x3100,
    M32 = 0x3200,
    M33 = 0x3300,
    M34 = 0x3400,
    M40 = 0x4000,
    M41 = 0x4100,
    M42 = 0x4200,
    M43 = 0x4300,
    M99 = 0x9900
}

public class TaxExemptionInfoPT
{
    public string Code { get; set; } = string.Empty;
    public string Mention { get; set; } = string.Empty;
    public string Law { get; set; } = string.Empty;
}

public static class TaxExemptionDictionaryPT
{
    public static readonly Dictionary<TaxExemptionCodePT, TaxExemptionInfoPT> TaxExemptionTable = new()
    {
        { TaxExemptionCodePT.M01, new TaxExemptionInfoPT { Code = "M01", Mention = "Artigo 16.º, n.º 6 do CIVA", Law = "Artigo 16.º, n.º 6, alíneas a) a d) do CIVA" }},
        { TaxExemptionCodePT.M02, new TaxExemptionInfoPT { Code = "M02", Mention = "Artigo 6.º do Decreto-Lei n.º 198/90, de 19 de junho", Law = "Artigo 6.º do Decreto-Lei n.º 198/90, de 19 de junho" }},
        { TaxExemptionCodePT.M04, new TaxExemptionInfoPT { Code = "M04", Mention = "Isento artigo 13.º do CIVA", Law = "Artigo 13.º do CIVA" }},
        { TaxExemptionCodePT.M05, new TaxExemptionInfoPT { Code = "M05", Mention = "Isento artigo 14.º do CIVA", Law = "Artigo 14.º do CIVA" }},
        { TaxExemptionCodePT.M06, new TaxExemptionInfoPT { Code = "M06", Mention = "Isento artigo 15.º do CIVA", Law = "Artigo 15.º do CIVA" }},
        { TaxExemptionCodePT.M07, new TaxExemptionInfoPT { Code = "M07", Mention = "Isento artigo 9.º do CIVA", Law = "Artigo 9.º do CIVA" }},
        { TaxExemptionCodePT.M09, new TaxExemptionInfoPT { Code = "M09", Mention = "IVA - não confere direito a dedução", Law = "Artigo 62.º alínea b) do CIVA" }},
        { TaxExemptionCodePT.M10, new TaxExemptionInfoPT { Code = "M10", Mention = "IVA – regime de isenção", Law = "Artigo 57.º do CIVA" }},
        { TaxExemptionCodePT.M11, new TaxExemptionInfoPT { Code = "M11", Mention = "Regime particular do tabaco", Law = "Decreto-Lei n.º 346/85, de 23 de agosto" }},
        { TaxExemptionCodePT.M12, new TaxExemptionInfoPT { Code = "M12", Mention = "Regime da margem de lucro – Agências de viagens", Law = "Decreto-Lei n.º 221/85, de 3 de julho" }},
        { TaxExemptionCodePT.M13, new TaxExemptionInfoPT { Code = "M13", Mention = "Regime da margem de lucro – Bens em segunda mão", Law = "Decreto-Lei n.º 199/96, de 18 de outubro" }},
        { TaxExemptionCodePT.M14, new TaxExemptionInfoPT { Code = "M14", Mention = "Regime da margem de lucro – Objetos de arte", Law = "Decreto-Lei n.º 199/96, de 18 de outubro" }},
        { TaxExemptionCodePT.M15, new TaxExemptionInfoPT { Code = "M15", Mention = "Regime da margem de lucro – Objetos de coleção e antiguidades", Law = "Decreto-Lei n.º 199/96, de 18 de outubro" }},
        { TaxExemptionCodePT.M16, new TaxExemptionInfoPT { Code = "M16", Mention = "Isento artigo 14.º do RITI", Law = "Artigo 14.º do RITI" }},
        { TaxExemptionCodePT.M19, new TaxExemptionInfoPT { Code = "M19", Mention = "Outras isenções", Law = "Isenções temporárias determinadas em diploma próprio" }},
        { TaxExemptionCodePT.M20, new TaxExemptionInfoPT { Code = "M20", Mention = "IVA - regime forfetário", Law = "Artigo 59.º-D n.º 2 do CIVA" }},
        { TaxExemptionCodePT.M21, new TaxExemptionInfoPT { Code = "M21", Mention = "IVA – não confere direito à dedução (ou expressão similar)", Law = "Artigo 72.º n.º 4 do CIVA" }},
        { TaxExemptionCodePT.M25, new TaxExemptionInfoPT { Code = "M25", Mention = "Mercadorias à consignação", Law = "Artigo 38.º n.º 1 alínea a) do CIVA" }},
        { TaxExemptionCodePT.M26, new TaxExemptionInfoPT { Code = "M26", Mention = "Isenção de IVA com direito à dedução no cabaz alimentar", Law = "Lei n.º 17/2023, de 14 de abril" }},
        { TaxExemptionCodePT.M30, new TaxExemptionInfoPT { Code = "M30", Mention = "IVA - autoliquidação", Law = "Artigo 2.º n.º 1 alínea i) do CIVA" }},
        { TaxExemptionCodePT.M31, new TaxExemptionInfoPT { Code = "M31", Mention = "IVA - autoliquidação", Law = "Artigo 2.º n.º 1 alínea j) do CIVA" }},
        { TaxExemptionCodePT.M32, new TaxExemptionInfoPT { Code = "M32", Mention = "IVA - autoliquidação", Law = "Artigo 2.º n.º 1 alínea l) do CIVA" }},
        { TaxExemptionCodePT.M33, new TaxExemptionInfoPT { Code = "M33", Mention = "IVA - autoliquidação", Law = "Artigo 2.º n.º 1 alínea m) do CIVA" }},
        { TaxExemptionCodePT.M34, new TaxExemptionInfoPT { Code = "M34", Mention = "IVA - autoliquidação", Law = "Artigo 2.º n.º 1 alínea n) do CIVA" }},
        { TaxExemptionCodePT.M40, new TaxExemptionInfoPT { Code = "M40", Mention = "IVA - autoliquidação", Law = "Artigo 6.º n.º 6 alínea a) do CIVA, a contrário" }},
        { TaxExemptionCodePT.M41, new TaxExemptionInfoPT { Code = "M41", Mention = "IVA - autoliquidação", Law = "Artigo 8.º n.º 3 do RITI" }},
        { TaxExemptionCodePT.M42, new TaxExemptionInfoPT { Code = "M42", Mention = "IVA - autoliquidação", Law = "Decreto-Lei n.º 21/2007, de 29 de janeiro" }},
        { TaxExemptionCodePT.M43, new TaxExemptionInfoPT { Code = "M43", Mention = "IVA - autoliquidação", Law = "Decreto-Lei n.º 362/99, de 16 de setembro" }},
        { TaxExemptionCodePT.M99, new TaxExemptionInfoPT { Code = "M99", Mention = "Não sujeito ou não tributado", Law = "Outras situações de não liquidação do imposto" }},
    };
}
