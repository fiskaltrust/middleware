using fiskaltrust.Interface.Tagging.Interfaces;
using fiskaltrust.ifPOS.v1;
using V1 = fiskaltrust.Interface.Tagging.Models.V1;
using V2 = fiskaltrust.Interface.Tagging.Models.V2;
using fiskaltrust.Interface.Tagging.Models.Extensions;
using fiskaltrust.Interface.Tagging.Models.V2.Extensions;

namespace fiskaltrust.Interface.Tagging.DE
{
    public class CaseConverterDE : ICaseConverter
    {
        public void ConvertftChargeItemCaseToV1(ChargeItem chargeItem)
        {
            var v2ChargeItem = new ChargeItem() { ftChargeItemCase = chargeItem.ftChargeItemCase };

            chargeItem.ftChargeItemCase = (long) ((ulong) v2ChargeItem.ftChargeItemCase & 0xFFFF_0000_0000_0000);



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
                _ => throw new NotImplementedException(),
            })
            ;



            /*

Unreal grant,
normal VAT rate   0041    cccc_2000_0000_0083
Unreal grant,
discounted VAT rate 1 0042    cccc_2000_0000_0081
Unreal grant,
special VAT rate 1    0043    cccc_2000_0000_0084
Unreal grant,
special VAT rate 2    0044    cccc_2000_0000_0085
Unreal grant,
not taxable   0045    cccc_2000_0000_0088
Unreal grant,
zero VAT  0046    cccc_2000_0000_0087
Unreal grant,
unknown VAT   0047    cccc_2000_0000_0080
Real grant,
not taxable 0049    cccc_2000_0000_0088
Tip to owner,
normal VAT rate   0051    cccc_2000_0000_0033
Tip to owner,
discounted VAT rate 1 0052    cccc_2000_0000_0031
Tip to owner,
special VAT rate 1    0053    cccc_2000_0000_0034
Tip to owner,
special VAT rate 2    0054    cccc_2000_0000_0035
Tip to owner,
not taxable   0055    cccc_2000_0000_0038
Tip to owner,
zero VAT  0056    cccc_2000_0000_0037
Tip to owner,
unknown VAT   0057    cccc_2000_0000_0030
Tip to employee,
zero VAT  !!!sik => not taxable    0059    cccc_2000_0000_0038
Voucher sale,
not taxable   0060    cccc_2000_0000_0048
Coupon sales,
normal VAT rate   0061    cccc_2000_0000_0043
Coupon sales,
discounted VAT rate 1 0062    cccc_2000_0000_0041
Coupon sales,
discounted VAT rate 2     cccc_2000_0000_0042
Coupon sales,
special VAT rate 1    0063    cccc_2000_0000_0044
Coupon sales special VAT rate 2 0064    cccc_2000_0000_0045
Coupon sales,
not taxable!!!sik => voucher sale    0065    cccc_2000_0000_0048
Coupon sales,
zero VAT  0066    cccc_2000_0000_0047
Coupon sales,
unknown VAT   0067    cccc_2000_0000_0040
Voucher redeem,
not taxable 0068    cccc_2000_0000_0048
Coupon redeem,
normal VAT rate  0069    cccc_2000_0000_0043
Coupon redeem,
discounted VAT rate 1    006A cccc_2000_0000_0041
Coupon redeem,
special VAT rate 1   006B cccc_2000_0000_0044
Coupon redeem,
special VAT rate 2   006C cccc_2000_0000_0045
Coupon redeem,
not taxable!!!sik => voucher redeem   006D    cccc_2000_0000_0048
Coupon redeem,
zero VAT 006E    cccc_2000_0000_0047
Coupon redeem,
unknown VAT  006F    cccc_2000_0000_0040
Receivable creation,
normal VAT rate    0071    cccc_2000_0000_0093
Receivable creation,
discounted VAT rate 1  0072    cccc_2000_0000_0091
Receivable creation,
special VAT rate 1 0073    cccc_2000_0000_0094
Receivable creation,
special VAT rate 2 0074    cccc_2000_0000_0095
Receivable creation,
not taxable    0075    cccc_2000_0000_0098
Receivable creation,
zero VAT   0076    cccc_2000_0000_0097
Receivable creation,
unknown VAT    0077    cccc_2000_0000_0090
Receivable reduction,
normal VAT rate   0079    cccc_2000_0000_0093
Receivable reduction,
discounted VAT rate 1 007A cccc_2000_0000_0091
Receivable reduction,
special VAT rate 1    007B cccc_2000_0000_0094
Receivable reduction,
special VAT rate 2    007C cccc_2000_0000_0095
Receivable reduction,
not taxable   007D    cccc_2000_0000_0098
Receivable reduction,
zero VAT  007E    cccc_2000_0000_0097
Receivable reduction,
unknown VAT   007F    cccc_2000_0000_0090
Down payment creation,
normal VAT rate  0081    cccc_2000_0008_00x3
Down payment creation,
discounted VAT rate 1    0082    cccc_2000_0008_00x1
Down payment creation,
special VAT rate 1   0083    cccc_2000_0008_00x4
Down payment creation,
special VAT rate 2   0084    cccc_2000_0008_00x5
Down payment creation,
not taxable  0085    cccc_2000_0008_00x8
Down payment creation,
zero VAT 0086    cccc_2000_0008_00x7
Down payment creation,
unknown VAT  0087    cccc_2000_0008_00x0
Down payment reduction,
normal VAT rate 0089    cccc_2000_0008_00x3
Down payment reduction,
discounted VAT rate 1   008A cccc_2000_0008_00x1
Down payment reduction,
special VAT rate 1  008B cccc_2000_0008_00x4
Down payment reduction,
special VAT rate 2  008C cccc_2000_0008_00x5
Down payment reduction,
not taxable 008D    cccc_2000_0008_00x8
Down payment reduction,
zero VAT    008E    cccc_2000_0008_00x7
Down payment reduction,
unknown VAT 008F    cccc_2000_0008_00x0
Cash transfer to empty till,
not taxable    0090    cccc_2000_0000_00A8
Cash transfer from till to owner,
not taxable   0091    cccc_2000_0000_00A8
Cash transfer from owner to till,
not taxable   0092    cccc_2000_0000_00A8
Cash transfer from / to till,
not taxable 0093    cccc_2000_0000_00A8
Cash transfer from till to employee,
not taxable    0094    cccc_2000_0000_00A8
Cash transfer to cash book,
not taxable 0095    cccc_2000_0000_00A8
Cash transfer from cash book,
not taxable   0096    cccc_2000_0000_00A8
Cash amount difference from/ to till,
not taxable    0097    cccc_2000_0000_00A8





            if ()
{
    _ => throw new NotImplementedException()
}

*/

            if (ChargeItemftChargeItemCaseFlagExt.IsReturnable(v2ChargeItem))
            {
                if (v2ChargeItem.Amount <= 0)
                {
                    if (chargeItem.IsVatNormal())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ReturnableNormalVat0x0021;
                    }else if (chargeItem.IsVatDiscounted1())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ReturnableDiscounted1Vat0x0022;
                    }
                    else if (chargeItem.IsVatSpecial1())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ReturnableSpecial1Vat0x0023;
                    }
                    else if (chargeItem.IsVatSpecial2())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ReturnableSpecial2Vat0x0024;
                    }
                    else if (chargeItem.IsVatNotTaxable())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ReturnableNotTaxable0x0025;
                    }
                    else if (chargeItem.IsVatZero())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ReturnableZeroVat0x0026;
                    }
                    else if (chargeItem.IsVatUnknown())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ReturnableUnknownVat0x0027;
                    }
                }
                else
                {
                    if (chargeItem.IsVatNormal())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ReturnableReverseNormalVat0x0029;
                    }
                    else if (chargeItem.IsVatDiscounted1())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ReturnableReverseDiscounted1Vat0x002A;
                    }
                    else if (chargeItem.IsVatSpecial1())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ReturnableReverseSpecial1Vat0x002B;
                    }
                    else if (chargeItem.IsVatSpecial2())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ReturnableReverseSpecial2Vat0x002C;
                    }
                    else if (chargeItem.IsVatNotTaxable())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ReturnableReverseNotTaxable0x002D;
                    }
                    else if (chargeItem.IsVatZero())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ReturnableReverseZeroVat0x002E;
                    }
                    else if (chargeItem.IsVatUnknown())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ReturnableReverseUnknownVat0x002F;
                    }
                }
            }

            if (ChargeItemftChargeItemCaseFlagExt.IsDiscountOrExtraCharge(v2ChargeItem))
            {
                if (v2ChargeItem.Amount <= 0)
                {
                    if (chargeItem.IsVatNormal())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.DiscountNormalVat0x0031;
                    }
                    else if (chargeItem.IsVatDiscounted1())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.DiscountDiscounted1Vat0x0032;
                    }
                    else if (chargeItem.IsVatSpecial1())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.DiscountSpecial1Vat0x0033;
                    }
                    else if (chargeItem.IsVatSpecial2())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.DiscountSpecial2Vat0x0034;
                    }
                    else if (chargeItem.IsVatNotTaxable())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.DiscountNotTaxable0x0035;
                    }
                    else if (chargeItem.IsVatZero())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.DiscountZeroVat0x0036;
                    }
                    else if (chargeItem.IsVatUnknown())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.DiscountUnknownVat0x0037;
                    }
                }
                else
                {
                    if (chargeItem.IsVatNormal())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ExtraChargeNormalVat0x0039;
                    }
                    else if (chargeItem.IsVatDiscounted1())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ExtraChargeDiscounted1Vat0x003A;
                    }
                    else if (chargeItem.IsVatSpecial1())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ExtraChargeSpecial1Vat0x003B;
                    }
                    else if (chargeItem.IsVatSpecial2())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ExtraChargeSpecial2Vat0x003C;
                    }
                    else if (chargeItem.IsVatNotTaxable())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ExtraChargeNotTaxable0x003D;
                    }
                    else if (chargeItem.IsVatZero())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ExtraChargeZeroVat0x003E;
                    }
                    else if (chargeItem.IsVatUnknown())
                    {
                        chargeItem.ftChargeItemCase = (long) V1.DE.ftChargeItemCases.ExtraChargeUnknownVat0x003F;
                    }
                }
            }








        }

        public void ConvertftJournalTypeToV1(JournalRequest ftJournalType) => throw new NotImplementedException();
        public void ConvertftPayItemCaseToV1(PayItem ftPayItemCase) => throw new NotImplementedException();
        public void ConvertftReceiptCaseToV1(ReceiptRequest receiptRequest)
        {
 
            if (!receiptRequest.IsV2())
            {
                // TODO: create NotV2CaseException
                throw new Exception($"Not a V2 receipt case. Found V{receiptRequest.GetVersion()}.");
            }

            if (!receiptRequest.IsDE())
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

            if (V2.Extensions.ReceiptRequestftReceiptCaseFlagExt.IsLateSigning(v2ReceiptRequest))
            {
                V1.DE.Extensions.ReceiptRequestftReceiptCaseFlagExt.SetFailed(receiptRequest);
            }
            if (V2.Extensions.ReceiptRequestftReceiptCaseFlagExt.IsVoid(v2ReceiptRequest))
            {
                V1.DE.Extensions.ReceiptRequestftReceiptCaseFlagExt.SetVoid(receiptRequest);
            }
            if (V2.Extensions.ReceiptRequestftReceiptCaseFlagExt.IsTraining(v2ReceiptRequest))
            {
                V1.DE.Extensions.ReceiptRequestftReceiptCaseFlagExt.SetTraining(receiptRequest);
            }
            if (V2.Extensions.ReceiptRequestftReceiptCaseFlagExt.IsHandWritten(v2ReceiptRequest))
            {
                V1.DE.Extensions.ReceiptRequestftReceiptCaseFlagExt.SetHandWritten(receiptRequest);
            }
            if (V2.Extensions.ReceiptRequestftReceiptCaseFlagExt.IsHandWritten(v2ReceiptRequest))
            {
                V1.DE.Extensions.ReceiptRequestftReceiptCaseFlagExt.SetHandWritten(receiptRequest);
            }
            if (V2.Extensions.ReceiptRequestftReceiptCaseFlagExt.IsIssuerIsSmallBusiness(v2ReceiptRequest))
            {
                V1.DE.Extensions.ReceiptRequestftReceiptCaseFlagExt.SetSmallBusiness(receiptRequest);
            }
            if (V2.Extensions.ReceiptRequestftReceiptCaseFlagExt.IsReceiverIsBusiness(v2ReceiptRequest))
            {
                V1.DE.Extensions.ReceiptRequestftReceiptCaseFlagExt.SetReceiverIsCompany(receiptRequest);
            }
            if (V2.Extensions.ReceiptRequestftReceiptCaseFlagExt.IsReceiptRequested(v2ReceiptRequest))
            {
                V1.DE.Extensions.ReceiptRequestftReceiptCaseFlagExt.SetReceiptRequested(receiptRequest);
            }

        }
        public void ConvertftSignatureFormatToV2(SignaturItem ftSignatureFormat) => throw new NotImplementedException();
        public void ConvertftSignatureTypeToV2(SignaturItem ftSignatureType) => throw new NotImplementedException();
        public void ConvertftStateToV2(ReceiptResponse ftstate) => throw new NotImplementedException();
    }
}
