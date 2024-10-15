using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.AT
{
    [CaseExtensions(OnType = typeof(ChargeItem), OnField = nameof(ChargeItem.ftChargeItemCase), Mask = 0xFFFF, Prefix = "V1", CaseName = "ChargeItemCase")]
    public enum ftChargeItemCases : long
    {
        UnknownTypeOfService0x0000 = 0x0000,
        UndefinedTypeOfServiceNormalVat0x0003 = 0x0003,
        UndefinedTypeOfServiceDiscounted1Vat0x0001 = 0x0001,
        UndefinedTypeOfServiceDiscounted2Vat0x0002 = 0x0002,
        UndefinedTypeOfServiceSpecial1Vat0x0004 = 0x0004,
        UndefinedTypeOfServiceZeroVat0x0005 = 0x0005,
        ReverseCharge0x0006 = 0x0006,
        NotOwnSales0x0007 = 0x0007,

        DeliveryNormalVat0x000A = 0x000A,
        DeliveryDiscounted1Vat0x0008 = 0x0008,
        DeliveryDiscounted2Vat0x0009 = 0x0009,
        DeliverySpecial1Vat0x000B = 0x000B,
        DeliveryZeroVat0x000C = 0x000C,

        OtherServicesNormalVat0x000F = 0x000F,
        OtherServicesDiscounted1Vat0x000D = 0x000D,
        OtherServicesDiscounted2Vat0x000E = 0x000E,
        OtherServicesSpecial1Vat0x0010 = 0x0010,
        OtherServicesZeroVat0x0011 = 0x0011,

        CatalogueServicesNormalVat0x0014 = 0x0014,
        CatalogueServicesDiscounted1Vat0x0012 = 0x0012,
        CatalogueServicesDiscounted2Vat0x0013 = 0x0013,
        CatalogueServicesSpecial1Vat0x0015 = 0x0015,
        CatalogueServicesZeroVat0x0016 = 0x0016,

        OwnConsumptionNormalVat0x0019 = 0x0019,
        OwnConsumptionDiscounted1Vat0x0017 = 0x0017,
        OwnConsumptionDiscounted2Vat0x0018 = 0x0018,
        OwnConsumptionSpecial1Vat0x001A = 0x001A,
        OwnConsumptionZeroVat0x001B = 0x001B,

        DownPaymentNormalVATRate0x001E = 0x001E,
        DownPaymentDiscountedVATRate10x001C = 0x001C,
        DownPaymentDiscountedVATRate20x001D = 0x001D,
        DownPaymentSpecialVATRate10x001F = 0x001F,
        DownPaymentZeroVAT0x0020 = 0x0020,

        AccountOfThirdParty0x0021 = 0x0021,
        ObligationSigned0x0022 = 0x0022,
    }
}