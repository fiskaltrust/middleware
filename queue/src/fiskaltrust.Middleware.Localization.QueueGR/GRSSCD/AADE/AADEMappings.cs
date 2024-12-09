using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE;

public static class AADEMappings
{
    public static IncomeClassificationType GetIncomeClassificationType(ReceiptRequest receiptRequest, ChargeItem chargeItem)
    {
        var vatAmount = chargeItem.GetVATAmount();
        var netAmount = receiptRequest.IsRefund() ? (-chargeItem.Amount - -vatAmount) : chargeItem.Amount - vatAmount;
        if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x3004)
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
            classificationCategory = AADEMappings.GetIncomeClassificationCategoryType(receiptRequest, chargeItem),
            classificationType = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem),
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
        if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x0005)
        {
            return IncomeClassificationValueType.E3_561_001;
        }
        if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x3005)
        {
            return IncomeClassificationValueType.E3_562;
        }
        if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x3003)
        {
            return IncomeClassificationValueType.E3_595;
        }

        if (chargeItem.IsAgencyBusiness())
        {
            if (receiptRequest.IsReceiptOperation())
            {
                if ((chargeItem.ftChargeItemCase & 0xFF00) == NatureExemptions.EndOfClimateCrisesNature)
                {
                    return IncomeClassificationValueType.E3_881_001;
                }

                if (receiptRequest.HasGreeceCountryCode())
                {
                    return IncomeClassificationValueType.E3_881_002;
                }
                else if (receiptRequest.HasEUCountryCode())
                {
                    return IncomeClassificationValueType.E3_881_003;
                }
                else
                {
                    return IncomeClassificationValueType.E3_881_004;

                }
            }
            else
            {
                if (receiptRequest.HasGreeceCountryCode())
                {
                    return IncomeClassificationValueType.E3_881_001;
                }
                else if (receiptRequest.HasEUCountryCode())
                {
                    return IncomeClassificationValueType.E3_881_003;
                }
                else
                {
                    throw new Exception("Agency business with non EU customer is not supported");
                }
            }
        }

        if (receiptRequest.IsInvoiceOperation())
        {
            if (receiptRequest.HasGreeceCountryCode())
            {
                return (chargeItem.ftChargeItemCase & 0xF0) switch
                {
                    0x00 => IncomeClassificationValueType.E3_561_001,
                    0x10 => IncomeClassificationValueType.E3_561_001,
                    0x20 => IncomeClassificationValueType.E3_561_001,
                    _ => IncomeClassificationValueType.E3_561_007,
                };
            }
            else if (receiptRequest.HasEUCountryCode())
            {
                return IncomeClassificationValueType.E3_561_005;
            }
            else
            {
                return IncomeClassificationValueType.E3_561_006;
            }
        }
        else if (receiptRequest.IsReceiptOperation())
        {
            return (chargeItem.ftChargeItemCase & 0xF0) switch
            {
                0x00 => IncomeClassificationValueType.E3_561_003,
                0x10 => IncomeClassificationValueType.E3_561_003,
                0x20 => IncomeClassificationValueType.E3_561_003,
                _ => IncomeClassificationValueType.E3_561_007,
            };
        }

        if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x3006)
        {
            return (chargeItem.ftChargeItemCase & 0xF0) switch
            {
                0x00 => IncomeClassificationValueType.E3_561_001,
                _ => IncomeClassificationValueType.E3_561_007,
            };
        }
        return (chargeItem.ftChargeItemCase & 0xF0) switch
        {
            0x00 => IncomeClassificationValueType.E3_561_003,
            0x10 => IncomeClassificationValueType.E3_561_003,
            0x20 => IncomeClassificationValueType.E3_561_003,
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
        if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x3003)
        {
            return IncomeClassificationCategoryType.category1_6;
        }

        if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x3005)
        {
            return IncomeClassificationCategoryType.category1_5;
        }

        return (chargeItem.ftChargeItemCase & 0xF0) switch
        {
            0x00 => IncomeClassificationCategoryType.category1_2,
            0x10 => IncomeClassificationCategoryType.category1_2,
            0x20 => IncomeClassificationCategoryType.category1_3,
            0x60 => IncomeClassificationCategoryType.category1_7,
            _ => IncomeClassificationCategoryType.category1_2,
        };
    }

    public static InvoiceType GetInvoiceType(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.IsReceiptOperation())
        {

            if (receiptRequest.GetCasePart() == 0x0003)
            {
                if(!string.IsNullOrEmpty(receiptRequest.ftReceiptCaseData?.ToString()))
                {
                    return InvoiceType.Item32;
                }
                return InvoiceType.Item31;
            }

            if (receiptRequest.GetCasePart() == 0x0005)
            {
                return InvoiceType.Item114;
            }

            if (receiptRequest.cbChargeItems.All(x => x.IsAgencyBusiness()))
            {
                if (receiptRequest.cbChargeItems.Any(x => (x.ftChargeItemCase & 0xFF00) == NatureExemptions.EndOfClimateCrisesNature))
                {
                    return InvoiceType.Item82;
                }

                return InvoiceType.Item115;
            }
            else if (receiptRequest.cbReceiptAmount < 100m)
            {
                return InvoiceType.Item113;
            }
            else if (receiptRequest.HasOnlyServiceItems())
            {
                return InvoiceType.Item112;
            }
            else
            {
                return InvoiceType.Item111;
            }
        }

        if (receiptRequest.IsInvoiceOperation())
        {
            if (receiptRequest.GetCasePart() == 0x1004)
            {
                return !string.IsNullOrEmpty(receiptRequest.cbPreviousReceiptReference) ? InvoiceType.Item51 : InvoiceType.Item52;
            }

            if (receiptRequest.IsInvoiceB2COperation() && !receiptRequest.ContainsCustomerInfo())
            {
                // in this case we don't know the customer so we cannot add the VAT. The invoice is handled as a Μη Αντικριζόμενα operation ( non facing)
                if (receiptRequest.cbChargeItems.All(x => (x.ftChargeItemCase & 0xF0) == 0x20))
                {
                    return InvoiceType.Item112;
                }
                else
                {
                    return InvoiceType.Item111;
                }
            }
            if (receiptRequest.cbChargeItems.Any(x => (x.ftChargeItemCase & 0xF0) == 0x90))
            {
                return InvoiceType.Item15;
            }
            if (receiptRequest.cbChargeItems.Any(x => x.IsAgencyBusiness()))
            {
                return InvoiceType.Item14;
            }
            else if (receiptRequest.IsInvoiceOperation() && receiptRequest.cbChargeItems.All(x => (x.ftChargeItemCase & 0xF0) == 0x20))
            {
                if (!string.IsNullOrEmpty(receiptRequest.cbPreviousReceiptReference))
                {
                    return InvoiceType.Item24;
                }

                if (receiptRequest.HasEUCountryCode())
                {
                    return InvoiceType.Item22;
                }
                else if (receiptRequest.HasNonEUCountryCode())
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
                if (!string.IsNullOrEmpty(receiptRequest.cbPreviousReceiptReference))
                {
                    return InvoiceType.Item16;
                }

                if (receiptRequest.HasGreeceCountryCode())
                {
                    return InvoiceType.Item11;
                }
                else if (receiptRequest.HasEUCountryCode())
                {
                    return InvoiceType.Item12;
                }
                else
                {
                    return InvoiceType.Item13;
                }
            }
        }

        if (receiptRequest.IsProtocolOperation())
        {
            switch (receiptRequest.GetCasePart())
            {
                case 0x3003:
                    return receiptRequest.HasOnlyServiceItems() ? InvoiceType.Item62 : InvoiceType.Item61;
                case 0x3004:
                    return receiptRequest.IsRefund() ? InvoiceType.Item85 : InvoiceType.Item84;
                case 0x3005:
                    return InvoiceType.Item81;
                case 0x3006:
                    return InvoiceType.Item71;
            }
        }
        throw new Exception("Unknown type of receipt " + receiptRequest.ftReceiptCase.ToString("x"));
    }

    public static int GetVATCategory(ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0x0F) switch
    {
        (long) ChargeItemCaseVat.NormalVatRate => MyDataVatCategory.VatRate24, // Normal 24%
        (long) ChargeItemCaseVat.DiscountedVatRate1 => MyDataVatCategory.VatRate13, // Discounted-1 13&
        (long) ChargeItemCaseVat.DiscountedVatRate2 => MyDataVatCategory.VatRate6, // Discounted-2 6%
        (long) ChargeItemCaseVat.SuperReducedVatRate1 => MyDataVatCategory.VatRate17, // Super reduced 1 17%
        (long) ChargeItemCaseVat.SuperReducedVatRate2 => MyDataVatCategory.VatRate9, // Super reduced 2 9%
        (long) ChargeItemCaseVat.ParkingVatRate => MyDataVatCategory.VatRate4, // Parking VAT 4%
        (long) ChargeItemCaseVat.NotTaxable => MyDataVatCategory.RegistrationsWithoutVat, // Not Taxable
        (long) ChargeItemCaseVat.ZeroVatRate => MyDataVatCategory.ExcludingVat, // Zero
        _ => throw new Exception($"The VAT type {chargeItem.ftChargeItemCase & 0xF} of ChargeItem with the case {chargeItem.ftChargeItemCase} is not supported."),
    };

    public static int GetPaymentType(PayItem payItem) => (payItem.ftPayItemCase & 0xF) switch
    {
        (long) PayItemCases.UnknownPaymentType => MyDataPaymentMethods.Cash,
        (long) PayItemCases.CashPayment => MyDataPaymentMethods.Cash,
        (long) PayItemCases.NonCash => MyDataPaymentMethods.Cash,
        (long) PayItemCases.CrossedCheque => MyDataPaymentMethods.Cheque,
        (long) PayItemCases.DebitCardPayment => MyDataPaymentMethods.PosEPos,
        (long) PayItemCases.CreditCardPayment => MyDataPaymentMethods.PosEPos,
        (long) PayItemCases.VoucherPaymentCouponVoucherByMoneyValue => -1,
        (long) PayItemCases.OnlinePayment => MyDataPaymentMethods.WebBanking,
        (long) PayItemCases.LoyaltyProgramCustomerCardPayment => -1,
        (long) PayItemCases.AccountsReceivable => -1,
        (long) PayItemCases.SEPATransfer => -1,
        (long) PayItemCases.OtherBankTransfer => -1,
        (long) PayItemCases.TransferToCashbookVaultOwnerEmployee => -1,
        (long) PayItemCases.InternalMaterialConsumption => -1,
        (long) PayItemCases.Grant => MyDataPaymentMethods.OnCredit,
        (long) PayItemCases.TicketRestaurant => -1,
        _ => throw new Exception($"The Payment type {payItem.ftPayItemCase & 0xF} of PayItem with the case {payItem.ftPayItemCase} is not supported."),
    };

    public static bool RequiresCustomerInfo(InvoiceType invoiceType)
    {
        return invoiceType switch
        {
            InvoiceType.Item11 or InvoiceType.Item12 or InvoiceType.Item13 or InvoiceType.Item14 or InvoiceType.Item15 or InvoiceType.Item16 or InvoiceType.Item21 or InvoiceType.Item22 or InvoiceType.Item23 or InvoiceType.Item24 or InvoiceType.Item51 or InvoiceType.Item52 or InvoiceType.Item31 or InvoiceType.Item32 or InvoiceType.Item61 or InvoiceType.Item62 or InvoiceType.Item71 or InvoiceType.Item81 => true,
            InvoiceType.Item82 or InvoiceType.Item84 or InvoiceType.Item85 or InvoiceType.Item111 or InvoiceType.Item112 or InvoiceType.Item113 or InvoiceType.Item114 or InvoiceType.Item115 => false,
            _ => throw new NotSupportedException($"The invoice type '{invoiceType.GetXmlEnumAttributeValueFromEnum()}' is not supported"),
        };
    }
}
