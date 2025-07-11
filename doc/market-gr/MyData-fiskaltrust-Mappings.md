# myDATA to fiskaltrust Type Mappings

This document outlines the mappings between myDATA (Greek tax authority AADE) specific datatypes and fiskaltrust types based on the actual implementation in `AADEMappings.cs`.

## fiskaltrust Hexcode Structure

The fiskaltrust hexcodes follow a specific 64-bit structure for Greek localization:

```
0x4752_2000_0000_0000
  ^^^^                Country Code (GR = 0x4752)
       ^^^^           Version (v2 = 0x2000)
            ^^^^      Flags/Nature/Service Type
                 ^^^^ Base Case
```

### Country Code
- `0x4752` = Greece (GR in ASCII hex)

### Version
- `0x2000` = Version 2 of the interface

### Flags and Modifiers
- **Receipt Flags**: `0x0010_0000` = Refund flag
- **Service Types**: 
  - `0x0100_0000` = Other Service
  - `0x0200_0000` = Not Own Sales  
  - `0x0300_0000` = Receivable
- **Nature of VAT**: Encoded in the third segment (e.g., `0x1100`, `0x3100`, `0x8100`)

### Base Cases
Common base cases include:
- `0x0001` = Point of Sale Receipt
- `0x0002` = Payment Transfer  
- `0x0003` = Receipt Without Obligation
- `0x0005` = Delivery Note
- `0x1001` = Invoice
- `0x3003` = Internal Usage
- `0x3004` = Order
- `0x3005` = Payment

**Note**: `xxxx` in VAT exemption hexcodes represents the base VAT case (e.g., `0013` for normal VAT rate).

## 1. VAT Category Mappings

| fiskaltrust VAT Type | fiskaltrust Hexcode | myDATA VAT Category | VAT Rate | Description |
|---------------------|-------------------|---------------------|----------|-------------|
| `ChargeItemCase.NormalVatRate` | `0x4752_2000_0000_0013` | `MyDataVatCategory.VatRate24` (1) | 24% | Normal VAT rate |
| `ChargeItemCase.DiscountedVatRate1` | `0x4752_2000_0000_0093` | `MyDataVatCategory.VatRate13` (2) | 13% | Discounted VAT rate 1 |
| `ChargeItemCase.DiscountedVatRate2` | `0x4752_2000_0000_0053` | `MyDataVatCategory.VatRate6` (3) | 6% | Discounted VAT rate 2 |
| `ChargeItemCase.SuperReducedVatRate1` | `0x4752_2000_0000_00D3` | `MyDataVatCategory.VatRate17` (4) | 17% | Super reduced VAT rate 1 |
| `ChargeItemCase.SuperReducedVatRate2` | `0x4752_2000_0000_0073` | `MyDataVatCategory.VatRate9` (5) | 9% | Super reduced VAT rate 2 |
| `ChargeItemCase.ParkingVatRate` | `0x4752_2000_0000_0033` | `MyDataVatCategory.VatRate4` (6) | 4% | Parking VAT rate |
| `ChargeItemCase.NotTaxable` | `0x4752_2000_0000_6017` | `MyDataVatCategory.ExcludingVat` (7) | 0% | Not taxable |
| `ChargeItemCase.ZeroVatRate` | `0x4752_2000_0000_6027` | `MyDataVatCategory.ExcludingVat` (7) | 0% | Zero VAT rate |

## 2. Payment Method Mappings

| fiskaltrust Payment Type | fiskaltrust Hexcode | myDATA Payment Method | Code | Description |
|--------------------------|-------------------|----------------------|------|-------------|
| `PayItemCase.UnknownPaymentType` | `0x4752_2000_0000_0000` | `MyDataPaymentMethods.Cash` | 3 | Cash payment |
| `PayItemCase.CashPayment` | `0x4752_2000_0000_0001` | `MyDataPaymentMethods.Cash` | 3 | Cash payment |
| `PayItemCase.NonCash` | `0x4752_2000_0000_0002` | `MyDataPaymentMethods.Cash` | 3 | Non-cash payment |
| `PayItemCase.CrossedCheque` | `0x4752_2000_0000_0003` | `MyDataPaymentMethods.Check` | 4 | Check payment |
| `PayItemCase.DebitCardPayment` | `0x4752_2000_0000_0004` | `MyDataPaymentMethods.PosEPos` | 7 | POS/E-POS payment |
| `PayItemCase.CreditCardPayment` | `0x4752_2000_0000_0005` | `MyDataPaymentMethods.PosEPos` | 7 | POS/E-POS payment |
| `PayItemCase.VoucherPaymentCouponVoucherByMoneyValue` | `0x4752_2000_0000_000F` | `MyDataPaymentMethods.Check` | 4 | Check payment |
| `PayItemCase.OnlinePayment` | `0x4752_2000_0000_0006` | `MyDataPaymentMethods.WebBanking` | 6 | Web banking payment |
| `PayItemCase.AccountsReceivable` | `0x4752_2000_0000_0009` | `MyDataPaymentMethods.OnCredit` | 5 | On credit payment |
| `PayItemCase.SEPATransfer` (IRIS) | `0x4752_2000_0000_0007` | `MyDataPaymentMethods.IrisDirectPayments` | 8 | IRIS direct payments |
| `PayItemCase.LoyaltyProgramCustomerCardPayment` | `0x4752_2000_0000_0008` | -1 | N/A | Not supported |
| `PayItemCase.SEPATransfer` (non-IRIS) | `0x4752_2000_0000_0007` | -1 | N/A | Not supported |
| `PayItemCase.OtherBankTransfer` | `0x4752_2000_0000_000A` | -1 | N/A | Not supported |
| `PayItemCase.TransferToCashbookVaultOwnerEmployee` | `0x4752_2000_0000_000B` | -1 | N/A | Not supported |
| `PayItemCase.InternalMaterialConsumption` | `0x4752_2000_0000_000C` | -1 | N/A | Not supported |
| `PayItemCase.Grant` | `0x4752_2000_0000_000D` | -1 | N/A | Not supported |
| `PayItemCase.TicketRestaurant` | `0x4752_2000_0000_000E` | -1 | N/A | Not supported |

## 3. Income Classification Category Mappings

| fiskaltrust Service Type | fiskaltrust Hexcode | myDATA Classification Category | Description |
|--------------------------|-------------------|------------------------------|-------------|
| `ChargeItemCaseTypeOfService.UnknownService` | `0x4752_2000_0000_0000` | `IncomeClassificationCategoryType.category1_1` | Revenue from Sales of Goods |
| `ChargeItemCaseTypeOfService.Delivery` | `0x4752_2000_0000_0000` | `IncomeClassificationCategoryType.category1_1` | Revenue from Sales of Goods |
| `ChargeItemCaseTypeOfService.OtherService` | `0x4752_2000_0100_0000` | `IncomeClassificationCategoryType.category1_3` | Revenue from Sales of Services |
| `ChargeItemCaseTypeOfService.NotOwnSales` | `0x4752_2000_0200_0000` | `IncomeClassificationCategoryType.category1_7` | Revenue for third parties |
| `ReceiptCase.InternalUsageMaterialConsumption0x3003` | `0x4752_2000_0000_3003` | `IncomeClassificationCategoryType.category1_6` | Self-delivery / Self-use |
| `ReceiptCase.Order0x3004` | `0x4752_2000_0000_3004` | `IncomeClassificationCategoryType.category1_95` | Other revenue Information |

## 4. Income Classification Value Type Mappings

| fiskaltrust Receipt/Service Type | fiskaltrust Hexcode | Conditions | myDATA Classification Value |
|----------------------------------|-------------------|------------|----------------------------|
| `ReceiptCase.DeliveryNote0x0005` | `0x4752_2000_0000_0005` | Any | `IncomeClassificationValueType.E3_561_001` |
| `ReceiptCase.Pay0x3005` | `0x4752_2000_0000_3005` | Any | `IncomeClassificationValueType.E3_562` |
| `ReceiptCase.InternalUsageMaterialConsumption0x3003` | `0x4752_2000_0000_3003` | Any | `IncomeClassificationValueType.E3_595` |
| `ChargeItemCaseTypeOfService.NotOwnSales` + Receipt + Greece | `0x4752_2000_0200_1001` | Receipt, Greece | `IncomeClassificationValueType.E3_881_002` |
| `ChargeItemCaseTypeOfService.NotOwnSales` + Receipt + EU | `0x4752_2000_0200_1001` | Receipt, EU | `IncomeClassificationValueType.E3_881_003` |
| `ChargeItemCaseTypeOfService.NotOwnSales` + Receipt + Non-EU | `0x4752_2000_0200_1001` | Receipt, Non-EU | `IncomeClassificationValueType.E3_881_004` |
| `ChargeItemCaseTypeOfService.NotOwnSales` + Invoice + Greece | `0x4752_2000_0200_1001` | Invoice, Greece | `IncomeClassificationValueType.E3_881_001` |
| `ChargeItemCaseTypeOfService.NotOwnSales` + Invoice + EU | `0x4752_2000_0200_1001` | Invoice, EU | `IncomeClassificationValueType.E3_881_003` |
| Invoice + Greece + UnknownService/Delivery/OtherService | `0x4752_2000_0000_1001` | Invoice, Greece | `IncomeClassificationValueType.E3_561_001` |
| Invoice + Greece + Other | `0x4752_2000_0000_1001` | Invoice, Greece | `IncomeClassificationValueType.E3_561_007` |
| Invoice + EU | `0x4752_2000_0000_1001` | Invoice, EU | `IncomeClassificationValueType.E3_561_005` |
| Invoice + Non-EU | `0x4752_2000_0000_1001` | Invoice, Non-EU | `IncomeClassificationValueType.E3_561_006` |
| Receipt + UnknownService/Delivery/OtherService | `0x4752_2000_0000_1001` | Receipt | `IncomeClassificationValueType.E3_561_003` |
| Receipt + Other | `0x4752_2000_0000_1001` | Receipt | `IncomeClassificationValueType.E3_561_007` |

## 5. Invoice Type Mappings

| fiskaltrust Receipt Type | fiskaltrust Hexcode | Conditions | myDATA Invoice Type |
|--------------------------|-------------------|------------|-------------------|
| Receipt + `PaymentTransfer0x0002` + Refund | `0x4752_2010_0000_0002` | Receipt, Payment Transfer, Refund | `InvoiceType.Item85` |
| Receipt + `PaymentTransfer0x0002` | `0x4752_2000_0000_0002` | Receipt, Payment Transfer | `InvoiceType.Item84` |
| Receipt + Refund | `0x4752_2010_0000_0001` | Receipt, Refund | `InvoiceType.Item114` |
| Receipt + `DeliveryNote0x0005` | `0x4752_2000_0000_0005` | Receipt, Delivery Note | `InvoiceType.Item93` |
| Receipt + `PointOfSaleReceiptWithoutObligation0x0003` + Data | `0x4752_2000_0000_0003` | Receipt, Without Obligation, With Data | `InvoiceType.Item32` |
| Receipt + `PointOfSaleReceiptWithoutObligation0x0003` | `0x4752_2000_0000_0003` | Receipt, Without Obligation | `InvoiceType.Item31` |
| Receipt + All NotOwnSales | `0x4752_2000_0200_0001` | Receipt, All Not Own Sales | `InvoiceType.Item115` |
| Receipt + Only Services | `0x4752_2000_0100_0001` | Receipt, Only Services | `InvoiceType.Item112` |
| Receipt + Other | `0x4752_2000_0000_0001` | Receipt, Other | `InvoiceType.Item111` |
| Invoice + Refund + Previous Reference | `0x4752_2010_0000_1001` | Invoice, Refund, With Reference | `InvoiceType.Item51` |
| Invoice + Refund | `0x4752_2010_0000_1001` | Invoice, Refund | `InvoiceType.Item52` |
| Invoice + Receivable | `0x4752_2000_0300_1001` | Invoice, Receivable | `InvoiceType.Item15` |
| Invoice + NotOwnSales | `0x4752_2000_0200_1001` | Invoice, Not Own Sales | `InvoiceType.Item14` |
| Invoice + All OtherService + Previous Reference | `0x4752_2000_0100_1001` | Invoice, All Services, With Reference | `InvoiceType.Item24` |
| Invoice + All OtherService + Greece | `0x4752_2000_0100_1001` | Invoice, All Services, Greece | `InvoiceType.Item21` |
| Invoice + All OtherService + EU | `0x4752_2000_0100_1001` | Invoice, All Services, EU | `InvoiceType.Item22` |
| Invoice + All OtherService + Non-EU | `0x4752_2000_0100_1001` | Invoice, All Services, Non-EU | `InvoiceType.Item23` |
| Invoice + Previous Reference | `0x4752_2000_0000_1001` | Invoice, With Reference | `InvoiceType.Item16` |
| Invoice + Greece | `0x4752_2000_0000_1001` | Invoice, Greece | `InvoiceType.Item11` |
| Invoice + EU | `0x4752_2000_0000_1001` | Invoice, EU | `InvoiceType.Item12` |
| Invoice + Non-EU | `0x4752_2000_0000_1001` | Invoice, Non-EU | `InvoiceType.Item13` |
| Log + `Order0x3004` | `0x4752_2000_0000_3004` | Log, Order | `InvoiceType.Item86` |

## 6. VAT Exemption Category Mappings

| fiskaltrust Nature of VAT | fiskaltrust Hexcode | myDATA Exemption Category | Code | Description |
|---------------------------|-------------------|---------------------------|------|-------------|
| `ChargeItemCaseNatureOfVatGR.NotTaxableIntraCommunitySupplies` | `0x4752_2000_1100_xxxx` | `MyDataVatExemptionCategory.IntraCommunitySupplies` | 14 | Intra-community supplies |
| `ChargeItemCaseNatureOfVatGR.NotTaxableExportOutsideEU` | `0x4752_2000_1200_xxxx` | `MyDataVatExemptionCategory.ExportOutsideEU` | 8 | Export outside EU |
| `ChargeItemCaseNatureOfVatGR.NotTaxableTaxFreeRetail` | `0x4752_2000_1300_xxxx` | `MyDataVatExemptionCategory.TaxFreeRetailNonEU` | 28 | Tax-free retail to non-EU |
| `ChargeItemCaseNatureOfVatGR.NotTaxableArticle39a` | `0x4752_2000_1400_xxxx` | `MyDataVatExemptionCategory.Article39aSpecialRegime` | 16 | Article 39a special regime |
| `ChargeItemCaseNatureOfVatGR.NotTaxableArticle19` | `0x4752_2000_1500_xxxx` | `MyDataVatExemptionCategory.BottleRecyclingTicketsNewspapers` | 6 | Article 19 - Bottles, tickets, newspapers |
| `ChargeItemCaseNatureOfVatGR.NotTaxableArticle22` | `0x4752_2000_1600_xxxx` | `MyDataVatExemptionCategory.MedicalInsuranceBankServices` | 7 | Article 22 - Medical, insurance, bank services |
| `ChargeItemCaseNatureOfVatGR.ExemptArticle43TravelAgencies` | `0x4752_2000_3100_xxxx` | `MyDataVatExemptionCategory.TravelAgencies` | 20 | Article 43 - Travel agencies |
| `ChargeItemCaseNatureOfVatGR.ExemptArticle25CustomsRegimes` | `0x4752_2000_3200_xxxx` | `MyDataVatExemptionCategory.SpecialCustomsRegimes` | 9 | Article 25 - Special customs regimes |
| `ChargeItemCaseNatureOfVatGR.ExemptArticle39SmallBusinesses` | `0x4752_2000_3300_xxxx` | `MyDataVatExemptionCategory.SmallBusinesses` | 15 | Article 39 - Small businesses |
| `ChargeItemCaseNatureOfVatGR.VatPaidOtherEUArticle13` | `0x4752_2000_6100_xxxx` | `MyDataVatExemptionCategory.GoodsOutsideGreece` | 3 | Article 13 - Goods outside Greece |
| `ChargeItemCaseNatureOfVatGR.VatPaidOtherEUArticle14` | `0x4752_2000_6200_xxxx` | `MyDataVatExemptionCategory.ServicesOutsideGreece` | 4 | Article 14 - Services outside Greece |
| `ChargeItemCaseNatureOfVatGR.ExcludedArticle2And3` | `0x4752_2000_8100_xxxx` | `MyDataVatExemptionCategory.GeneralExemption` | 1 | Article 2&3 - General exemption |
| `ChargeItemCaseNatureOfVatGR.ExcludedArticle5BusinessTransfer` | `0x4752_2000_8200_xxxx` | `MyDataVatExemptionCategory.BusinessAssetsTransfer` | 2 | Article 5 - Business transfer |
| `ChargeItemCaseNatureOfVatGR.ExcludedArticle26TaxWarehouses` | `0x4752_2000_8300_xxxx` | `MyDataVatExemptionCategory.TaxWarehouses` | 10 | Article 26 - Tax warehouses |
| `ChargeItemCaseNatureOfVatGR.ExcludedArticle27Diplomatic` | `0x4752_2000_8400_xxxx` | `MyDataVatExemptionCategory.DiplomaticConsularNATO` | 11 | Article 27 - Diplomatic exemptions |

## 7. Customer Information Requirements

| myDATA Invoice Type | Requires Customer Info |
|-------------------|---------------------|
| Item11, Item12, Item13, Item14, Item15, Item16 | ✓ Yes |
| Item21, Item22, Item23, Item24 | ✓ Yes |
| Item31, Item32 | ✓ Yes |
| Item51, Item52 | ✓ Yes |
| Item61, Item62, Item71, Item81 | ✓ Yes |
| Item82, Item84, Item85, Item86 | ✗ No |
| Item111, Item112, Item113, Item114, Item115 | ✗ No |

## myDATA Income Classification Value Types Reference

The following are the complete myDATA income classification value types with their descriptions:

- **E3_561_001**: Sales of goods and services Wholesale - Professionals
- **E3_561_002**: Sales of goods and services Wholesale under article 39a par 5 of the VAT Code (Law 2859/2000)
- **E3_561_003**: Sales of goods and services Retail - Private customers
- **E3_561_004**: Retail sales of goods and services under article 39a par 5 of the VAT Code (Law 2859/2000)
- **E3_561_005**: Foreign sales of goods and services Intra-Community sales
- **E3_561_006**: Foreign sales of goods and services Third countries
- **E3_561_007**: Sales of goods and services Other
- **E3_562**: Other ordinary income
- **E3_595**: Expenditure on own-account production
- **E3_881_001**: Sales for third party accounts Wholesale
- **E3_881_002**: Sales for third party accounts Retail
- **E3_881_003**: Sales for third party accounts Abroad Intra-Community
- **E3_881_004**: Sales for foreign account Third Countries

## myDATA Income Classification Categories Reference

The following are the myDATA income classification categories:

- **category1_1**: Revenue from Sales of Goods (+/-)
- **category1_2**: Revenue from Sales of Products (+/-)
- **category1_3**: Revenue from Sales of Services (+/-)
- **category1_4**: Proceeds from Sale of Assets (+/-)
- **category1_5**: Other income/profit (+/-)
- **category1_6**: Self-delivery / Self-use (+/-)
- **category1_7**: Revenue for third parties (+/-)
- **category1_8**: Revenue from previous years (+/-)
- **category1_9**: Deferred income (+/-)
- **category1_10**: Other revenue adjustment entries (+/-)
- **category1_95**: Other revenue Information (+/-)
- **category3**: Movement

---

*Note: All mappings are based on the actual implementation in the fiskaltrust middleware AADEMappings.cs file and associated model classes. The codes in parentheses represent the numeric values used in the myDATA API.*

*Generated on: July 11, 2025*
*Source: `fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData.AADEMappings.cs`*
