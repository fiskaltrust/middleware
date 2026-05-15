using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using NodaTime;

namespace fiskaltrust.Middleware.SCU.GR.MyData;
public static class AADEMappings
{
    private static readonly DateTimeZone _athensZone = DateTimeZoneProviders.Tzdb["Europe/Athens"];
    public static IncomeClassificationType? GetIncomeClassificationType(ReceiptRequest receiptRequest, ChargeItem chargeItem)
    {
        // Special-tax line items (e.g. plastic-bag environmental fee) carry their tax via
        // feesAmount / stampDutyAmount / otherTaxesAmount / withheldAmount on the row itself —
        // AADE forbids pairing those with an incomeClassification on the same row.
        if (SpecialTaxMappings.IsVatableSpecialTaxItem(chargeItem))
        {
            return null;
        }

        var vatAmount = chargeItem.GetVATAmount();
        var netAmount = receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) ? -chargeItem.Amount - -vatAmount : chargeItem.Amount - vatAmount;

        // Per-item return flag (0x0002) inside an 8.6 order = recType 7
        // myDATA spec: for recType=7 lines amounts MUST be positive; myDATA itself treats them as cancellations
        if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.Order0x3004) && chargeItem.IsRefund())
        {
            netAmount = Math.Abs(netAmount);
        }

        if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.Order0x3004) || receiptRequest.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002))
        {
            return new IncomeClassificationType
            {
                amount = netAmount,
                classificationCategory = IncomeClassificationCategoryType.category1_95
            };
        }

        if(receiptRequest.ftReceiptCase.IsCase(ReceiptCase.DeliveryNote0x0005) && receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsGR.HasTransportInformation))
        {
            return new IncomeClassificationType
            {
                amount = netAmount,
                classificationCategory = IncomeClassificationCategoryType.category3
            };
        }

        if (chargeItem.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.UnknownService))
        {
            return new IncomeClassificationType
            {
                amount = netAmount,
                classificationCategory = IncomeClassificationCategoryType.category1_95
            };
        }

        return new IncomeClassificationType
        {
            amount = netAmount,
            classificationCategory = GetIncomeClassificationCategoryType(receiptRequest, chargeItem),
            classificationType = GetIncomeClassificationValueType(receiptRequest, chargeItem),
            classificationTypeSpecified = true
        };
    }

    /// <summary>
    /// E3_106 Intrinsic production of fixed assets -Self - deliveries - Inventory destruction / Goods
    /// E3_205 Intrinsic reproduction of fixed assets -Self - production - Destruction of inventories / Raw materials and other materials
    /// E3_210 Proprietary assets - Self - production - Inventory destruction / Products and production in progress
    /// E3_305 Intrinsic reproduction of fixed assets -Self - production - Destruction of inventories / Raw materials and other materials
    /// E3_310 Proprietary assets - Self - production - Inventory destruction / Products and production in progress
    /// E3_318 Proprietary production of fixed assets -Self - deliveries - Inventory losses / Production costs
    /// E3_561_001 Sales of goods and services Wholesale - Professionals
    /// E3_561_002 Sales of goods and services Wholesale under article 39a par 5 of the VAT Code(Law 2859 / 2000)
    /// E3_561_003 Sales of goods and services Retail - Private customers
    /// E3_561_004 Retail sales of goods and services under article 39a par 5 of the VAT Code(Law 2859 / 2000)
    /// E3_561_005 Foreign sales of goods and services Intra - Community sales
    /// E3_561_006 Foreign sales of goods and services Third countries
    /// E3_561_007 Sales of goods and services Other
    /// E3_562 Other ordinary income
    /// E3_563 Interest on loans and related income
    /// E3_564 Credit and exchange rate differences
    /// E3_565 Revenue from participations
    /// E3_566 Gains on disposal of non - current assets
    /// E3_567 Gains from reversal of provisions and impairments
    /// E3_568 Gains from measurement at fair value
    /// E3_570 Unusual income and gains
    /// E3_595 Expenditure on own - account production
    /// E3_596 Subsidies - Grants
    /// E3_597 Grants - Grants for investment purposes - Covering expenditure
    /// E3_880_001 Wholesale sales of fixed assets
    /// E3_880_002 Retail sales of fixed assets
    /// E3_880_003 Foreign sales of fixed assets Intra-Community sales
    /// E3_880_004 Sales of Foreign Fixed Assets Third Countries
    /// E3_881_001 Sales for third party accounts Wholesale
    /// E3_881_002 Sales for third party accounts Retail
    /// E3_881_003 Sales for third party accounts Abroad Intra - Community
    /// E3_881_004 Sales for foreign account Third Countries Third Countries
    /// E3_598_001 Sales of goods subject to VAT
    /// E3_598_003 Sales on behalf of farmers through an agricultural cooperative, etc.
    /// </summary>
    public static IncomeClassificationValueType GetIncomeClassificationValueType(ReceiptRequest receiptRequest, ChargeItem chargeItem)
    {
        if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.DeliveryNote0x0005))
        {
            return IncomeClassificationValueType.E3_561_001;
        }
        if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.Pay0x3005))
        {
            return IncomeClassificationValueType.E3_562;
        }
        if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.InternalUsageMaterialConsumption0x3003))
        {
            return IncomeClassificationValueType.E3_595;
        }

        if (chargeItem.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.NotOwnSales))
        {
            if (receiptRequest.ftReceiptCase.IsType(fiskaltrust.ifPOS.v2.Cases.ReceiptCaseType.Receipt))
            {
                if (receiptRequest.GetCustomerCountryCategory() == CustomerCountryCategory.EU)
                {
                    return IncomeClassificationValueType.E3_881_003;
                }
                else if (receiptRequest.GetCustomerCountryCategory() == CustomerCountryCategory.ThirdCountry)
                {
                    return IncomeClassificationValueType.E3_881_004;
                }
                else
                {
                    return IncomeClassificationValueType.E3_881_002;
                }
            }
            else
            {
                if (receiptRequest.GetCustomerCountryCategory() == CustomerCountryCategory.EU)
                {
                    return IncomeClassificationValueType.E3_881_003;
                }
                else if (receiptRequest.GetCustomerCountryCategory() == CustomerCountryCategory.ThirdCountry)
                {
                    return IncomeClassificationValueType.E3_881_004;
                }
                else
                {
                    return IncomeClassificationValueType.E3_881_001;
                }
            }
        }

        if (chargeItem.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.OwnConsumption))
        {
            return IncomeClassificationValueType.E3_595;
        }

        if (receiptRequest.ftReceiptCase.IsType(fiskaltrust.ifPOS.v2.Cases.ReceiptCaseType.Invoice))
        {
            if (receiptRequest.GetCustomerCountryCategory() == CustomerCountryCategory.EU)
            {
                return IncomeClassificationValueType.E3_561_005;
            }
            else if (receiptRequest.GetCustomerCountryCategory() == CustomerCountryCategory.ThirdCountry)
            {
                return IncomeClassificationValueType.E3_561_006;
            }
            else
            {
                if (chargeItem.ftChargeItemCase.IsNatureOfVat(ChargeItemCaseNatureOfVatGR.NotTaxableArticle39a))
                {
                    return IncomeClassificationValueType.E3_561_002;
                }
                
                if (SpecialTaxMappings.IsVatableSpecialTaxItem(chargeItem))
                {
                    return IncomeClassificationValueType.E3_561_001;
                }

                return chargeItem.ftChargeItemCase.TypeOfService() switch
                {
                    ChargeItemCaseTypeOfService.UnknownService => IncomeClassificationValueType.E3_561_001,
                    ChargeItemCaseTypeOfService.Delivery => IncomeClassificationValueType.E3_561_001,
                    ChargeItemCaseTypeOfService.OtherService => IncomeClassificationValueType.E3_561_001,
                    ChargeItemCaseTypeOfService.CatalogService => IncomeClassificationValueType.E3_561_001,
                    _ => IncomeClassificationValueType.E3_561_007,
                };
            }
        }
        else if (receiptRequest.ftReceiptCase.IsType(fiskaltrust.ifPOS.v2.Cases.ReceiptCaseType.Receipt))
        {
            if(chargeItem.ftChargeItemCase.IsNatureOfVat(ChargeItemCaseNatureOfVatGR.NotTaxableArticle39a))
            {
                return IncomeClassificationValueType.E3_561_004;
            }

            return chargeItem.ftChargeItemCase.TypeOfService() switch
            {
                ChargeItemCaseTypeOfService.UnknownService => IncomeClassificationValueType.E3_561_003,
                ChargeItemCaseTypeOfService.Delivery => IncomeClassificationValueType.E3_561_003,
                ChargeItemCaseTypeOfService.OtherService => IncomeClassificationValueType.E3_561_003,
                ChargeItemCaseTypeOfService.CatalogService => IncomeClassificationValueType.E3_561_003,
                _ => IncomeClassificationValueType.E3_561_007,
            };
        }

        if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.Pay0x3005))
        {
            return chargeItem.ftChargeItemCase.TypeOfService() switch
            {
                ChargeItemCaseTypeOfService.UnknownService => IncomeClassificationValueType.E3_561_001,
                _ => IncomeClassificationValueType.E3_561_007,
            };
        }
        return chargeItem.ftChargeItemCase.TypeOfService() switch
        {
            ChargeItemCaseTypeOfService.UnknownService => IncomeClassificationValueType.E3_561_003,
            ChargeItemCaseTypeOfService.Delivery => IncomeClassificationValueType.E3_561_003,
            ChargeItemCaseTypeOfService.OtherService => IncomeClassificationValueType.E3_561_003,
            _ => IncomeClassificationValueType.E3_561_007,
        };
    }

    /// <summary>
    /// The following income classifications belong to myDATA API
    /// 
    /// category1_1 => Revenue from Sales of Goods (+ / -)
    /// category1_2 => Revenue from Sales of Products (+ / -)
    /// category1_3 => Revenue from Sales of Services (+ / -)
    /// category1_4 => Proceeds from Sale of Assets (+ / -)
    /// category1_5 => Other income/profit (+ / -)
    /// category1_6 => Self-delivery / Self-use (+ / -)
    /// category1_7 => Revenue for third parties (+ / -)
    /// category1_8 => Revenue from previous years (+ / -)
    /// category1_9 => Deferred income (+ / -)
    /// category1_10 => Other revenue adjustment entries (+ / -)
    /// category1_95 => Other revenue Information (+ / -)
    /// category3 => Movement        
    /// </summary>
    public static IncomeClassificationCategoryType GetIncomeClassificationCategoryType(ReceiptRequest receiptRequest, ChargeItem chargeItem)
    {
        if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.InternalUsageMaterialConsumption0x3003))
        {
            return IncomeClassificationCategoryType.category1_6;
        }

        if (SpecialTaxMappings.IsVatableSpecialTaxItem(chargeItem))
        {
            return IncomeClassificationCategoryType.category1_1;
        }

        return chargeItem.ftChargeItemCase.TypeOfService() switch
        {
            ChargeItemCaseTypeOfService.UnknownService => IncomeClassificationCategoryType.category1_95,
            ChargeItemCaseTypeOfService.Delivery => IncomeClassificationCategoryType.category1_1,
            ChargeItemCaseTypeOfService.CatalogService => IncomeClassificationCategoryType.category1_2,
            ChargeItemCaseTypeOfService.OtherService => IncomeClassificationCategoryType.category1_3,
            ChargeItemCaseTypeOfService.NotOwnSales => IncomeClassificationCategoryType.category1_7,
            ChargeItemCaseTypeOfService.OwnConsumption => IncomeClassificationCategoryType.category1_6,
            _ => IncomeClassificationCategoryType.category1_95,
        };
    }


    public static DateTime GetLocalTime(ReceiptRequest receiptRequest)
    {
        var iniDateTime = receiptRequest.cbReceiptMoment;

        if (iniDateTime.Kind == DateTimeKind.Local)
        {
            iniDateTime = iniDateTime.ToUniversalTime();
        }
        else if (iniDateTime.Kind == DateTimeKind.Unspecified)
        {
            iniDateTime = DateTime.SpecifyKind(iniDateTime, DateTimeKind.Utc);
        }

        Instant utcInstant = Instant.FromDateTimeUtc(iniDateTime);
        ZonedDateTime zoned = utcInstant.InZone(_athensZone);
        return zoned.ToDateTimeUnspecified();
    }

    public static InvoiceType GetInvoiceType(ReceiptRequest receiptRequest)
    {
        // InvoiceType override is handled in AADEFactory.ApplyInvoiceHeaderOverride after the initial mapping.
        // Here we only determine the base type from the receipt case.

        // Validate that agency business (NotOwnSales) items are not mixed with other types
        var hasNotOwnSales = receiptRequest.cbChargeItems.Any(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.NotOwnSales));
        var hasOtherTypes = receiptRequest.cbChargeItems.Any(x =>
        {
            var typeOfService = x.ftChargeItemCase.TypeOfService();
            return typeOfService != ChargeItemCaseTypeOfService.NotOwnSales &&
                   typeOfService != (ChargeItemCaseTypeOfService)0xF0; // Exclude special tax items
        });

        if (hasNotOwnSales && hasOtherTypes)
        {
            throw new Exception("In Greece, agency business (NotOwnSales) items cannot be mixed with other types of service items in the same receipt.");
        }

        // Validate that own consumption (OwnConsumption) items are not mixed with other types
        var hasOwnConsumption = receiptRequest.cbChargeItems.Any(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.OwnConsumption));
        var hasOtherTypesForOwnConsumption = receiptRequest.cbChargeItems.Any(x =>
        {
            var typeOfService = x.ftChargeItemCase.TypeOfService();
            return typeOfService != ChargeItemCaseTypeOfService.OwnConsumption &&
                   typeOfService != (ChargeItemCaseTypeOfService)0xF0; // Exclude special tax items
        });

        if (hasOwnConsumption && hasOtherTypesForOwnConsumption)
        {
            throw new Exception("In Greece, own consumption (OwnConsumption) items cannot be mixed with other types of service items in the same receipt.");
        }

        if (receiptRequest.ftReceiptCase.IsType(fiskaltrust.ifPOS.v2.Cases.ReceiptCaseType.Receipt))
        {
            // Check if this is a delivery note with transport document flag
            if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.DeliveryNote0x0005))
            {
                if (((long)receiptRequest.ftReceiptCase & (long)ReceiptCaseFlagsGR.HasTransportInformation) != 0)
                {
                    return InvoiceType.Item93;
                }
            }

            if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002))
            {
                return receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) ? InvoiceType.Item85 : InvoiceType.Item84;
            }

            if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
            {
                return InvoiceType.Item114;
            }

            if (hasNotOwnSales)
            {
                return InvoiceType.Item115;
            }
            else if (hasOwnConsumption)
            {
                return InvoiceType.Item61;
            }
            else if (receiptRequest.HasOnlyServiceItemsExcludingSpecialTaxes() || receiptRequest.HasAtLeastOneServiceItemAndOnlyUnknownsExcludingSpecialTaxes())
            {
                return InvoiceType.Item112;
            }
            else
            {
                return InvoiceType.Item111;
            }
        }

        if (receiptRequest.ftReceiptCase.IsType(fiskaltrust.ifPOS.v2.Cases.ReceiptCaseType.Invoice))
        {
            if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
            {
                return HasAnyPreviousInvoiceReference(receiptRequest) ? InvoiceType.Item51 : InvoiceType.Item52;
            }

            if (hasNotOwnSales)
            {
                return InvoiceType.Item14;
            }
            else if (hasOwnConsumption)
            {
                return InvoiceType.Item61;
            }
            else if (receiptRequest.ftReceiptCase.IsType(fiskaltrust.ifPOS.v2.Cases.ReceiptCaseType.Invoice) && receiptRequest.HasOnlyServiceItemsExcludingSpecialTaxes())
            {
                //if (receiptRequest.cbPreviousReceiptReference is not null)
                //{
                //    return InvoiceType.Item24;
                //}

                var customer = receiptRequest.GetCustomerOrNull();
                if (receiptRequest.GetCustomerCountryCategory() == CustomerCountryCategory.EU)
                {
                    return InvoiceType.Item22;
                }
                else if (receiptRequest.GetCustomerCountryCategory() == CustomerCountryCategory.ThirdCountry)
                {
                    return InvoiceType.Item23;
                }
                else
                {
                    return InvoiceType.Item21;
                }
            }
            else
            {
                //if (receiptRequest.cbPreviousReceiptReference is not null)
                //{
                //    return InvoiceType.Item16;
                //}

                var customer = receiptRequest.GetCustomerOrNull();
                if (receiptRequest.GetCustomerCountryCategory() == CustomerCountryCategory.EU)
                {
                    return InvoiceType.Item12;
                }
                else if (receiptRequest.GetCustomerCountryCategory() == CustomerCountryCategory.ThirdCountry)
                {
                    return InvoiceType.Item13;
                }
                else
                {
                    return InvoiceType.Item11;
                }
            }
        }

        if (receiptRequest.ftReceiptCase.IsType(fiskaltrust.ifPOS.v2.Cases.ReceiptCaseType.Log))
        {
            switch (receiptRequest.ftReceiptCase.Case())
            {
                //case ReceiptCase.InternalUsageMaterialConsumption0x3003:
                //    return receiptRequest.HasOnlyServiceItems() ? InvoiceType.Item62 : InvoiceType.Item61;
                case ReceiptCase.Order0x3004:
                    return InvoiceType.Item86;
                    //case (ReceiptCase) 0x3005: // TODO
                    //    return InvoiceType.Item81;
                    //case (ReceiptCase) 0x3006: // TODO
                    //    return InvoiceType.Item71;
            }
        }

        throw new Exception("Unknown type of receipt " + receiptRequest.ftReceiptCase.ToString("x"));
    }

    public static int GetVATCategory(ChargeItem chargeItem)
    {
        if (chargeItem.ftChargeItemCase.Vat() == ChargeItemCase.UnknownService)
        {
            return chargeItem.VATRate switch
            {
                24m => MyDataVatCategory.VatRate24_Category1,
                13m => MyDataVatCategory.VatRate13_Category2,
                6m => MyDataVatCategory.VatRate6_Category3,
                17m => MyDataVatCategory.VatRate17_Category4,
                9m => MyDataVatCategory.VatRate9_Category5,
                4m => MyDataVatCategory.VatRate4_Category6, // TODO maybe we need to distinguish here between 4% Category 6 and 4% Category 10
                0m => MyDataVatCategory.VatRate0_ExcludingVat_Category7, // TODO maybe we need to distinguish here between ExcludingVat and RegistrationsWithoutVat
                3m => MyDataVatCategory.VatRate3_Category9, // 3% VAT (ν.5057/2023)
                _ => throw new Exception($"The VAT Rate {chargeItem.VATRate} with VAT type 0x0000-Unknown of ChargeItem with the case {chargeItem.ftChargeItemCase.ToString("x")} is not supported."),
            };
        }
        else if (chargeItem.ftChargeItemCase.Vat() == ChargeItemCase.NormalVatRate)
        {
            if (chargeItem.VATRate == 24m)
            {
                return MyDataVatCategory.VatRate24_Category1;
            }
            // The VAT rate of 25% with VAT type '0000000000000003' used in the charge item for case '4752200000000013' is not supported. Please verify the VAT configuration or use a supported VAT type."
            throw new Exception($"The Property cbChargeItem.VATRate with the Value '{chargeItem.VATRate}' is invalid for the given Type of VAT '{chargeItem.ftChargeItemCase.Vat().ToString("x")}'.");
        }
        else if (chargeItem.ftChargeItemCase.Vat() == ChargeItemCase.DiscountedVatRate1 || chargeItem.ftChargeItemCase.Vat() == ChargeItemCase.DiscountedVatRate2)
        {
            if (chargeItem.VATRate == 13m)
            {
                return MyDataVatCategory.VatRate13_Category2;
            }
            if (chargeItem.VATRate == 17m)
            {
                return MyDataVatCategory.VatRate17_Category4;
            }
            if (chargeItem.VATRate == 6m)
            {
                return MyDataVatCategory.VatRate6_Category3;
            }
            if (chargeItem.VATRate == 9m)
            {
                return MyDataVatCategory.VatRate9_Category5;
            }
            throw new Exception($"The Property cbChargeItem.VATRate with the Value '{chargeItem.VATRate}' is invalid for the given Type of VAT '{chargeItem.ftChargeItemCase.Vat().ToString("x")}'.");
        }
        else if (chargeItem.ftChargeItemCase.Vat() == ChargeItemCase.SuperReducedVatRate1 || chargeItem.ftChargeItemCase.Vat() == ChargeItemCase.SuperReducedVatRate2)
        {
            if (chargeItem.VATRate == 4m)
            {
                return MyDataVatCategory.VatRate4_Category6;
            }
            throw new Exception($"The Property cbChargeItem.VATRate with the Value '{chargeItem.VATRate}' is invalid for the given Type of VAT '{chargeItem.ftChargeItemCase.Vat().ToString("x")}'.");
        }
        else if (chargeItem.ftChargeItemCase.Vat() == ChargeItemCase.ParkingVatRate)
        {
            //if (chargeItem.VATRate == 3m)
            //{
            //    return MyDataVatCategory.VatRate3_Category9;
            //}
            //else if (chargeItem.VATRate == 4m)
            //{
            //    return MyDataVatCategory.VatRate4_Category10;
            //}
            throw new Exception($"The Property cbChargeItem.VATRate with the Value '{chargeItem.VATRate}' is invalid for the given Type of VAT ' {chargeItem.ftChargeItemCase.Vat().ToString("x")} '.");
        }
        else if (chargeItem.ftChargeItemCase.Vat() == ChargeItemCase.ZeroVatRate)
        {
            if (chargeItem.VATRate == 0m)
            {
                return MyDataVatCategory.VatRate0_ExcludingVat_Category7;
            }
            throw new Exception($"When using VAT type ZeroVatRate {chargeItem.ftChargeItemCase.Vat().ToString("x")} the VATRate has to be 0.00.");
        }
        else if (chargeItem.ftChargeItemCase.Vat() == ChargeItemCase.NotTaxable)
        {
            if (chargeItem.VATRate == 0m)
            {
                return MyDataVatCategory.RegistrationsWithoutVat;
            }
            throw new Exception($"When using VAT type NotTaxable {chargeItem.ftChargeItemCase.Vat().ToString("x")} the VATRate has to be 0.00.");
        }
        else
        {
            throw new Exception($"The VAT type {chargeItem.ftChargeItemCase.Vat().ToString("x")} of ChargeItem with the case {chargeItem.ftChargeItemCase} is not supported.");
        }
    }

    public static int GetPaymentType(PayItem payItem) => payItem.ftPayItemCase.Case() switch
    {
        PayItemCase.UnknownPaymentType => MyDataPaymentMethods.Cash,
        PayItemCase.CashPayment => MyDataPaymentMethods.Cash,
        PayItemCase.NonCash => -1,
        PayItemCase.CrossedCheque => MyDataPaymentMethods.Check,
        PayItemCase.DebitCardPayment => MyDataPaymentMethods.PosEPos,
        PayItemCase.CreditCardPayment => MyDataPaymentMethods.PosEPos,
        PayItemCase.VoucherPaymentCouponVoucherByMoneyValue => MyDataPaymentMethods.Check,
        PayItemCase.OnlinePayment => -1,
        PayItemCase.LoyaltyProgramCustomerCardPayment => -1,
        PayItemCase.AccountsReceivable => MyDataPaymentMethods.OnCredit,
        PayItemCase.SEPATransfer => GetSEPATransferPaymentType(payItem.Description),
        PayItemCase.OtherBankTransfer => MyDataPaymentMethods.ForeignPaymentsSpecialAccount,
        PayItemCase.TransferToCashbookVaultOwnerEmployee => -1,
        PayItemCase.InternalMaterialConsumption => -1,
        PayItemCase.Grant => -1,
        PayItemCase.TicketRestaurant => -1,
        PayItemCase c => throw new Exception($"The Payment type {c} of PayItem with the case {payItem.ftPayItemCase} is not supported."),
    };

    /// <summary>
    /// Determines whether a pay item should be transmitted to the myDATA API as a payment method.
    /// Internal material consumption has no AADE payment-method counterpart and is filtered out
    /// so it never appears in <c>inv.paymentMethods</c> nor in a standalone <c>SendPaymentsMethod</c> call.
    /// </summary>
    public static bool ShouldTransmitPayItem(PayItem payItem) => payItem.ftPayItemCase.Case() switch
    {
        PayItemCase.InternalMaterialConsumption => false,
        _ => true
    };

    private static int GetSEPATransferPaymentType(string description)
    {
        if (string.IsNullOrEmpty(description))
            return MyDataPaymentMethods.DomesticPaymentAccount;

        else if (description.ToUpper().Contains("IRIS"))
        {
            return MyDataPaymentMethods.IrisDirectPayments;
        }
        else if (description.ToUpper().Contains("RF CODE PAYMENT (WEB BANKING)"))
        {
            return MyDataPaymentMethods.WebBanking;
        }
        else if (description.ToUpper().Contains("FOREIGN") || description.ToUpper().Contains("INTERNATIONAL"))
        {
            return MyDataPaymentMethods.ForeignPaymentsSpecialAccount;
        }
        else
        {
            return MyDataPaymentMethods.DomesticPaymentAccount;
        }
    }

    public static bool RequiresCustomerInfo(InvoiceType invoiceType)
    {
        return invoiceType switch
        {
            InvoiceType.Item11 or InvoiceType.Item12 or InvoiceType.Item13 or InvoiceType.Item14 or InvoiceType.Item15 or InvoiceType.Item16 or InvoiceType.Item21 or InvoiceType.Item22 or InvoiceType.Item23 or InvoiceType.Item24 or InvoiceType.Item51 or InvoiceType.Item52 or InvoiceType.Item31 or InvoiceType.Item32 or InvoiceType.Item61 or InvoiceType.Item62 or InvoiceType.Item71 or InvoiceType.Item81 or InvoiceType.Item93 => true,
            InvoiceType.Item82 or InvoiceType.Item84 or InvoiceType.Item86 or InvoiceType.Item85 or InvoiceType.Item111 or InvoiceType.Item112 or InvoiceType.Item113 or InvoiceType.Item114 or InvoiceType.Item115 => false,
            _ => throw new NotSupportedException($"The invoice type '{invoiceType.GetXmlEnumAttributeValueFromEnum()}' is not supported"),
        };
    }

    public static bool SupportsCounterpart(InvoiceType invoiceType) => invoiceType switch
    {
        InvoiceType.Item111 or InvoiceType.Item112 or InvoiceType.Item113 or InvoiceType.Item114 or InvoiceType.Item115 => false,
        _ => true
    };

    public static bool SupportsCorrelatedInvoices(InvoiceType invoiceType) => invoiceType switch
    {
        InvoiceType.Item111 or InvoiceType.Item112 or InvoiceType.Item113 or InvoiceType.Item114 or InvoiceType.Item115 => false,
        _ => true
    };

    /// <summary>
    /// Returns true if the request points to a previous invoice via any of the supported
    /// correlation sources:
    /// <list type="bullet">
    /// <item><c>cbPreviousReceiptReference</c> — an invoice issued by this middleware.</item>
    /// <item><c>ftReceiptCaseData.GR.PreviousReceiptReference.invoiceMark</c> — an invoice issued
    /// by another system, identified by its AADE MARK.</item>
    /// <item><c>ftReceiptCaseData.GR.mydataoverride.invoice.invoiceHeader.correlatedInvoices</c>
    /// or <c>multipleConnectedMarks</c> — marks supplied via the invoice-header override.</item>
    /// </list>
    /// Used by invoice-type selection so e.g. a B2B refund correlated only by an external MARK
    /// (or only via the mydataoverride header) still resolves to the correlated credit-note type
    /// (Item51) instead of the uncorrelated one (Item52). The myDATA spec encodes the
    /// correlation in the type name itself: 5.1 = Συσχετιζόμενο (Correlated),
    /// 5.2 = Μη Συσχετιζόμενο (Non-Correlated). Other invoice types do not require this — they
    /// accept correlatedInvoices as an optional field without changing type.
    /// </summary>
    public static bool HasAnyPreviousInvoiceReference(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.cbPreviousReceiptReference is not null)
        {
            return true;
        }
        return receiptRequest.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayload>(out var caseData)
            && HasPreviousInvoiceReferenceInCaseData(caseData);
    }

    /// <summary>
    /// Returns true if <paramref name="caseData"/> carries any of the two case-data-side
    /// correlation sources accepted by <see cref="HasAnyPreviousInvoiceReference"/>:
    /// <c>PreviousReceiptReference.invoiceMark</c>, or
    /// <c>mydataoverride.invoice.invoiceHeader.correlatedInvoices</c> /
    /// <c>multipleConnectedMarks</c>. Call sites that have already deserialized the payload
    /// (e.g. <c>SetInvoiceHeaderFieldsForVoid</c>) use this to avoid double-deserializing.
    /// </summary>
    public static bool HasPreviousInvoiceReferenceInCaseData(ftReceiptCaseDataPayload? caseData)
    {
        if (caseData?.GR?.PreviousReceiptReference?.InvoiceMark is { Count: > 0 })
        {
            return true;
        }
        var overrideHeader = caseData?.GR?.MyDataOverride?.Invoice?.InvoiceHeader;
        return overrideHeader?.CorrelatedInvoices is { Count: > 0 }
            || overrideHeader?.MultipleConnectedMarks is { Count: > 0 };
    }
    public static bool SupportsMultipleConnectedMarks(InvoiceType invoiceType) => invoiceType switch
    {
        InvoiceType.Item16 or InvoiceType.Item24 or InvoiceType.Item51 => false,
        _ => true
    };

    public static bool SupportsIncomeClassification(InvoiceType invoiceType) => invoiceType switch
    {
        InvoiceType.Item31 or InvoiceType.Item32 => false,
        _ => true
    };

    /// <summary>
    /// Maps ChargeItemCase Nature of VAT to MyData VAT exemption category.
    /// Based on the Greek tax regulations and AADE requirements.
    /// </summary>
    /// <param name="chargeItem">The charge item to evaluate</param>
    /// <returns>The VAT exemption category number, or null if no exemption applies</returns>
    public static int? GetVatExemptionCategory(ChargeItem chargeItem)
    {
        var natureOfVat = chargeItem.ftChargeItemCase.NatureOfVat();
        return natureOfVat switch
        {
            // Not Taxable (10)
            ChargeItemCaseNatureOfVatGR.NotTaxableIntraCommunitySupplies => MyDataVatExemptionCategory.IntraCommunitySupplies,
            ChargeItemCaseNatureOfVatGR.NotTaxableExportOutsideEU => MyDataVatExemptionCategory.ExportOutsideEU,
            ChargeItemCaseNatureOfVatGR.NotTaxableTaxFreeRetail => MyDataVatExemptionCategory.TaxFreeRetailNonEU,
            ChargeItemCaseNatureOfVatGR.NotTaxableArticle39a => MyDataVatExemptionCategory.Article39aSpecialRegime,
            ChargeItemCaseNatureOfVatGR.NotTaxableArticle19 => MyDataVatExemptionCategory.BottleRecyclingTicketsNewspapers,
            ChargeItemCaseNatureOfVatGR.NotTaxableArticle22 => MyDataVatExemptionCategory.MedicalInsuranceBankServices,

            // Not Subject (20)

            // Exempt (30)
            ChargeItemCaseNatureOfVatGR.ExemptArticle43TravelAgencies => MyDataVatExemptionCategory.TravelAgencies,
            ChargeItemCaseNatureOfVatGR.ExemptArticle25CustomsRegimes => MyDataVatExemptionCategory.SpecialCustomsRegimes,
            ChargeItemCaseNatureOfVatGR.ExemptArticle39SmallBusinesses => MyDataVatExemptionCategory.SmallBusinesses,
            ChargeItemCaseNatureOfVatGR.ExemptArticle41Farmers => MyDataVatExemptionCategory.SpecialRegimeFarmers,
            ChargeItemCaseNatureOfVatGR.ExemptOtherCases => MyDataVatExemptionCategory.OtherExemptionCases,

            // Margin Scheme (40)
            ChargeItemCaseNatureOfVatGR.MarginSChemeTaxableResellers => MyDataVatExemptionCategory.MarginScheme,

            // Reverse Charge (51)
            ChargeItemCaseNatureOfVatGR.ReverseChargeIntraCommunityDeliveries => MyDataVatExemptionCategory.Article54InterCommunityDeliveryReverseCharge,

            // VAT paid in other EU country (60)
            ChargeItemCaseNatureOfVatGR.VatPaidOtherEUArticle13 => MyDataVatExemptionCategory.GoodsOutsideGreece,
            ChargeItemCaseNatureOfVatGR.VatPaidOtherEUArticle14 => MyDataVatExemptionCategory.ServicesOutsideGreece,

            // Excluded (80)
            ChargeItemCaseNatureOfVatGR.ExcludedArticle2And3 => MyDataVatExemptionCategory.GeneralExemption,
            ChargeItemCaseNatureOfVatGR.ExcludedArticle5BusinessTransfer => MyDataVatExemptionCategory.BusinessAssetsTransfer,
            ChargeItemCaseNatureOfVatGR.ExcludedArticle26TaxWarehouses => MyDataVatExemptionCategory.TaxWarehouses,
            ChargeItemCaseNatureOfVatGR.ExcludedArticle27Diplomatic => MyDataVatExemptionCategory.DiplomaticConsularNATO,
            ChargeItemCaseNatureOfVatGR.ExcludedArticle32OpenSeasShips => MyDataVatExemptionCategory.OpenSeasShips,
            ChargeItemCaseNatureOfVatGR.ExcludedArticle32_1OpenSeasShips => MyDataVatExemptionCategory.OpenSeasShipsArticle32_1,
            ChargeItemCaseNatureOfVatGR.ExcludedPOL_1029_1995 => MyDataVatExemptionCategory.POL_1029_1995,
            ChargeItemCaseNatureOfVatGR.ExcludeGoodsServicesForEUOrThirdCountry => MyDataVatExemptionCategory.GoodsServicesForEUOrThirdCountry,
            ChargeItemCaseNatureOfVatGR.UsualVatApplies => null,
            _ => throw new NotSupportedException($"The ChargeItemCase 0x{chargeItem.ftChargeItemCase:x} contains a not supported Nature NN.") // Unknown nature, no exemption category
        };
    }


    public static int GetMeasurementUnit(ChargeItem? chargeItem)
    {
        if (chargeItem == null || string.IsNullOrWhiteSpace(chargeItem.Unit))
        {
            return MyDataMeasurementUnit.Pieces;
        }

        return chargeItem.Unit.ToLowerInvariant() switch
        {
            "pieces" => MyDataMeasurementUnit.Pieces,
            "kg" => MyDataMeasurementUnit.Kg,
            "litres" => MyDataMeasurementUnit.Litres,
            "meters" => MyDataMeasurementUnit.Meters,
            "squaremeters" => MyDataMeasurementUnit.SquareMeters,
            "cubicmeters" => MyDataMeasurementUnit.CubicMeters,
            "otherpieces" => MyDataMeasurementUnit.OtherPieces,
            // A non-empty Unit that does not match any AADE-defined measurement unit
            // is mapped to OtherPieces; the original string is carried over via
            // otherMeasurementUnitTitle (see AADEFactory.ApplyMeasurementUnit).
            _ => MyDataMeasurementUnit.OtherPieces
        };
    }

    /// <summary>
    /// Applies the AADE measurement-unit-related fields on an invoice row from a charge item,
    /// honoring the ChargeItem.Unit / ChargeItem.UnitQuantity named properties with fallbacks
    /// per market-gr issue #182.
    /// </summary>
    /// <remarks>
    /// Per myDATA REST API spec v2.0.1 §5.4 rule #9: when <c>measurementUnit = 7</c>
    /// (Τεμάχια_Λοιπές Περιπτώσεις / Other Pieces), <c>otherMeasurementUnitQuantity</c> and
    /// <c>otherMeasurementUnitTitle</c> are MANDATORY. The spec also clarifies that
    /// <c>quantity</c> always represents the count of items being moved, NOT the count of packaging.
    ///
    /// Mapping applied here:
    /// <list type="bullet">
    ///   <item><description><c>row.quantity</c> ← <see cref="ChargeItem.UnitQuantity"/> when set, otherwise <see cref="ChargeItem.Quantity"/></description></item>
    ///   <item><description><c>row.otherMeasurementUnitQuantity</c> ← <see cref="ChargeItem.UnitQuantity"/> when set, otherwise <see cref="ChargeItem.Quantity"/> (only when measurementUnit=7; whole-number validated)</description></item>
    ///   <item><description><c>row.otherMeasurementUnitTitle</c> ← <see cref="ChargeItem.Unit"/> (only when measurementUnit=7)</description></item>
    /// </list>
    ///
    /// AADE schema requires <c>quantity &gt; 0</c> (<c>minExclusive="0"</c>), so the absolute value
    /// is always emitted.
    /// </remarks>
    public static void ApplyMeasurementUnit(InvoiceRowType row, ChargeItem chargeItem)
    {
        row.measurementUnit = GetMeasurementUnit(chargeItem);
        row.measurementUnitSpecified = true;

        if (!string.IsNullOrWhiteSpace(chargeItem.Unit))
        {
            // measurementUnit = 7 → both otherMeasurementUnitTitle and otherMeasurementUnitQuantity
            // are mandatory per spec §5.4 rule #9.
            if (row.measurementUnit == MyDataMeasurementUnit.OtherPieces)
            {
                row.otherMeasurementUnitTitle = chargeItem.Unit;
                row.otherMeasurementUnitQuantity = ToOtherMeasurementUnitQuantity(chargeItem.UnitQuantity ?? chargeItem.Quantity);
                row.otherMeasurementUnitQuantitySpecified = true;
            }

            // Unit is set and UnitQuantity is provided: the AADE row's quantity becomes UnitQuantity
            // (the count of items being moved). When UnitQuantity is missing, row.quantity already
            // carries ChargeItem.Quantity from the caller — implementing the fallback rule
            // "Quantity is used as UnitQuantity".
            if (chargeItem.UnitQuantity.HasValue)
            {
                row.quantity = chargeItem.UnitQuantity.Value;
                row.quantitySpecified = true;
            }
        }

        // Normalize: AADE requires quantity > 0 (minExclusive="0"). At this point row.quantity
        // is whichever source already won above (UnitQuantity or caller-supplied Quantity).
        row.quantity = Math.Abs(row.quantity);
    }

    /// <summary>
    /// Enforces myDATA spec v2.0.1 §5.4 rule #9: whenever the row ends up with
    /// <c>measurementUnit = 7</c> (OtherPieces / Τεμάχια_Λοιπές Περιπτώσεις),
    /// <c>otherMeasurementUnitTitle</c> and <c>otherMeasurementUnitQuantity</c> must both be set.
    ///
    /// This guard runs AFTER any line-level override has been applied — it covers the case
    /// where a caller flips <c>measurementUnit</c> to 7 via override without supplying the
    /// accompanying title/quantity, falling back to the values on the original ChargeItem.
    /// </summary>
    public static void EnsureOtherPiecesMandatoryFields(InvoiceRowType row, ChargeItem chargeItem)
    {
        if (!row.measurementUnitSpecified || row.measurementUnit != MyDataMeasurementUnit.OtherPieces)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(row.otherMeasurementUnitTitle) && !string.IsNullOrWhiteSpace(chargeItem.Unit))
        {
            row.otherMeasurementUnitTitle = chargeItem.Unit;
        }

        if (!row.otherMeasurementUnitQuantitySpecified)
        {
            row.otherMeasurementUnitQuantity = ToOtherMeasurementUnitQuantity(chargeItem.UnitQuantity ?? chargeItem.Quantity);
            row.otherMeasurementUnitQuantitySpecified = true;
        }
    }

    /// <summary>
    /// Converts a source quantity (<see cref="ChargeItem.UnitQuantity"/> or
    /// <see cref="ChargeItem.Quantity"/>) to the AADE <c>otherMeasurementUnitQuantity</c> (int).
    /// Throws if the value would lose precision — fractional inputs are rejected since the
    /// AADE field is an integer.
    /// </summary>
    private static int ToOtherMeasurementUnitQuantity(decimal sourceQuantity)
    {
        var abs = Math.Abs(sourceQuantity);
        if (abs != Math.Truncate(abs))
        {
            throw new Exception(
                $"ChargeItem.UnitQuantity/Quantity ({sourceQuantity}) must be a whole number to populate "
                + "otherMeasurementUnitQuantity for AADE measurementUnit=7 (OtherPieces).");
        }
        return (int) abs;
    }


    /// <summary>
    /// Valid values: 1-NOT OBLIGED TO ISSUE, 2-REFUSAL/CLERICAL ERROR,
    /// 3-INTRA-COMMUNITY ACQUISITION, 4-THIRD COUNTRY ACQUISITION, 5-REVERSAL OF OBLIGATION
    /// </summary>
    public static int GetReverseDeliveryNotePurpose(int purpose)
    {
        if (purpose < 1 || purpose > 5)
        {
            throw new Exception(
                $"Invalid reverseDeliveryNotePurpose value '{purpose}'. " +
                "Allowed values: 1-NOT OBLIGED TO ISSUE, 2-REFUSAL/CLERICAL ERROR, " +
                "3-INTRA-COMMUNITY ACQUISITION, 4-THIRD COUNTRY ACQUISITION, 5-REVERSAL OF OBLIGATION.");
        }
        return purpose;
    }
}
