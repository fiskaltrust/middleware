using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;
namespace fiskaltrust.Interface.Tagging.Models.V1.FR
{
    [CaseExtensions(OnType = typeof(ChargeItem), OnField = nameof(ChargeItem.ftChargeItemCase), Mask = 0xFFFF, Prefix = "V1", CaseName = "ChargeItemCase")]
    public enum ftChargeItemCases : long
    {
        UnknownTypeOfService0x0000 = 0x0000,
        UndefinedTypeOfServiceNormalVATRate0x0003 = 0x0003,
        UndefinedTypeOfServiceDiscountedVATRate10x0001 = 0x0001,
        UndefinedTypeOfServiceDiscountedVATRate20x0002 = 0x0002,
        UndefinedTypeOfServiceSpecialVATRate10x0004 = 0x0004,
        UndefinedTypeOfServiceZeroVAT0x0005 = 0x0005,

        ReverseChargeReversalOfTaxLiability0x0006 = 0x0006,
        NotOwnSales0x0007 = 0x0007,

        DeliveryNormalVATRate0x000A = 0x000A,
        DeliveryDiscountedVATRate10x0008 = 0x0008,
        DeliveryDiscountedVATRate20x0009 = 0x0009,
        DeliverySpecialVATRate10x000B = 0x000B,
        DeliveryZeroVAT0x000C = 0x000C,

        OtherServicesNormalVATRate0x000F = 0x000F,
        OtherServicesDiscountedVATRate10x000D = 0x000D,
        OtherServicesDiscountedVATRate20x000E = 0x000E,
        OtherServicesSpecialVATRate10x0010 = 0x0010,
        OtherServicesZeroVAT0x0011 = 0x0011,

        CatalogueServicesNormalVATRate0x0014 = 0x0014,
        CatalogueServicesDiscountedVATRate10x0012 = 0x0012,
        CatalogueServicesDiscountedVATRate20x0013 = 0x0013,
        CatalogueServicesSpecialVATRate10x0015 = 0x0015,
        CatalogueServicesZeroVAT0x0016 = 0x0016,

        OwnConsumptionNormalVATRate0x0019 = 0x0019,
        OwnConsumptionDiscountedVATRate10x0017 = 0x0017,
        OwnConsumptionDiscountedVATRate20x0018 = 0x0018,
        OwnConsumptionSpecialVATRate10x001A = 0x001A,
        OwnConsumptionZeroVAT0x001B = 0x001B,

        DownPaymentNormalVATRate0x001E = 0x001E,
        DownPaymentDiscountedVATRate10x001C = 0x001C,
        DownPaymentDiscountedVATRate20x001D = 0x001D,
        DownPaymentSpecialVATRate10x001F = 0x001F,
        DownPaymentZeroVAT0x0020 = 0x0020,

        AccountOfAThirdParty0x0021 = 0x0021,
        ObligationSigned0x0090 = 0x0090,

    }
}