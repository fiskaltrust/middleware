using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.DE
{
    [CaseExtensions(OnType = typeof(ChargeItem), OnField = nameof(ChargeItem.ftChargeItemCase), Mask = 0xFFFF)]
    public enum ftChargeItemCases : long
    {
        UnknownTypeOfService0x0000 = 0x0000,
        UndefinedTypeOfServiceNormalVat0x0001 = 0x0001,
        UndefinedTypeOfServiceDiscounted1Vat0x0002 = 0x0002,
        UndefinedTypeOfServiceSpecial1Vat0x0003 = 0x0003,
        UndefinedTypeOfServiceSpecial2Vat0x0004 = 0x0004,
        UndefinedTypeOfServiceNotTaxable0x0005 = 0x0005,
        UndefinedTypeOfServiceZeroVat0x0006 = 0x0006,
        UndefinedTypeOfServiceUnknownVat0x0007 = 0x0007,

        DeliveryNormalVat0x0011 = 0x0011,
        DeliveryDiscounted1Vat0x0012 = 0x0012,
        DeliverySpecial1Vat0x0013 = 0x0013,
        DeliverySpecial2Vat0x0014 = 0x0014,
        DeliveryNotTaxable0x0015 = 0x0015,
        DeliveryZeroVat0x0016 = 0x0016,
        DeliveryUnknownVat0x0017 = 0x00017,

        OtherServicesNormalVat0x0019 = 0x0019,
        OtherServicesDiscounted1Vat0x001A = 0x001A,
        OtherServicesSpecial1Vat0x001B = 0x001B,
        OtherServicesSpecial2Vat0x001C = 0x001C,
        OtherServicesNotTaxable0x001D = 0x001D,
        OtherServicesZeroVat0x001E = 0x001E,
        OtherServicesUnknownVat0x001F = 0x0001F,

        ReturnableNormalVat0x0021 = 0x0021,
        ReturnableDiscounted1Vat0x0022 = 0x0022,
        ReturnableSpecial1Vat0x0023 = 0x0023,
        ReturnableSpecial2Vat0x0024 = 0x0024,
        ReturnableNotTaxable0x0025 = 0x0025,
        ReturnableZeroVat0x0026 = 0x0026,
        ReturnableUnknownVat0x0027 = 0x00027,

        ReturnableReverseNormalVat0x0029 = 0x0029,
        ReturnableReverseDiscounted1Vat0x002A = 0x002A,
        ReturnableReverseSpecial1Vat0x002B = 0x002B,
        ReturnableReverseSpecial2Vat0x002C = 0x002C,
        ReturnableReverseNotTaxable0x002D = 0x002D,
        ReturnableReverseZeroVat0x002E = 0x002E,
        ReturnableReverseUnknownVat0x002F = 0x0002F,

        DiscountNormalVat0x0031 = 0x0031,
        DiscountDiscounted1Vat0x0032 = 0x0032,
        DiscountSpecial1Vat0x0033 = 0x0033,
        DiscountSpecial2Vat0x0034 = 0x0034,
        DiscountNotTaxable0x0035 = 0x0035,
        DiscountZeroVat0x0036 = 0x0036,
        DiscountUnknownVat0x0037 = 0x00037,

        ExtraChargeNormalVat0x0039 = 0x0039,
        ExtraChargeDiscounted1Vat0x003A = 0x003A,
        ExtraChargeSpecial1Vat0x003B = 0x003B,
        ExtraChargeSpecial2Vat0x003C = 0x003C,
        ExtraChargeNotTaxable0x003D = 0x003D,
        ExtraChargeZeroVat0x003E = 0x003E,
        ExtraChargeUnknownVat0x003F = 0x0003F,

        UnrealGrantNormalVat0x0041 = 0x0041,
        UnrealGrantDiscounted1Vat0x0042 = 0x0042,
        UnrealGrantSpecial1Vat0x0043 = 0x0043,
        UnrealGrantSpecial2Vat0x0044 = 0x0044,
        UnrealGrantNotTaxable0x0045 = 0x0045,
        UnrealGrantZeroVat0x0046 = 0x0046,
        UnrealGrantUnknownVat0x0047 = 0x00047,

        RealGrantNotTaxable0x0049 = 0x0049,

        TipToOwnerNormalVat0x0051 = 0x0051,
        TipToOwnerDiscounted1Vat0x0052 = 0x0052,
        TipToOwnerSpecial1Vat0x0053 = 0x0053,
        TipToOwnerSpecial2Vat0x0054 = 0x0054,
        TipToOwnerNotTaxable0x0055 = 0x0055,
        TipToOwnerZeroVat0x0056 = 0x0056,
        TipToOwnerUnknownVat0x0057 = 0x00057,

        TipToEmployeeNotTaxable0x0059 = 0x0059,

        VoucherSaleNotTaxable0x0060 = 0x0060,

        CouponSalesNormalVat0x0061 = 0x0061,
        CouponSalesDiscounted1Vat0x0062 = 0x0062,
        CouponSalesSpecial1Vat0x0063 = 0x0063,
        CouponSalesSpecial2Vat0x0064 = 0x0064,
        CouponSalesNotTaxable0x0065 = 0x0065,
        CouponSalesZeroVat0x0066 = 0x0066,
        CouponSalesUnknownVat0x0067 = 0x00067,

        VoucherRedeemNotTaxable0x0068 = 0x0068,

        CouponRedeemNormalVat0x0069 = 0x0069,
        CouponRedeemDiscounted1Vat0x006A = 0x006A,
        CouponRedeemSpecial1Vat0x006B = 0x006B,
        CouponRedeemSpecial2Vat0x006C = 0x006C,
        CouponRedeemNotTaxable0x006D = 0x006D,
        CouponRedeemZeroVat0x006E = 0x006E,
        CouponRedeemUnknownVat0x006F = 0x0006F,

        ReceivableCreationNormalVat0x0071 = 0x0071,
        ReceivableCreationDiscounted1Vat0x0072 = 0x0072,
        ReceivableCreationSpecial1Vat0x0073 = 0x0073,
        ReceivableCreationSpecial2Vat0x0074 = 0x0074,
        ReceivableCreationNotTaxable0x0075 = 0x0075,
        ReceivableCreationZeroVat0x0076 = 0x0076,
        ReceivableCreationUnknownVat0x0077 = 0x00077,

        ReceivableReductionNormalVat0x0079 = 0x0079,
        ReceivableReductionDiscounted1Vat0x007A = 0x007A,
        ReceivableReductionSpecial1Vat0x007B = 0x007B,
        ReceivableReductionSpecial2Vat0x007C = 0x007C,
        ReceivableReductionNotTaxable0x007D = 0x007D,
        ReceivableReductionZeroVat0x007E = 0x007E,
        ReceivableReductionUnknownVat0x007F = 0x0007F,

        DownPaymentCreationNormalVat0x0081 = 0x0081,
        DownPaymentCreationDiscounted1Vat0x0082 = 0x0082,
        DownPaymentCreationSpecial1Vat0x0083 = 0x0083,
        DownPaymentCreationSpecial2Vat0x0084 = 0x0084,
        DownPaymentCreationNotTaxable0x0085 = 0x0085,
        DownPaymentCreationZeroVat0x0086 = 0x0086,
        DownPaymentCreationUnknownVat0x0087 = 0x0087,

        DownPaymentReductionNormalVat0x0089 = 0x0089,
        DownPaymentReductionDiscounted1Vat0x008A = 0x008A,
        DownPaymentReductionSpecial1Vat0x008B = 0x008B,
        DownPaymentReductionSpecial2Vat0x008C = 0x008C,
        DownPaymentReductionNotTaxable0x008D = 0x008D,
        DownPaymentReductionZeroVat0x008E = 0x008E,
        DownPaymentReductionUnknownVat0x008F = 0x0008F,

        CashTransferToEmptyTillNotTaxable0x0090 = 0x0090,
        CashTransferFromTillToOwnerNotTaxable0x0091 = 0x0091,
        CashTransferFromOwnerToTillNotTaxable0x0092 = 0x0092,
        CashTransferFromToTillNotTaxable0x0093 = 0x0093,
        CashTransferFromTillToEmployeeNotTaxable0x0094 = 0x0094,
        CashTransferToCashBookNotTaxable0x0095 = 0x0095,
        CashTransferFromCashBookNotTaxable0x0096 = 0x0096,
        CashAmountDifferenceFromToTillNotTaxable0x0097 = 0x0097,

        ReverseCharge0x00A1 = 0x00A1,
        NotOwnSales0x00A2 = 0x00A2,
    }
}