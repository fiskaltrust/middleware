using fiskaltrust.Interface.Tagging.Interfaces;
using fiskaltrust.ifPOS.v1;
using V1 = fiskaltrust.Interface.Tagging.Models.V1;
using V2 = fiskaltrust.Interface.Tagging.Models.V2;
using fiskaltrust.Interface.Tagging.Models.Extensions;
using fiskaltrust.Interface.Tagging.Models.V2.Extensions;
using fiskaltrust.Interface.Tagging.Models.V1.DE.Extensions;
using fiskaltrust.Interface.Tagging.Models.V2.DE.Extensions;
using V2DE = fiskaltrust.Interface.Tagging.Models.V2.DE;


namespace fiskaltrust.Interface.Tagging.DE
{
    public class CaseConverterDE : ICaseConverter
    {
        public void ConvertftChargeItemCaseToV1(ChargeItem chargeItem)
        {
            var v2ChargeItem = new ChargeItem() { ftChargeItemCase = chargeItem.ftChargeItemCase };

            chargeItem.ftChargeItemCase = (long) ((ulong) v2ChargeItem.ftChargeItemCase & 0xFFFF_0000_0000_0000);

            if (v2ChargeItem.IsV2ChargeItemCaseFlagTakeAway0x0020())
            {
                chargeItem.SetV1ChargeItemCaseFlagTakeAway0x0001();
            }

            if (v2ChargeItem.IsV2ChargeItemCaseVoucherNotTaxable0x0048())
            {
                if (chargeItem.Amount > 0)
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.VoucherSaleNotTaxable0x0060);
                }
                else
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.VoucherRedeemNotTaxable0x0068);
                }
            }
            else if (v2ChargeItem.IsV2ChargeItemCaseVoucherNormalVATRate0x0043())
            {
                if (chargeItem.Amount > 0)
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.CouponSalesNormalVat0x0061);
                }
                else
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.CouponRedeemNormalVat0x0069);
                }
            }
            else if (v2ChargeItem.IsV2ChargeItemCaseVoucherDiscountedVATRate10x0041())
            {
                if (chargeItem.Amount > 0)
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.CouponSalesDiscounted1Vat0x0062);
                }
                else
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.CouponRedeemDiscounted1Vat0x006A);
                }
            }
            else if (v2ChargeItem.IsV2ChargeItemCaseVoucherSpecialVATRate10x0044())
            {
                if (chargeItem.Amount > 0)
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.CouponSalesSpecial1Vat0x0063);
                }
                else
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.CouponRedeemSpecial1Vat0x006B);
                }
            }
            else if (v2ChargeItem.IsV2ChargeItemCaseVoucherSpecialVATRate20x0045())
            {
                if (chargeItem.Amount > 0)
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.CouponSalesSpecial2Vat0x0064);
                }
                else
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.CouponRedeemSpecial2Vat0x006C);
                }
            }
            else if (v2ChargeItem.IsV2ChargeItemCaseVoucherZeroVAT0x0047())
            {
                if (chargeItem.Amount > 0)
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.CouponSalesZeroVat0x0066);
                }
                else
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.CouponRedeemZeroVat0x006E);
                }
            }
            else if (v2ChargeItem.IsV2ChargeItemCaseVoucherUnknownVAT0x0040())
            {
                if (chargeItem.Amount > 0)
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.CouponSalesUnknownVat0x0067);
                }
                else
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.CouponRedeemUnknownVat0x006F);
                }
            }
            else if (v2ChargeItem.IsV2ChargeItemCaseReceivableNormalVATRate0x0093())
            {
                if (chargeItem.Amount > 0)
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReceivableReductionNormalVat0x0079);
                }
                else
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReceivableCreationNormalVat0x0071);
                }
            }
            else if (v2ChargeItem.IsV2ChargeItemCaseReceivableDiscountedVATRate10x0091())
            {
                if (chargeItem.Amount > 0)
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReceivableReductionDiscounted1Vat0x007A);
                }
                else
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReceivableCreationDiscounted1Vat0x0072);
                }
            }
            else if (v2ChargeItem.IsV2ChargeItemCaseReceivableSpecialVATRate10x0094())
            {
                if (chargeItem.Amount > 0)
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReceivableReductionSpecial1Vat0x007B);
                }
                else
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReceivableCreationSpecial1Vat0x0073);
                }
            }
            else if (v2ChargeItem.IsV2ChargeItemCaseReceivableSpecialVATRate20x0095())
            {
                if (chargeItem.Amount > 0)
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReceivableReductionSpecial2Vat0x007C);
                }
                else
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReceivableCreationSpecial2Vat0x0074);
                }
            }
            else if (v2ChargeItem.IsV2ChargeItemCaseReceivableNotTaxable0x0098())
            {
                if (chargeItem.Amount > 0)
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReceivableReductionNotTaxable0x007D);
                }
                else
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReceivableCreationNotTaxable0x0075);
                }
            }
            else if (v2ChargeItem.IsV2ChargeItemCaseReceivableZeroVAT0x0097())
            {
                if (chargeItem.Amount > 0)
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReceivableReductionZeroVat0x007E);
                }
                else
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReceivableCreationZeroVat0x0076);
                }
            }
            else if (v2ChargeItem.IsV2ChargeItemCaseReceivableUnknownVAT0x0090())
            {
                if (chargeItem.Amount > 0)
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReceivableReductionUnknownVat0x007F);
                }
                else
                {
                    chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReceivableCreationUnknownVat0x0077);
                }
            }
            else if (v2ChargeItem.IsV2ChargeItemCaseFlagDownPayment0x0008())
            {
                if (chargeItem.Amount <= 0)
                {
                    if (v2ChargeItem.IsV2ChargeItemCaseVatNormal0x3())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DownPaymentReductionNormalVat0x0089);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatDiscounted10x1())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DownPaymentReductionDiscounted1Vat0x008A);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatSpecial10x4())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DownPaymentReductionSpecial1Vat0x008B);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatSpecial20x5())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DownPaymentReductionSpecial2Vat0x008C);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatNotTaxable0x8())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DownPaymentReductionNotTaxable0x008D);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatZero0x7())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DownPaymentReductionZeroVat0x008E);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatUnknown0x0())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DownPaymentReductionUnknownVat0x008F);
                    }
                }
                else
                {
                    if (v2ChargeItem.IsV2ChargeItemCaseVatNormal0x3())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DownPaymentCreationNormalVat0x0081);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatDiscounted10x1())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DownPaymentCreationDiscounted1Vat0x0082);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatSpecial10x4())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DownPaymentCreationSpecial1Vat0x0083);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatSpecial20x5())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DownPaymentCreationSpecial2Vat0x0084);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatNotTaxable0x8())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DownPaymentCreationNotTaxable0x0085);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatZero0x7())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DownPaymentCreationZeroVat0x0086);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatUnknown0x0())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DownPaymentCreationUnknownVat0x0087);
                    }
                }
            }
            else if (v2ChargeItem.IsV2ChargeItemCaseFlagReturnable0x0010())
            {
                if (chargeItem.Amount <= 0)
                {
                    if (v2ChargeItem.IsV2ChargeItemCaseVatNormal0x3())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReturnableNormalVat0x0021);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatDiscounted10x1())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReturnableDiscounted1Vat0x0022);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatSpecial10x4())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReturnableSpecial1Vat0x0023);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatSpecial20x5())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReturnableSpecial2Vat0x0024);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatNotTaxable0x8())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReturnableNotTaxable0x0025);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatZero0x7())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReturnableZeroVat0x0026);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatUnknown0x0())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReturnableUnknownVat0x0027);
                    }
                }
                else
                {
                    if (v2ChargeItem.IsV2ChargeItemCaseVatNormal0x3())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReturnableReverseNormalVat0x0029);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatDiscounted10x1())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReturnableReverseDiscounted1Vat0x002A);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatSpecial10x4())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReturnableReverseSpecial1Vat0x002B);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatSpecial20x5())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReturnableReverseSpecial2Vat0x002C);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatNotTaxable0x8())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReturnableReverseNotTaxable0x002D);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatZero0x7())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReturnableReverseZeroVat0x002E);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatUnknown0x0())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ReturnableReverseUnknownVat0x002F);
                    }
                }
            }
            else if (v2ChargeItem.IsV2ChargeItemCaseFlagDiscountOrExtraCharge0x0004())
            {
                if (chargeItem.Amount <= 0)
                {
                    if (v2ChargeItem.IsV2ChargeItemCaseVatNormal0x3())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DiscountNormalVat0x0031);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatDiscounted10x1())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DiscountDiscounted1Vat0x0032);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatSpecial10x4())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DiscountSpecial1Vat0x0033);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatSpecial20x5())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DiscountSpecial2Vat0x0034);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatNotTaxable0x8())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DiscountNotTaxable0x0035);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatZero0x7())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DiscountZeroVat0x0036);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatUnknown0x0())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.DiscountUnknownVat0x0037);
                    }
                }
                else
                {
                    if (v2ChargeItem.IsV2ChargeItemCaseVatNormal0x3())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ExtraChargeNormalVat0x0039);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatDiscounted10x1())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ExtraChargeDiscounted1Vat0x003A);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatSpecial10x4())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ExtraChargeSpecial1Vat0x003B);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatSpecial20x5())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ExtraChargeSpecial2Vat0x003C);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatNotTaxable0x8())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ExtraChargeNotTaxable0x003D);
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatZero0x7())
                    {
                        chargeItem.ftChargeItemCase |= (long) V1.DE.ftChargeItemCases.ExtraChargeZeroVat0x003E;
                    }
                    else if (v2ChargeItem.IsV2ChargeItemCaseVatUnknown0x0())
                    {
                        chargeItem.SetV1ChargeItemCase((long) V1.DE.ftChargeItemCases.ExtraChargeUnknownVat0x003F);
                    }
                }
            }
            else
            {
                chargeItem.ftChargeItemCase |= (long) ((V2.ftChargeItemCases) (v2ChargeItem.ftChargeItemCase & 0xFFFF) switch
                {
                    V2.ftChargeItemCases.UnknownTypeOfService0x0000 => V1.DE.ftChargeItemCases.UnknownTypeOfService0x0000,
                    V2.ftChargeItemCases.UndefinedTypeOfServiceNormalVATRate0x0003 => V1.DE.ftChargeItemCases.UndefinedTypeOfServiceNormalVat0x0001,
                    V2.ftChargeItemCases.UndefinedTypeOfServiceDiscountedVATRate10x0001 => V1.DE.ftChargeItemCases.UndefinedTypeOfServiceDiscounted1Vat0x0002,
                    V2.ftChargeItemCases.UndefinedTypeOfServiceSpecialVATRate10x0004 => V1.DE.ftChargeItemCases.UndefinedTypeOfServiceSpecial1Vat0x0003,
                    V2.ftChargeItemCases.UndefinedTypeOfServiceSpecialVATRate20x0005 => V1.DE.ftChargeItemCases.UndefinedTypeOfServiceSpecial2Vat0x0004,
                    V2.ftChargeItemCases.UndefinedTypeOfServiceNotTaxable0x0008 => V1.DE.ftChargeItemCases.UndefinedTypeOfServiceNotTaxable0x0005,
                    V2.ftChargeItemCases.UndefinedTypeOfServiceZeroVAT0x0007 => V1.DE.ftChargeItemCases.UndefinedTypeOfServiceZeroVat0x0006,
                    V2.ftChargeItemCases.ReverseChargeReversalOfTaxLiability0x5000 => V1.DE.ftChargeItemCases.ReverseCharge0x00A1,
                    V2.ftChargeItemCases.NotOwnSales0x0060 => V1.DE.ftChargeItemCases.NotOwnSales0x00A2,
                    V2.ftChargeItemCases.DeliveryNormalVATRate0x0013 => V1.DE.ftChargeItemCases.DeliveryNormalVat0x0011,
                    V2.ftChargeItemCases.DeliveryDiscountedVATRate10x0011 => V1.DE.ftChargeItemCases.DeliveryDiscounted1Vat0x0012,
                    V2.ftChargeItemCases.DeliverySpecialVATRate10x0014 => V1.DE.ftChargeItemCases.DeliverySpecial1Vat0x0013,
                    V2.ftChargeItemCases.DeliverySpecialVATRate20x0015 => V1.DE.ftChargeItemCases.DeliverySpecial2Vat0x0014,
                    V2.ftChargeItemCases.DeliveryNotTaxable0x0018 => V1.DE.ftChargeItemCases.DeliveryNotTaxable0x0015,
                    V2.ftChargeItemCases.DeliveryZeroVAT0x0017 => V1.DE.ftChargeItemCases.DeliveryZeroVat0x0016,
                    V2.ftChargeItemCases.DeliveryUnknownVAT0x0010 => V1.DE.ftChargeItemCases.DeliveryUnknownVat0x0017,
                    V2.ftChargeItemCases.OtherServicesNormalVATRate0x0023 => V1.DE.ftChargeItemCases.OtherServicesNormalVat0x0019,
                    V2.ftChargeItemCases.OtherServicesDiscountedVATRate10x0021 => V1.DE.ftChargeItemCases.OtherServicesDiscounted1Vat0x001A,
                    V2.ftChargeItemCases.OtherServicesSpecialVATRate10x0024 => V1.DE.ftChargeItemCases.OtherServicesSpecial1Vat0x001B,
                    V2.ftChargeItemCases.OtherServicesSpecialVATRate20x0025 => V1.DE.ftChargeItemCases.OtherServicesSpecial2Vat0x001C,
                    V2.ftChargeItemCases.OtherServicesNotTaxable0x0028 => V1.DE.ftChargeItemCases.OtherServicesNotTaxable0x001D,
                    V2.ftChargeItemCases.OtherServicesZeroVAT0x0027 => V1.DE.ftChargeItemCases.OtherServicesZeroVat0x001E,
                    V2.ftChargeItemCases.OtherServicesUnknownVAT0x0020 => V1.DE.ftChargeItemCases.OtherServicesUnknownVat0x001F,
                    V2.ftChargeItemCases.UnrealGrantNormalVATRate0x0083 => V1.DE.ftChargeItemCases.UnrealGrantNormalVat0x0041,
                    V2.ftChargeItemCases.UnrealGrantDiscountedVATRate10x0081 => V1.DE.ftChargeItemCases.UnrealGrantDiscounted1Vat0x0042,
                    V2.ftChargeItemCases.UnrealGrantSpecialVATRate10x0084 => V1.DE.ftChargeItemCases.UnrealGrantSpecial1Vat0x0043,
                    V2.ftChargeItemCases.UnrealGrantSpecialVATRate20x0085 => V1.DE.ftChargeItemCases.UnrealGrantSpecial2Vat0x0044,
                    V2.ftChargeItemCases.GrantNotTaxable0x0088 => V1.DE.ftChargeItemCases.RealGrantNotTaxable0x0049,
                    V2.ftChargeItemCases.UnrealGrantZeroVAT0x0087 => V1.DE.ftChargeItemCases.UnrealGrantZeroVat0x0046,
                    V2.ftChargeItemCases.UnrealGrantUnknownVAT0x0080 => V1.DE.ftChargeItemCases.UnrealGrantUnknownVat0x0047,
                    V2.ftChargeItemCases.TipToOwnerNormalVATRate0x0033 => V1.DE.ftChargeItemCases.TipToOwnerNormalVat0x0051,
                    V2.ftChargeItemCases.TipToOwnerDiscountedVATRate10x0031 => V1.DE.ftChargeItemCases.TipToOwnerDiscounted1Vat0x0052,
                    V2.ftChargeItemCases.TipToOwnerSpecialVATRate10x0034 => V1.DE.ftChargeItemCases.TipToOwnerSpecial1Vat0x0053,
                    V2.ftChargeItemCases.TipToOwnerSpecialVATRate20x0035 => V1.DE.ftChargeItemCases.TipToOwnerSpecial2Vat0x0054,
                    V2.ftChargeItemCases.TipNotTaxable0x0038 => V1.DE.ftChargeItemCases.TipToEmployeeNotTaxable0x0059,
                    V2.ftChargeItemCases.TipToOwnerZeroVAT0x0037 => V1.DE.ftChargeItemCases.TipToOwnerZeroVat0x0056,
                    V2.ftChargeItemCases.TipToOwnerUnknownVAT0x0030 => V1.DE.ftChargeItemCases.TipToOwnerUnknownVat0x0057,
                    V2.ftChargeItemCases.CashTransferNotTaxable0x00A8 => V1.DE.ftChargeItemCases.CashAmountDifferenceFromToTillNotTaxable0x0097,
                    _ => throw new NotImplementedException(),
                });
            }
        }

        public void ConvertftJournalTypeToV1(JournalRequest journalRequest)
        {
            var v2JournalRequest = new JournalRequest() { ftJournalType = journalRequest.ftJournalType };

            journalRequest.ftJournalType = (long) ((ulong) v2JournalRequest.ftJournalType & 0xFFFF_0000_0000_0000);

            journalRequest.ftJournalType |= (long) ((V2DE.ftJournalTypesDE) (v2JournalRequest.ftJournalType & 0xFFFF) switch
            {
                V2DE.ftJournalTypesDE.StatusInformationQueueDE0x1000 => V1.DE.ftJournalTypes.StatusInformationQueueDE0x0000,
                V2DE.ftJournalTypesDE.TARFileExportTse0x1001 => V1.DE.ftJournalTypes.TARFileExportTse0x0001,
                V2DE.ftJournalTypesDE.DsfinVKExport0x1002 => V1.DE.ftJournalTypes.DsfinVKExport0x0002,
                V2DE.ftJournalTypesDE.TARFileExport0x1003 => V1.DE.ftJournalTypes.TARFileExport0x0003,
                _ => throw new NotImplementedException()
            });
        }

        public void ConvertftPayItemCaseToV1(PayItem ftPayItem)
        {
            var v2ftPayItem = new PayItem() { ftPayItemCase = ftPayItem.ftPayItemCase };

            ftPayItem.ftPayItemCase = (long) ((ulong) v2ftPayItem.ftPayItemCase & 0xFFFF_0000_0000_0000);

            if (v2ftPayItem.IsV2PayItemCaseFlagTip0x0040() && v2ftPayItem.IsV2PayItemCaseCash0x0001())
            {
                ftPayItem.SetV1PayItemCase((long) V1.DE.ftPayItemCases.TipToEmployee0x0010);
            }
            else if (v2ftPayItem.IsV2PayItemCaseFlagChange0x0020() && v2ftPayItem.IsV2PayItemCaseCash0x0001())
            {
                ftPayItem.SetV1PayItemCase((long) V1.DE.ftPayItemCases.Change0x000B);
            }
            else if (v2ftPayItem.IsV2PayItemCaseFlagForeignCurrency0x0010() && v2ftPayItem.IsV2PayItemCaseCash0x0001())
            {
                ftPayItem.SetV1PayItemCase((long) V1.DE.ftPayItemCases.CashForeignCurrency0x0002);
            }
            else if (v2ftPayItem.IsV2PayItemCaseFlagDigital0x0080())
            {
                ftPayItem.ftPayItemCase |= (long) ((V2.ftPayItemCases) (v2ftPayItem.ftPayItemCase & 0xFFFF) switch
                {
                    V2.ftPayItemCases.DebitCard0x0004 => V1.DE.ftPayItemCases.DebitCard0x0004,
                    V2.ftPayItemCases.CreditCard0x0005 => V1.DE.ftPayItemCases.CreditCard0x0005,
                    V2.ftPayItemCases.Online0x0007 => V1.DE.ftPayItemCases.Online0x0006,
                    V2.ftPayItemCases.CustomerCard0x0008 => V1.DE.ftPayItemCases.CustomerCard0x0007,
                    V2.ftPayItemCases.SEPATransfer0x000A => V1.DE.ftPayItemCases.SEPATransfer0x0008,
                    V2.ftPayItemCases.OtherBankTransfer0x000B => V1.DE.ftPayItemCases.OtherBankTransfer0x0009,
                    V2.ftPayItemCases.AccountsReceivable0x0009 => V1.DE.ftPayItemCases.DownPayment0x000F,
                    _ => throw new NotImplementedException()
                });
            }
            else
            {
                ftPayItem.ftPayItemCase |= (long) ((V2.ftPayItemCases) (v2ftPayItem.ftPayItemCase & 0xFFFF) switch
                {
                    V2.ftPayItemCases.UnknownPaymentType0x0000 => V1.DE.ftPayItemCases.UnknownPaymentType0x0000,
                    V2.ftPayItemCases.Cash0x0001 => V1.DE.ftPayItemCases.Cash0x0001,
                    V2.ftPayItemCases.CrossedCheque0x0003 => V1.DE.ftPayItemCases.CrossedCheque0x0003,
                    V2.ftPayItemCases.Voucher0x0006 => V1.DE.ftPayItemCases.Voucher0x000D,
                    V2.ftPayItemCases.AccountsReceivable0x0009 => V1.DE.ftPayItemCases.AccountsReceivable0x000E,
                    V2.ftPayItemCases.InternalConsumption0x000D => V1.DE.ftPayItemCases.InternalConsumption0x000A,
                    V2.ftPayItemCases.Grant0x000E => V1.DE.ftPayItemCases.Grant0x0011,
                    V2.ftPayItemCases.TransferTo0x000C => V1.DE.ftPayItemCases.CashTransferFromToTill0x0014,
                    _ => throw new NotImplementedException()
                });
            }


        }
        public void ConvertftReceiptCaseToV1(ReceiptRequest receiptRequest)
        {

            if (!receiptRequest.IsVersionV2())
            {
                // TODO: create NotV2CaseException
                throw new Exception($"Not a V2 receipt case. Found V{receiptRequest.GetVersion()}.");
            }

            if (!receiptRequest.IsCountryDE())
            {
                // TODO: create NotDECaseException
                throw new Exception("Not a DE receipt case.");
            }

            var v2ReceiptRequest = new ReceiptRequest() { ftReceiptCase = receiptRequest.ftReceiptCase };

            receiptRequest.ftReceiptCase = (long) ((ulong) v2ReceiptRequest.ftReceiptCase & 0xFFFF_0000_0000_0000);

            receiptRequest.ftReceiptCase |= (long) ((V2.ftReceiptCases) (v2ReceiptRequest.ftReceiptCase & 0xFFFF) switch
            {
                V2.ftReceiptCases.UnknownReceipt0x0000 => V1.DE.ftReceiptCases.UnknownReceipt0x0000,
                V2.ftReceiptCases.PointOfSaleReceipt0x0001 => V1.DE.ftReceiptCases.PointOfSaleReceipt0x0001,
                V2.ftReceiptCases.PaymentTransfer0x0002 => V1.DE.ftReceiptCases.PaymentTransfer0x0011,
                V2.ftReceiptCases.Protocol0x0005 => V1.DE.ftReceiptCases.Protocol0x000F,
                V2.ftReceiptCases.InvoiceB2B0x1002 => V1.DE.ftReceiptCases.InvoiceB2B0x000C,
                V2.ftReceiptCases.InvoiceB2C0x1001 => V1.DE.ftReceiptCases.InvoiceB2C0x000D,
                V2.ftReceiptCases.ZeroReceipt0x2000 => V1.DE.ftReceiptCases.ZeroReceipt0x0002,
                V2.ftReceiptCases.DailyClosing0x2011 => V1.DE.ftReceiptCases.DailyClosing0x0007,
                V2.ftReceiptCases.MonthlyClosing0x2012 => V1.DE.ftReceiptCases.MonthlyClosing0x0005,
                V2.ftReceiptCases.YearlyClosing0x2013 => V1.DE.ftReceiptCases.YearlyClosing0x0006,
                V2.ftReceiptCases.ProtocolUnspecified0x3000 => V1.DE.ftReceiptCases.ProtocolUnspecified0x0014,
                V2.ftReceiptCases.InternalUsageMaterialConsumption0x3003 => V1.DE.ftReceiptCases.InternalUsageMaterialConsumption0x0012,
                V2.ftReceiptCases.Order0x3004 => V1.DE.ftReceiptCases.Order0x0010,
                V2.ftReceiptCases.InitialOperationReceipt0x4001 => V1.DE.ftReceiptCases.InitialOperationReceipt0x0003,
                V2.ftReceiptCases.OutOfOperationReceipt0x4002 => V1.DE.ftReceiptCases.OutOfOperationReceipt0x0004,
                V2.ftReceiptCases.InitSCUSwitch0x4011 => V1.DE.ftReceiptCases.InitSCUSwitch0x0017,
                V2.ftReceiptCases.FinishSCUSwitch0x4012 => V1.DE.ftReceiptCases.FinishSCUSwitch0x0018,
                _ => throw new NotImplementedException()
            });

            if (v2ReceiptRequest.IsV2ReceiptCaseFlagLateSigning0x0001())
            {
                receiptRequest.SetV1ReceiptCaseFlagFailed0x0001();
            }
            if (v2ReceiptRequest.IsV2ReceiptCaseFlagVoid0x0004())
            {
                receiptRequest.SetV1ReceiptCaseFlagVoid0x0004();
            }
            if (v2ReceiptRequest.IsV2ReceiptCaseFlagTraining0x0002())
            {
                receiptRequest.SetV1ReceiptCaseFlagTraining0x0002();
            }
            if (v2ReceiptRequest.IsV2ReceiptCaseFlagHandWritten0x0008())
            {
                receiptRequest.SetV1ReceiptCaseFlagHandWritten0x0008();
            }
            if (v2ReceiptRequest.IsV2ReceiptCaseFlagIssuerIsSmallBusiness0x0010())
            {
                receiptRequest.SetV1ReceiptCaseFlagSmallBusiness0x0010();
            }
            if (v2ReceiptRequest.IsV2ReceiptCaseFlagReceiverIsBusiness0x0020())
            {
                receiptRequest.SetV1ReceiptCaseFlagReceiverIsCompany0x00020();
            }
            if (v2ReceiptRequest.IsV2ReceiptCaseFlagReceiptRequested0x8000())
            {
                receiptRequest.SetV1ReceiptCaseFlagReceiptRequested0x8000_0000();
            }

            receiptRequest.SetV1ReceiptCaseFlagImplicitTransaction0x0001_0000();

            if (receiptRequest.IsV1ReceiptCaseDailyClosing0x0007() || receiptRequest.IsV1ReceiptCaseMonthlyClosing0x0005() || receiptRequest.IsV1ReceiptCaseYearlyClosing0x0006())
            {
                receiptRequest.SetV1ReceiptCaseFlagUpdateMasterData0x0800();
            }

            if (receiptRequest.IsV1ReceiptCaseDailyClosing0x0007())
            {
                receiptRequest.SetV1ReceiptCaseFlagCloseOpenTransactions0x2000();
            }
        }
        public void ConvertftSignatureFormatToV2(SignaturItem ftSignatureFormat)
        {
            var v1SignaturItem = new SignaturItem() { ftSignatureFormat = ftSignatureFormat.ftSignatureFormat };

            ftSignatureFormat.ftSignatureFormat = (long) ((ulong) v1SignaturItem.ftSignatureFormat & 0xFFFF);

        }
        public void ConvertftSignatureTypeToV2(SignaturItem ftSignatureType)
        {
            var v1SignaturItem = new SignaturItem()
            {
                ftSignatureType = ftSignatureType.ftSignatureType,
                ftSignatureFormat = ftSignatureType.ftSignatureFormat
            };

            ftSignatureType.ftSignatureType = (long) ((ulong) v1SignaturItem.ftSignatureType & 0xFFFF_0000_0000_0000);

            ftSignatureType.SetTypeVersion(2);
            ftSignatureType.SetV2CategorySignatureType((long) V2.SignatureTypesCategory.Normal0x0);
            if (ftSignatureType.IsV1SignatureFormatFlagPrintingOptional0x0001())
            {
                ftSignatureType.SetV2SignatureTypeFlagPrintingOptional();
            }

            switch (v1SignaturItem.ftSignatureType & 0xFFF)
            {
                case (long) V1.DE.ftSignatureTypes.Signature0x0001:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.Signature0x001);
                    break;
                case (long) V1.DE.ftSignatureTypes.StartTransactionResult0x0010:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.StartTransactionResult0x010);
                    break;
                case (long) V1.DE.ftSignatureTypes.FinishTransactionPayload0x0011:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.FinishTransactionPayload0x011);
                    break;
                case (long) V1.DE.ftSignatureTypes.FinishTransactionResult0x0012:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.FinishTransactionResult0x012);
                    break;
                case (long) V1.DE.ftSignatureTypes.ReceiptQrVersion0x0013:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.ReceiptQrVersion0x013);
                    break;
                case (long) V1.DE.ftSignatureTypes.ReceiptPOSSerialNumber0x0014:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.ReceiptPOSSerialNumber0x014);
                    break;
                case (long) V1.DE.ftSignatureTypes.ReceiptProcessType0x0015:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.ReceiptProcessType0x015);
                    break;
                case (long) V1.DE.ftSignatureTypes.ReceiptProcessData0x0016:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.ReceiptProcessData0x016);
                    break;
                case (long) V1.DE.ftSignatureTypes.ReceiptTransactionNumber0x0017:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.ReceiptTransactionNumber0x017);
                    break;
                case (long) V1.DE.ftSignatureTypes.ReceiptSignatureCounter0x0018:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.ReceiptSignatureCounter0x018);
                    break;
                case (long) V1.DE.ftSignatureTypes.ReceiptStartTime0x0019:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.ReceiptStartTime0x019);
                    break;
                case (long) V1.DE.ftSignatureTypes.ReceiptLogTime0x001A:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.ReceiptLogTime0x01A);
                    break;
                case (long) V1.DE.ftSignatureTypes.ReceiptSignatureAlgorithm0x001B:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.ReceiptSignatureAlgorithm0x01B);
                    break;
                case (long) V1.DE.ftSignatureTypes.ReceiptLogTimeFormat0x001C:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.ReceiptLogTimeFormat0x01C);
                    break;
                case (long) V1.DE.ftSignatureTypes.ReceiptSignature0x001D:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.ReceiptSignature0x01D);
                    break;
                case (long) V1.DE.ftSignatureTypes.ReceiptPublicKey0x001E:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.ReceiptPublicKey0x01E);
                    break;
                case (long) V1.DE.ftSignatureTypes.ReceiptProcessStart0x001F:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.ReceiptProcessStart0x01F);
                    break;
                case (long) V1.DE.ftSignatureTypes.UpdateTransactionPayload0x0020:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.UpdateTransactionPayload0x020);
                    break;
                case (long) V1.DE.ftSignatureTypes.UpdateTransactionResult0x0021:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.UpdateTransactionResult0x021);
                    break;
                case (long) V1.DE.ftSignatureTypes.CertificationIdentification0x0022:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.CertificationIdentification0x022);
                    break;
                case (long) V1.DE.ftSignatureTypes.TSESerialNumber0x0023:
                    ftSignatureType.SetV2SignatureType((long) V2DE.ftSignatureTypes.TSESerialNumber0x023);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        public void ConvertftStateToV2(ReceiptResponse ftstate)
        {
            var v1ReceiptResponse = new ReceiptResponse() { ftState = ftstate.ftState };

            ftstate.ftState = (long) ((ulong) v1ReceiptResponse.ftState & 0xFFFF_0000_0000_0000);

            ftstate.SetVersion(2);
            if (v1ReceiptResponse.IsV1StateScuSwitch0x0100())
            {
                ftstate.SetV2ftStateFlagScuInSwitchingState();
            }

            switch (v1ReceiptResponse.ftState & 0xFFF)
            {
                case (long) V1.DE.ftStates.Ready0x0000:
                    ftstate.SetV2State((long) V2.ftStates.Ready0x0000);
                    break;
                case (long) V1.DE.ftStates.SecurityMechanismOutOfService0x0001:
                    ftstate.SetV2State((long) V2.ftStates.SecurityMechanismOutOfOperation0x0001);
                    break;
                case (long) V1.DE.ftStates.ScuTemporaryOutOfService0x0002:
                    ftstate.SetV2State((long) V2.ftStates.ScuTemporaryOutOfService0x0002);
                    break;
                case (long) V1.DE.ftStates.LateSigningMode0x0008:
                    ftstate.SetV2State((long) V2.ftStates.LateSigningModeIsActive0x0008);
                    break;
                case (long) V1.DE.ftStates.ScuSwitch0x0100:
                    ftstate.SetV2State((long) V2.ftStates.Ready0x0000);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
