namespace fiskaltrust.Middleware.Localization.QueuePT.Logic;

public static class Constants
{
    public enum TaxExemptionCode
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

    public class TaxExemptionInfo
    {
        public string Code { get; set; }           // e.g., "M01"
        public string Mention { get; set; }        // Column 2: Mention on invoice
        public string Law { get; set; }            // Column 3: Applicable law
    }

    public static class TaxExemptionDictionary
    {
        public static Dictionary<TaxExemptionCode, TaxExemptionInfo> TaxExemptionTable = new()
        {
            { TaxExemptionCode.M01, new TaxExemptionInfo {
                Code = "M01",
                Mention = "Artigo 16.º, n.º 6 do CIVA",
                Law = "Artigo 16.º, n.º 6, alíneas a) a d) do CIVA"
            }},
            { TaxExemptionCode.M02, new TaxExemptionInfo {
                Code = "M02",
                Mention = "Artigo 6.º do Decreto-Lei n.º 198/90, de 19 de junho",
                Law = "Artigo 6.º do Decreto-Lei n.º 198/90, de 19 de junho"
            }},
            { TaxExemptionCode.M04, new TaxExemptionInfo {
                Code = "M04",
                Mention = "Isento artigo 13.º do CIVA",
                Law = "Artigo 13.º do CIVA"
            }},
            { TaxExemptionCode.M05, new TaxExemptionInfo {
                Code = "M05",
                Mention = "Isento artigo 14.º do CIVA",
                Law = "Artigo 14.º do CIVA"
            }},
            { TaxExemptionCode.M06, new TaxExemptionInfo {
                Code = "M06",
                Mention = "Isento artigo 15.º do CIVA",
                Law = "Artigo 15.º do CIVA"
            }},
            { TaxExemptionCode.M07, new TaxExemptionInfo {
                Code = "M07",
                Mention = "Isento artigo 9.º do CIVA",
                Law = "Artigo 9.º do CIVA"
            }},
            { TaxExemptionCode.M09, new TaxExemptionInfo {
                Code = "M09",
                Mention = "IVA - não confere direito a dedução",
                Law = "Artigo 62.º alínea b) do CIVA"
            }},
            { TaxExemptionCode.M10, new TaxExemptionInfo {
                Code = "M10",
                Mention = "IVA – regime de isenção",
                Law = "Artigo 57.º do CIVA"
            }},
            { TaxExemptionCode.M11, new TaxExemptionInfo {
                Code = "M11",
                Mention = "Regime particular do tabaco",
                Law = "Decreto-Lei n.º 346/85, de 23 de agosto"
            }},
            { TaxExemptionCode.M12, new TaxExemptionInfo {
                Code = "M12",
                Mention = "Regime da margem de lucro – Agências de viagens",
                Law = "Decreto-Lei n.º 221/85, de 3 de julho"
            }},
            { TaxExemptionCode.M13, new TaxExemptionInfo {
                Code = "M13",
                Mention = "Regime da margem de lucro – Bens em segunda mão",
                Law = "Decreto-Lei n.º 199/96, de 18 de outubro"
            }},
            { TaxExemptionCode.M14, new TaxExemptionInfo {
                Code = "M14",
                Mention = "Regime da margem de lucro – Objetos de arte",
                Law = "Decreto-Lei n.º 199/96, de 18 de outubro"
            }},
            { TaxExemptionCode.M15, new TaxExemptionInfo {
                Code = "M15",
                Mention = "Regime da margem de lucro – Objetos de coleção e antiguidades",
                Law = "Decreto-Lei n.º 199/96, de 18 de outubro"
            }},
            { TaxExemptionCode.M16, new TaxExemptionInfo {
                Code = "M16",
                Mention = "Isento artigo 14.º do RITI",
                Law = "Artigo 14.º do RITI"
            }},
            { TaxExemptionCode.M19, new TaxExemptionInfo {
                Code = "M19",
                Mention = "Outras isenções",
                Law = "Isenções temporárias determinadas em diploma próprio"
            }},
            { TaxExemptionCode.M20, new TaxExemptionInfo {
                Code = "M20",
                Mention = "IVA - regime forfetário",
                Law = "Artigo 59.º-D n.º 2 do CIVA"
            }},
            { TaxExemptionCode.M21, new TaxExemptionInfo {
                Code = "M21",
                Mention = "IVA – não confere direito à dedução (ou expressão similar)",
                Law = "Artigo 72.º n.º 4 do CIVA"
            }},
            { TaxExemptionCode.M25, new TaxExemptionInfo {
                Code = "M25",
                Mention = "Mercadorias à consignação",
                Law = "Artigo 38.º n.º 1 alínea a) do CIVA"
            }},
            { TaxExemptionCode.M26, new TaxExemptionInfo {
                Code = "M26",
                Mention = "Isenção de IVA com direito à dedução no cabaz alimentar",
                Law = "Lei n.º 17/2023, de 14 de abril"
            }},
            { TaxExemptionCode.M30, new TaxExemptionInfo {
                Code = "M30",
                Mention = "IVA - autoliquidação",
                Law = "Artigo 2.º n.º 1 alínea i) do CIVA"
            }},
            { TaxExemptionCode.M31, new TaxExemptionInfo {
                Code = "M31",
                Mention = "IVA - autoliquidação",
                Law = "Artigo 2.º n.º 1 alínea j) do CIVA"
            }},
            { TaxExemptionCode.M32, new TaxExemptionInfo {
                Code = "M32",
                Mention = "IVA - autoliquidação",
                Law = "Artigo 2.º n.º 1 alínea l) do CIVA"
            }},
            { TaxExemptionCode.M33, new TaxExemptionInfo {
                Code = "M33",
                Mention = "IVA - autoliquidação",
                Law = "Artigo 2.º n.º 1 alínea m) do CIVA"
            }},
            { TaxExemptionCode.M34, new TaxExemptionInfo {
                Code = "M34",
                Mention = "IVA - autoliquidação",
                Law = "Artigo 2.º n.º 1 alínea n) do CIVA"
            }},
            { TaxExemptionCode.M40, new TaxExemptionInfo {
                Code = "M40",
                Mention = "IVA - autoliquidação",
                Law = "Artigo 6.º n.º 6 alínea a) do CIVA, a contrário"
            }},
            { TaxExemptionCode.M41, new TaxExemptionInfo {
                Code = "M41",
                Mention = "IVA - autoliquidação",
                Law = "Artigo 8.º n.º 3 do RITI"
            }},
            { TaxExemptionCode.M42, new TaxExemptionInfo {
                Code = "M42",
                Mention = "IVA - autoliquidação",
                Law = "Decreto-Lei n.º 21/2007, de 29 de janeiro"
            }},
            { TaxExemptionCode.M43, new TaxExemptionInfo {
                Code = "M43",
                Mention = "IVA - autoliquidação",
                Law = "Decreto-Lei n.º 362/99, de 16 de setembro"
            }},
            { TaxExemptionCode.M99, new TaxExemptionInfo {
                Code = "M99",
                Mention = "Não sujeito ou não tributado",
                Law = "Outras situações de não liquidação do imposto"
            }}
        };

    }
}
