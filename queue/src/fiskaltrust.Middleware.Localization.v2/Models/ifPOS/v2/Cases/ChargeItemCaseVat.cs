namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

public enum ChargeItemCaseVat
{
    UnknownService = 0,  // Unknown type of service for IT (1.3.45)
    DiscountedVatRate1 = 1,  // Discounted-1 VAT rate (as of 1.1.2022, this is 10%) (1.3.45)
    DiscountedVatRate2 = 2,  // Discounted 2 VAT rate (as of 1.1.2022, this is 5%) (1.3.45)
    NormalVatRate = 3,  // Normal VAT rate (as of 1.1.2022, this is 22%) (1.3.45)
    SuperReducedVatRate1 = 4,  // Super reduced 1 VAT rate (1.3.45)
    SuperReducedVatRate2 = 5,  // Super reduced 2 VAT rate (1.3.45)
    ParkingVatRate = 6,  // Parking VAT rate, Reversal of tax liability (1.3.45)
    ZeroVatRate = 7,  // Zero VAT rate (1.3.45)
    NotTaxable = 8  // Not taxable (for processing, see 0x4954000000000001) (1.3.45)
}
