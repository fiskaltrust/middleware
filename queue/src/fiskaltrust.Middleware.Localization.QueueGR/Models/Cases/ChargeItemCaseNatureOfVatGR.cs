using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueGR.Models.NewFolder;

public enum ChargeItemCaseNatureOfVatGR
{
    // 00 - usual VAT applies
    UsualVatApplies = 0x0000,

    // 10 - Not Taxable
    NotTaxableIntraCommunitySupplies = 0x1100,           // [11] (mydata:14) Article 33. non-tax-able - intra-com-munity sup-plies Χωρίς ΦΠΑ - άρθρο 33 του Κώδικα ΦΠΑ
    NotTaxableExportOutsideEU = 0x1200,                  // [12] (mydata:8) Article 29-Export of goods outside of EU Χωρίς ΦΠΑ - άρθρο 29 του Κώδικα ΦΠΑ
    NotTaxableTaxFreeRetail = 0x1300,                    // [13] (mydata:28) TAXFREE retail to non eu citi-zens Χωρίς ΦΠΑ – άρθρο 29 περ. β' παρ.1 του Κώδικα ΦΠΑ
    NotTaxableArticle39a = 0x1400,                       // [14] (my-data:16) Article 45 Includes the special regime of payment of tax from the receiver of goods and ser-vices NOT THE IS-SUER Χωρίς ΦΠΑ - άρθρο 45 του Κώδικα ΦΠΑ
    NotTaxableArticle19 = 0x1500,                        // [15] (mydata:6) Ar-ticle 24 Bottle package recycling, sales of tickets, sales of newspa-pers and maga-zines Χωρίς ΦΠΑ - άρθρο 24 του Κώδικα ΦΠΑ
    NotTaxableArticle22 = 0x1600,                        // [16] (mydata:7) ar-ticle 27 Services in Greece for medical services, services provided from doc-tors, dentists, in-surance services, bank services, sale of 1st residence Χωρίς ΦΠΑ - άρθρο 27 του Κώδικα ΦΠΑ

    // 30 - Exempt
    ExemptArticle43TravelAgencies = 0x3100,              // [31] (mydata:20) article 50 Com-ments Special re-gime for travel agencies, travel packages taxed in GR. ΦΠΑ εμπεριεχόμενος - άρθρο 50 του Κώδικα ΦΠΑ
    ExemptArticle25CustomsRegimes = 0x3200,              // [32] (mydata:9) article 30 It concerns the exemptions ap-plied under special customs regimes. Goods placed in special customs re-gimes (e.g., cus-toms warehousing, active processing, etc.) are exempt from VAT. Χωρίς ΦΠΑ - άρθρο 30 του Κώδικα ΦΠΑ
    ExemptArticle39SmallBusinesses = 0x3300,             // [33] (mydata:15) Article 44 It con-cerns the special regime for small businesses(below 10K invoices). Χωρίς ΦΠΑ - άρθρο 44 του Κώδικα ΦΠΑ

    MarginSChemeTaxableResellers =  0x4100,              // [41] (my-data:22) Article52 Special taxa-tion regime for tax-able resellers who deliver second-hand goods and objects of artistic, collector's or ar-chaeological value Χωρίς ΦΠΑ εμπεριεχόμενος - άρθρο 52 του Κώδικα ΦΠΑ

    ReverseChargeIntraCommunityDeliveries = 0x5100,     // [51] (my-data:19) Article54 The delivery, intra-Community acquisition and im-port of investment gold, including in-vestment gold for which there are certificates, by type or by type or which is the sub-ject of a transac-tion between gold accounts, includ-ing, in particular, gold loans and swaps, with a right of ownership or claim to invest-ment gold, as well as investment gold transactions with futures and for-ward contracts, which cause a change of owner-ship or claim to in-vestment gold, Χωρίς ΦΠΑ - άρθρο 54 του Κώδικα ΦΠΑ

    // 60 - VAT paid in other EU country
    VatPaidOtherEUArticle13 = 0x6100,                   // [61] (mydata:3) ar-ticle 17 Sales of goods which DUR-ING SALES are lo-cated outside of Greece, sales on boats and or planes during an intra-eu sale Χωρίς ΦΠΑ - άρθρο 17 του Κώδικα ΦΠΑ
    VatPaidOtherEUArticle14 = 0x6200,                   // [62] (mydata:4) ar-ticle 18 Services taxed outside fo Greece including restaurant and ca-tering services pro-vided abroad IN-CLUDING SERVICES provided digitally when the receiver is living abroad. Χωρίς ΦΠΑ - άρθρο 18 του Κώδικα ΦΠΑ

    // 80 - Excluded
    ExcludedArticle2And3 = 0x8100,                      // [81] (mydata:1) Ar-ticle 2&3 Includes transactions out-side the scope of VAT (e.g. compen-sations for material damages, income from participa-tions, subsidies, grants, etc., as well as the special re-gime of Mount Athos. Χωρίς ΦΠΑ - άρθρο 2 και 3 του Κώδικα ΦΠΑ
    ExcludedArticle5BusinessTransfer = 0x8200,          // [82] (mydata:2) Ar-ticle 5 Case of transfer of assets of a business as a) a whole, b) a branch, or c) a part of it through oner-ous or gratuitous cause or in the form of contribu-tion to an existing or newly estab-lished legal entity. Χωρίς ΦΠΑ - άρθρο 5 του Κώδικα ΦΠΑ
    ExcludedArticle26TaxWarehouses = 0x8300,            // [83] (mydata:10) Article 31 Rare case of tax ware-houses sales Χωρίς ΦΠΑ - άρθρο 31 του Κώδικα ΦΠΑ
    ExcludedArticle27Diplomatic = 0x8400,               // [84] (mydata:11) Article 32 It in-cludes exemptions applicable to: cer-tain categories of ships and water-craft and aircraft, for diplomatic and consular authori-ties, recognized in-ternational organi-zations, the Euro-pean Community, the European Cen-tral Bank, etc., NATO and its or-ganizations, to meet the needs of refugees and vul-nerable groups, public donors, etc. for certain trans-ports for Χωρίς ΦΠΑ - άρθρο 32 του Κώδικα ΦΠΑ
}

public static class ChargeItemCaseNatureOfVatGRExt
{
    public static bool IsNatureOfVat(this ChargeItemCase self, ChargeItemCaseNatureOfVatGR natureOfVatGR) => ((long) self & 0xFF00) == (long) natureOfVatGR;
    public static ChargeItemCase WithNatureOfVat(this ChargeItemCase self, ChargeItemCaseNatureOfVatGR natureOfVatGR) => (ChargeItemCase) ((ulong) self & 0xFFFF_FFFF_FFFF_00FF | (ulong) natureOfVatGR);
    public static ChargeItemCaseNatureOfVatGR NatureOfVat(this ChargeItemCase self) => (ChargeItemCaseNatureOfVatGR) ((long) self & 0xFF00);
}