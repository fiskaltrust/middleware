using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;

public enum ChargeItemCaseNatureOfVatGR
{
    // 00 - usual VAT applies
    UsualVatApplies = 0x0000,

    // 10 - Not Taxable
    NotTaxableIntraCommunitySupplies = 0x1100,           // [11] (mydata:14) non-taxable - intra-community supplies
    NotTaxableExportOutsideEU = 0x1200,                  // [12] (mydata:8) Article 24-Export of goods outside of EU
    NotTaxableTaxFreeRetail = 0x1300,                    // [13] (mydata:28) TAXFREE retail to non eu citizens
    NotTaxableArticle39a = 0x1400,                       // [14] (mydata:16) Article 39a - special regime of payment of tax from receiver
    NotTaxableArticle19 = 0x1500,                        // [15] (mydata:6) Article 19 - Bottle package recycling, sales of tickets, newspapers
    NotTaxableArticle22 = 0x1600,                        // [16] (mydata:7) Article 22 - Medical services, insurance, bank services, 1st residence sale

    // 30 - Exempt
    ExemptArticle43TravelAgencies = 0x3100,              // [31] (mydata:20) Article 43 - Special regime for travel agencies
    ExemptArticle25CustomsRegimes = 0x3200,              // [32] (mydata:9) Article 25 - Exemptions under special customs regimes
    ExemptArticle39SmallBusinesses = 0x3300,             // [33] (mydata:15) Article 39 - Special regime for small businesses (below 10K invoices)

    // 60 - VAT paid in other EU country
    VatPaidOtherEUArticle13 = 0x6100,                   // [61] (mydata:3) Article 13 - Sales of goods located outside Greece during sale
    VatPaidOtherEUArticle14 = 0x6200,                   // [62] (mydata:4) Article 14 - Services taxed outside Greece including digital services

    // 80 - Excluded
    ExcludedArticle2And3 = 0x8100,                      // [81] (mydata:1) Article 2&3 - Transactions outside VAT scope
    ExcludedArticle5BusinessTransfer = 0x8200,          // [82] (mydata:2) Article 5 - Transfer of business assets
    ExcludedArticle26TaxWarehouses = 0x8300,            // [83] (mydata:10) Article 26 - Tax warehouses sales
    ExcludedArticle27Diplomatic = 0x8400,               // [84] (mydata:11) Article 27 - Diplomatic, consular, NATO exemptions
}

public static class ChargeItemCaseNatureOfVatGRExt
{
    public static bool IsNatureOfVat(this ChargeItemCase self, ChargeItemCaseNatureOfVatGR natureOfVatGR) => ((long) self & 0xFF00) == (long) natureOfVatGR;
    public static ChargeItemCase WithNatureOfVat(this ChargeItemCase self, ChargeItemCaseNatureOfVatGR natureOfVatGR) => (ChargeItemCase) (((ulong) self & 0xFFFF_FFFF_FFFF_00FF) | (ulong) natureOfVatGR);
    public static ChargeItemCaseNatureOfVatGR NatureOfVat(this ChargeItemCase self) => (ChargeItemCaseNatureOfVatGR) ((long) self & 0xFF00);
}