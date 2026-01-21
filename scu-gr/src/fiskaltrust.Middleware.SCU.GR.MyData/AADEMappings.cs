using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;

namespace fiskaltrust.Middleware.SCU.GR.MyData;
public static class AADEMappings
{
    public static IncomeClassificationType GetIncomeClassificationType(ReceiptRequest receiptRequest, ChargeItem chargeItem)
    {
        var vatAmount = chargeItem.GetVATAmount();
        var netAmount = receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) ? -chargeItem.Amount - -vatAmount : chargeItem.Amount - vatAmount;
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
                    throw new Exception("Agency business with non EU customer is not supported");
                }
                else
                {
                    return IncomeClassificationValueType.E3_881_001;
                }
            }
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

        return chargeItem.ftChargeItemCase.TypeOfService() switch
        {
            ChargeItemCaseTypeOfService.UnknownService => IncomeClassificationCategoryType.category1_95,
            ChargeItemCaseTypeOfService.Delivery => IncomeClassificationCategoryType.category1_1,
            ChargeItemCaseTypeOfService.CatalogService => IncomeClassificationCategoryType.category1_2,
            ChargeItemCaseTypeOfService.OtherService => IncomeClassificationCategoryType.category1_3,
            ChargeItemCaseTypeOfService.NotOwnSales => IncomeClassificationCategoryType.category1_7,
            _ => throw new Exception($"The ChargeItem type {chargeItem.ftChargeItemCase.TypeOfService()} is not supported for IncomeClassificationCategoryType."),
        };
    }

    public static InvoiceType GetInvoiceType(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayload>(out var overrideData) && overrideData?.GR?.MyDataOverride != null)
        {
            var headerOverride = overrideData.GR.MyDataOverride.Invoice?.InvoiceHeader;
            if (!string.IsNullOrEmpty(headerOverride?.InvoiceType))
            {
                // Define allowed invoice types for override
                var allowedInvoiceTypes = new HashSet<string> { "8.2" };
                if (!allowedInvoiceTypes.Contains(headerOverride.InvoiceType))
                {
                    throw new Exception($"Invalid invoice type override value '{headerOverride.InvoiceType}'. Only the following values are allowed: 3.1, 3.2, 6.1, 6.2, 8.1, 8.2, 9.3");
                }

                if(headerOverride.InvoiceType == "8.2")
                {
                    return InvoiceType.Item82;
                }
            }
        }


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

            if (receiptRequest.cbChargeItems.All(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.NotOwnSales)))
            {
                return InvoiceType.Item115;
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
                return receiptRequest.cbPreviousReceiptReference is not null ? InvoiceType.Item51 : InvoiceType.Item52;
            }

            if (receiptRequest.cbChargeItems.All(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.NotOwnSales)))
            {
                return InvoiceType.Item14;
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

    public static DateTime GetLocalTime(ReceiptRequest receiptRequest)
    {
        //option 1
        return receiptRequest.cbReceiptMoment.ToLocalTime();

        //option 2
        /*
        var gtbZone = TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(receiptRequest.cbReceiptMoment, gtbZone);
        */
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
}