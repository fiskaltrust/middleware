using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [CaseExtensions(OnType = typeof(ChargeItem), OnField = nameof(ChargeItem.ftChargeItemCase), Mask = 0xFFFF)]
    public enum ftChargeItemCases : long
    {
        UnknownTypeOfService0x0000 = 0x0000,
        UndefinedTypeOfServiceNormalVATRate0x0003 = 0x0003,
        UndefinedTypeOfServiceDiscountedVATRate10x0001 = 0x0001,
        UndefinedTypeOfServiceDiscountedVATRate20x0002 = 0x0002,
        UndefinedTypeOfServiceSpecialVATRate10x0004 = 0x0004,
        UndefinedTypeOfServiceSpecialVATRate20x0005 = 0x0005,
        UndefinedTypeOfServiceNotTaxable0x0008 = 0x0008,
        UndefinedTypeOfServiceZeroVAT0x0007 = 0x0007,

        ReverseChargeReversalOfTaxLiability0x5000 = 0x5000,
        NotOwnSales0x0060 = 0x0060,

        DeliveryNormalVATRate0x0013 = 0x0013,
        DeliveryDiscountedVATRate10x0011 = 0x0011,
        DeliveryDiscountedVATRate20x0012 = 0x0012,
        DeliverySpecialVATRate10x0014 = 0x0014,
        DeliverySpecialVATRate20x0015 = 0x0015,
        DeliveryNotTaxable0x0018 = 0x0018,
        DeliveryZeroVAT0x0017 = 0x0017,
        DeliveryUnknownVAT0x0010 = 0x0010,

        OtherServicesNormalVATRate0x0023 = 0x0023,
        OtherServicesDiscountedVATRate10x0021 = 0x0021,
        OtherServicesDiscountedVATRate20x0022 = 0x0022,
        OtherServicesSpecialVATRate10x0024 = 0x0024,
        OtherServicesSpecialVATRate20x0025 = 0x0025,
        OtherServicesNotTaxable0x0028 = 0x0028,
        OtherServicesZeroVAT0x0027 = 0x0027,
        OtherServicesUnknownVAT0x0020 = 0x0020,

        TipToOwnerNormalVATRate0x0033 = 0x0033,
        TipToOwnerDiscountedVATRate10x0031 = 0x0031,
        TipToOwnerDiscountedVATRate20x0032 = 0x0032,
        TipToOwnerSpecialVATRate10x0034 = 0x0034,
        TipToOwnerSpecialVATRate20x0035 = 0x0035,
        TipToOwnerZeroVAT0x0037 = 0x0037,
        TipToOwnerUnknownVAT0x0030 = 0x0030,

        TipNotTaxable0x0038 = 0x0038,

        CouponNormalVATRate0x0043 = 0x0043,
        CouponDiscountedVATRate10x0041 = 0x0041,
        CouponDiscountedVATRate20x0042 = 0x0042,
        CouponSpecialVATRate10x0044 = 0x0044,
        CouponSpecialVATRate20x0045 = 0x0045,
        CouponZeroVAT0x0047 = 0x0047,
        CouponUnknownVAT0x0040 = 0x0040,

        VoucherNotTaxable0x0048 = 0x0048,

        CatalogueServicesNormalVATRate0x0053 = 0x0053,
        CatalogueServicesDiscountedVATRate10x0051 = 0x0051,
        CatalogueServicesDiscountedVATRate20x0052 = 0x0052,
        CatalogueServicesSpecialVATRate10x0054 = 0x0054,
        CatalogueServicesSpecialVATRate20x0055 = 0x0055,
        CatalogueServicesNotTaxable0x0058 = 0x0058,
        CatalogueServicesZeroVAT0x0057 = 0x0057,
        CatalogueServicesUnknownVAT0x0050 = 0x0050,

        AccountOfAThirdParty0x0068 = 0x0068,

        OwnConsumptionNormalVATRate0x0073 = 0x0073,
        OwnConsumptionDiscountedVATRate10x0071 = 0x0071,
        OwnConsumptionDiscountedVATRate20x0072 = 0x0072,
        OwnConsumptionSpecialVATRate10x0074 = 0x0074,
        OwnConsumptionSpecialVATRate20x0075 = 0x0075,
        OwnConsumptionNotTaxable0x0078 = 0x0078,
        OwnConsumptionZeroVAT0x0077 = 0x0077,
        OwnConsumptionUnknownVAT0x0070 = 0x0070,

        UnrealGrantNormalVATRate0x0083 = 0x0083,
        UnrealGrantDiscountedVATRate10x0081 = 0x0081,
        UnrealGrantDiscountedVATRate20x0082 = 0x0082,
        UnrealGrantSpecialVATRate10x0084 = 0x0084,
        UnrealGrantSpecialVATRate20x0085 = 0x0085,
        GrantNotTaxable0x0088 = 0x0088,
        UnrealGrantZeroVAT0x0087 = 0x0087,
        UnrealGrantUnknownVAT0x0080 = 0x0080,

        ReceivableNormalVATRate0x0093 = 0x0093,
        ReceivableDiscountedVATRate10x0091 = 0x0091,
        ReceivableDiscountedVATRate20x0092 = 0x0092,
        ReceivableSpecialVATRate10x0094 = 0x0094,
        ReceivableSpecialVATRate20x0095 = 0x0095,
        ReceivableNotTaxable0x0098 = 0x0098,
        ReceivableZeroVAT0x0097 = 0x0097,
        ReceivableUnknownVAT0x0090 = 0x0090,

        CashTransferNotTaxable0x00A8 = 0x00A8,

    }
}