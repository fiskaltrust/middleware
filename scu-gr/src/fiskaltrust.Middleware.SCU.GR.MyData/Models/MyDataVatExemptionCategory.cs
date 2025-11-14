#pragma warning disable

namespace fiskaltrust.Middleware.SCU.GR.MyData.Models;

/// <summary>
/// VAT exemption categories according to AADE MyData API requirements.
/// These categories are used to specify the reason for VAT exemption.
/// </summary>
public static class MyDataVatExemptionCategory
{
    /// <summary>
    /// General exemption category used for most standard exemptions.
    /// Article 2&3 - Transactions outside the scope of VAT.
    /// </summary>
    public const int GeneralExemption = 1;
    
    /// <summary>
    /// Article 5 - Transfer of business assets.
    /// </summary>
    public const int BusinessAssetsTransfer = 2;
    
    /// <summary>
    /// Article 13 - Sales of goods located outside Greece during sale.
    /// </summary>
    public const int GoodsOutsideGreece = 3;
    
    /// <summary>
    /// Article 14 - Services taxed outside Greece including digital services.
    /// </summary>
    public const int ServicesOutsideGreece = 4;
    
    /// <summary>
    /// Article 19 - Bottle package recycling, sales of tickets, newspapers.
    /// </summary>
    public const int BottleRecyclingTicketsNewspapers = 6;
    
    /// <summary>
    /// Article 22 - Medical services, insurance, bank services, 1st residence sale.
    /// </summary>
    public const int MedicalInsuranceBankServices = 7;
    
    /// <summary>
    /// Article 24 - Export of goods outside of EU.
    /// </summary>
    public const int ExportOutsideEU = 8;
    
    /// <summary>
    /// Article 25 - Exemptions under special customs regimes.
    /// </summary>
    public const int SpecialCustomsRegimes = 9;
    
    /// <summary>
    /// Article 26 - Tax warehouses sales.
    /// </summary>
    public const int TaxWarehouses = 10;
    
    /// <summary>
    /// Article 27 - Diplomatic, consular, NATO exemptions.
    /// </summary>
    public const int DiplomaticConsularNATO = 11;

    /// <summary>
    /// Article 32 - Open seas ships.
    /// </summary>
    public const int OpenSeasShips = 12;

    /// <summary>
    /// Article 32.1 - Open seas ships (specific case).
    /// </summary>
    public const int OpenSeasShipsArticle32_1 = 13;
    
    /// <summary>
    /// Article 14 - Intra-community supplies.
    /// </summary>
    public const int IntraCommunitySupplies = 14;
    
    /// <summary>
    /// Article 39 - Special regime for small businesses (below 10K invoices).
    /// </summary>
    public const int SmallBusinesses = 15;
    
    /// <summary>
    /// Article 39a - Special regime of payment of tax from receiver.
    /// </summary>
    public const int Article39aSpecialRegime = 16;

    /// <summary>
    /// Special regime for farmers (article 41).
    /// </summary>
    public const int SpecialRegimeFarmers = 18;

    /// <summary>
    /// Article54 The delivery, intra-Community acquisition and im-port of investment gold, including in-vestment gold for which there are certificates, by type or by type or which is the sub-ject of a transac-tion between gold accounts, includ-ing, in particular, gold loans and swaps, with a right of ownership or claim to invest-ment gold, as well as investment gold transactions with futures and for-ward contracts, which cause a change of owner-ship or claim to in-vestment gold,
    /// </summary>
    public const int Article54InterCommunityDeliveryReverseCharge = 19;

    /// <summary>
    /// Article 43 - Special regime for travel agencies.
    /// </summary>
    public const int TravelAgencies = 20;

    /// <summary>
    /// Article 52 - Special taxa-tion regime for tax-able resellers who deliver second-hand goods and objects of artistic, collector's or ar-chaeological value
    /// </summary>
    public const int MarginScheme = 22;

    /// <summary>
    /// Special case where you don't pay VAT as long as the goods or services are intended for another EU state or 3rd party country.
    /// </summary>
    public const int GoodsServicesForEUOrThirdCountry = 26;

    /// <summary>
    /// Other exemption cases not covered by specific categories.
    /// </summary>
    public const int OtherExemptionCases = 27;

    /// <summary>
    /// TAXFREE retail to non EU citizens.
    /// </summary>
    public const int TaxFreeRetailNonEU = 28;

    /// <summary>
    /// ΠΟΛ.1029/1995 - Specific tax regulation exemption.
    /// </summary>
    public const int POL_1029_1995 = 87;
    
    // Additional exemption categories can be added here as needed
    // based on Greek tax law and AADE requirements
}