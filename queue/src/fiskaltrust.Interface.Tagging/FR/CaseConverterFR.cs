using fiskaltrust.Interface.Tagging.Interfaces;
using fiskaltrust.ifPOS.v1;
using V1 = fiskaltrust.Interface.Tagging.Models.V1;
using V2 = fiskaltrust.Interface.Tagging.Models.V2;
using fiskaltrust.Interface.Tagging.Models.Extensions;
using fiskaltrust.Interface.Tagging.Models.V2.Extensions;
using fiskaltrust.Interface.Tagging.Models.V1.FR.Extensions;


namespace fiskaltrust.Interface.Tagging.FR
{
    public class CaseConverterFR : ICaseConverter
    {
        public void ConvertftChargeItemCaseToV1(ChargeItem chargeItem)
        {
            if (!chargeItem.IsVersionV2())
            {
                // TODO: create NotV2CaseException
                throw new Exception($"Not a V2 receipt case. Found V{chargeItem.GetVersion()}.");
            }

            if (!chargeItem.IsCountryFR())
            {
                // TODO: create NotFRCaseException
                throw new Exception("Not a FR receipt case.");
            }
            var v2ChargeItem = new ChargeItem() { ftChargeItemCase = chargeItem.ftChargeItemCase };
            chargeItem.ftChargeItemCase = (long) ((ulong) chargeItem.ftChargeItemCase & 0xFFFF_0000_0000_0000);

            var v2ftChargeItemCase = (V2.ftChargeItemCases) v2ChargeItem.GetV2ChargeItemCase();
            V1.FR.ftChargeItemCases v1ftChargeItemCase;
            if (v2ChargeItem.IsV2ChargeItemCaseFlagDownPayment0x0008())
            {
                var v2ftChargeItemVat = (V2.Vat) v2ChargeItem.GetV2ChargeItemCaseVat();
                v1ftChargeItemCase = v2ftChargeItemVat switch
                {
                    V2.Vat.Normal0x3 => V1.FR.ftChargeItemCases.DownPaymentNormalVATRate0x001E,
                    V2.Vat.Discounted10x1 => V1.FR.ftChargeItemCases.DownPaymentDiscountedVATRate10x001C,
                    V2.Vat.Discounted20x2 => V1.FR.ftChargeItemCases.DownPaymentDiscountedVATRate20x001D,
                    V2.Vat.Special10x4 => V1.FR.ftChargeItemCases.DownPaymentSpecialVATRate10x001F,
                    V2.Vat.Zero0x7 => V1.FR.ftChargeItemCases.DownPaymentZeroVAT0x0020,
                    _ => throw new NotImplementedException()
                };
            }
            else
            {
                v1ftChargeItemCase = v2ftChargeItemCase switch
                {
                    V2.ftChargeItemCases.UnknownTypeOfService0x0000 => V1.FR.ftChargeItemCases.UnknownTypeOfService0x0000,
                    V2.ftChargeItemCases.UndefinedTypeOfServiceNormalVATRate0x0003 => V1.FR.ftChargeItemCases.UndefinedTypeOfServiceNormalVATRate0x0003,
                    V2.ftChargeItemCases.UndefinedTypeOfServiceDiscountedVATRate10x0001 => V1.FR.ftChargeItemCases.UndefinedTypeOfServiceDiscountedVATRate10x0001,
                    V2.ftChargeItemCases.UndefinedTypeOfServiceDiscountedVATRate20x0002 => V1.FR.ftChargeItemCases.UndefinedTypeOfServiceDiscountedVATRate20x0002,
                    V2.ftChargeItemCases.UndefinedTypeOfServiceSpecialVATRate10x0004 => V1.FR.ftChargeItemCases.UndefinedTypeOfServiceSpecialVATRate10x0004,
                    V2.ftChargeItemCases.UndefinedTypeOfServiceZeroVAT0x0007 => V1.FR.ftChargeItemCases.UndefinedTypeOfServiceZeroVAT0x0005,
                    V2.ftChargeItemCases.ReverseChargeReversalOfTaxLiability0x5000 => V1.FR.ftChargeItemCases.ReverseChargeReversalOfTaxLiability0x0006,
                    V2.ftChargeItemCases.NotOwnSales0x0060 => V1.FR.ftChargeItemCases.NotOwnSales0x0007,
                    V2.ftChargeItemCases.DeliveryNormalVATRate0x0013 => V1.FR.ftChargeItemCases.DeliveryNormalVATRate0x000A,
                    V2.ftChargeItemCases.DeliveryDiscountedVATRate10x0011 => V1.FR.ftChargeItemCases.DeliveryDiscountedVATRate10x0008,
                    V2.ftChargeItemCases.DeliveryDiscountedVATRate20x0012 => V1.FR.ftChargeItemCases.DeliveryDiscountedVATRate20x0009,
                    V2.ftChargeItemCases.DeliverySpecialVATRate10x0014 => V1.FR.ftChargeItemCases.DeliverySpecialVATRate10x000B,
                    V2.ftChargeItemCases.DeliveryZeroVAT0x0017 => V1.FR.ftChargeItemCases.DeliveryZeroVAT0x000C,
                    V2.ftChargeItemCases.OtherServicesNormalVATRate0x0023 => V1.FR.ftChargeItemCases.OtherServicesNormalVATRate0x000F,
                    V2.ftChargeItemCases.OtherServicesDiscountedVATRate10x0021 => V1.FR.ftChargeItemCases.OtherServicesDiscountedVATRate10x000D,
                    V2.ftChargeItemCases.OtherServicesDiscountedVATRate20x0022 => V1.FR.ftChargeItemCases.OtherServicesDiscountedVATRate20x000E,
                    V2.ftChargeItemCases.OtherServicesSpecialVATRate10x0024 => V1.FR.ftChargeItemCases.OtherServicesSpecialVATRate10x0010,
                    V2.ftChargeItemCases.OtherServicesZeroVAT0x0027 => V1.FR.ftChargeItemCases.OtherServicesZeroVAT0x0011,
                    V2.ftChargeItemCases.CatalogueServicesNormalVATRate0x0053 => V1.FR.ftChargeItemCases.CatalogueServicesNormalVATRate0x0014,
                    V2.ftChargeItemCases.CatalogueServicesDiscountedVATRate10x0051 => V1.FR.ftChargeItemCases.CatalogueServicesDiscountedVATRate10x0012,
                    V2.ftChargeItemCases.CatalogueServicesDiscountedVATRate20x0052 => V1.FR.ftChargeItemCases.CatalogueServicesDiscountedVATRate20x0013,
                    V2.ftChargeItemCases.CatalogueServicesSpecialVATRate10x0054 => V1.FR.ftChargeItemCases.CatalogueServicesSpecialVATRate10x0015,
                    V2.ftChargeItemCases.CatalogueServicesZeroVAT0x0057 => V1.FR.ftChargeItemCases.CatalogueServicesZeroVAT0x0016,
                    V2.ftChargeItemCases.OwnConsumptionNormalVATRate0x0073 => V1.FR.ftChargeItemCases.OwnConsumptionNormalVATRate0x0019,
                    V2.ftChargeItemCases.OwnConsumptionDiscountedVATRate10x0071 => V1.FR.ftChargeItemCases.OwnConsumptionDiscountedVATRate10x0017,
                    V2.ftChargeItemCases.OwnConsumptionDiscountedVATRate20x0072 => V1.FR.ftChargeItemCases.OwnConsumptionDiscountedVATRate20x0018,
                    V2.ftChargeItemCases.OwnConsumptionSpecialVATRate10x0074 => V1.FR.ftChargeItemCases.OwnConsumptionSpecialVATRate10x001A,
                    V2.ftChargeItemCases.OwnConsumptionZeroVAT0x0077 => V1.FR.ftChargeItemCases.OwnConsumptionZeroVAT0x001B,

                    V2.ftChargeItemCases.AccountOfAThirdParty0x0068 => V1.FR.ftChargeItemCases.AccountOfAThirdParty0x0021,
                    V2.ftChargeItemCases.ObligationSigned0x0090 => V1.FR.ftChargeItemCases.ObligationSigned0x0090,
                    _ => throw new NotImplementedException(),
                };
            }
            chargeItem.SetV1ChargeItemCase((long) v1ftChargeItemCase);
        }
        public void ConvertftJournalTypeToV1(JournalRequest journalRequest) => throw new NotImplementedException();
        public void ConvertftPayItemCaseToV1(PayItem payItem)
        {
            if (!payItem.IsVersionV2())
            {
                // TODO: create NotV2CaseException
                throw new Exception($"Not a V2 receipt case. Found V{payItem.GetVersion()}.");
            }

            if (!payItem.IsCountryFR())
            {
                // TODO: create NotFRCaseException
                throw new Exception("Not a FR receipt case.");
            }
            var v2PayItem = new PayItem() { ftPayItemCase = payItem.ftPayItemCase };
            payItem.ftPayItemCase = (long) ((ulong) payItem.ftPayItemCase & 0xFFFF_0000_0000_0000);

            var v2ftPayItemCase = (V2.ftPayItemCases) v2PayItem.GetV2PayItemCase();
            var v1ftPayItemCase = v2ftPayItemCase switch
            {
                V2.ftPayItemCases.UnknownPaymentType0x0000 => V1.FR.ftPayItemCases.UnknownPaymentType0x0000,
                V2.ftPayItemCases.Cash0x0001 => v2PayItem.IsV2PayItemCaseFlagTip0x0040() ? V1.FR.ftPayItemCases.TipToEmployee0x0012 : (v2PayItem.IsV2PayItemCaseFlagForeignCurrency0x0010() ? V1.FR.ftPayItemCases.CashForeignCurrency0x0002 : V1.FR.ftPayItemCases.Cash0x0001),
                V2.ftPayItemCases.CrossedCheque0x0003 => V1.FR.ftPayItemCases.CrossedCheque0x0003,
                V2.ftPayItemCases.DebitCard0x0004 => V1.FR.ftPayItemCases.DebitCard0x0004,
                V2.ftPayItemCases.CreditCard0x0005 => V1.FR.ftPayItemCases.CreditCard0x0005,
                V2.ftPayItemCases.Voucher0x0006 => V1.FR.ftPayItemCases.Voucher0x0006,
                V2.ftPayItemCases.Online0x0007 => V1.FR.ftPayItemCases.Online0x0007,
                V2.ftPayItemCases.CustomerCard0x0008 => V1.FR.ftPayItemCases.CustomerCard0x0008,
                V2.ftPayItemCases.AccountsReceivable0x0009 => v2PayItem.IsV2PayItemCaseFlagDownPayment0x0008() ? V1.FR.ftPayItemCases.DownPayment0x0010 : V1.FR.ftPayItemCases.AccountsReceivable0x000B,
                V2.ftPayItemCases.SEPATransfer0x000A => V1.FR.ftPayItemCases.SEPATransfer0x000C,
                V2.ftPayItemCases.OtherBankTransfer0x000B => V1.FR.ftPayItemCases.OtherBankTransfer0x000D,
                V2.ftPayItemCases.InternalConsumption0x000D => V1.FR.ftPayItemCases.InternalConsumption0x0011,
                V2.ftPayItemCases.TransferTo0x000C => V1.FR.ftPayItemCases.CashBookExpense0x000E,
                _ => throw new NotImplementedException()
            };
            payItem.SetV1PayItemCase((long) v1ftPayItemCase);

        }
        public void ConvertftReceiptCaseToV1(ReceiptRequest receiptRequest)
        {
            if (!receiptRequest.IsVersionV2())
            {
                // TODO: create NotV2CaseException
                throw new Exception($"Not a V2 receipt case. Found V{receiptRequest.GetVersion()}.");
            }

            if (!receiptRequest.IsCountryFR())
            {
                // TODO: create NotFRCaseException
                throw new Exception("Not a FR receipt case.");
            }

            var v2ReceiptRequest = new ReceiptRequest() { ftReceiptCase = receiptRequest.ftReceiptCase };
            receiptRequest.ftReceiptCase = (long) ((ulong) receiptRequest.ftReceiptCase & 0xFFFF_0000_0000_0000);

            var v2ftReceiptCase = (V2.ftReceiptCases) v2ReceiptRequest.GetV2ReceiptCase();
            var v1ftReceiptCase = v2ftReceiptCase switch
            {
                V2.ftReceiptCases.UnknownReceipt0x0000 => V1.FR.ftReceiptCases.UnknownReceipt0x0000,
                V2.ftReceiptCases.PointOfSaleReceipt0x0001 => V1.FR.ftReceiptCases.PointOfSaleReceipt0x0001,
                V2.ftReceiptCases.PaymentTransfer0x0002 => V1.FR.ftReceiptCases.PaymentTransfer0x000C,
                V2.ftReceiptCases.Protocol0x0005 => V1.FR.ftReceiptCases.Protocol0x0009,
                V2.ftReceiptCases.InvoiceUnknown0x1000 => V1.FR.ftReceiptCases.InvoiceUnknown0x0003,
                V2.ftReceiptCases.ZeroReceipt0x2000 => V1.FR.ftReceiptCases.ZeroReceipt0x000F,
                V2.ftReceiptCases.OneReceipt0x2001 => V1.FR.ftReceiptCases.OneReceipt0x2001,
                V2.ftReceiptCases.ShiftClosing0x2010 => V1.FR.ftReceiptCases.ShiftClosing0x0004,
                V2.ftReceiptCases.DailyClosing0x2011 => V1.FR.ftReceiptCases.DailyClosing0x0005,
                V2.ftReceiptCases.MonthlyClosing0x2012 => V1.FR.ftReceiptCases.MonthlyClosing0x0006,
                V2.ftReceiptCases.YearlyClosing0x2013 => V1.FR.ftReceiptCases.YearlyClosing0x0007,
                V2.ftReceiptCases.ProtocolUnspecified0x3000 => V1.FR.ftReceiptCases.ProtocolUnspecified0x0014,
                V2.ftReceiptCases.ProtocolTechnicalEvent0x3001 => V1.FR.ftReceiptCases.ProtocolTechnicalEvent0x0012,
                V2.ftReceiptCases.ProtocolAccountingEvent0x3002 => V1.FR.ftReceiptCases.ProtocolAccountingEvent0x0013,
                V2.ftReceiptCases.InternalUsageMaterialConsumption0x3003 => V1.FR.ftReceiptCases.InternalUsageMaterialConsumption0x000D,
                V2.ftReceiptCases.InitialOperationReceipt0x4001 => V1.FR.ftReceiptCases.InitialOperationReceipt0x0010,
                V2.ftReceiptCases.OutOfOperationReceipt0x4002 => V1.FR.ftReceiptCases.OutOfOperationReceipt0x0011,
                _ => throw new NotImplementedException()
            };
            receiptRequest.SetV1ReceiptCase((long) v1ftReceiptCase);
           
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
            if (v2ReceiptRequest.IsV2ReceiptCaseFlagReceiptRequested0x8000())
            {
                receiptRequest.SetV1ReceiptCaseFlagReceiptRequested0x8000_0000();
            }
            if (v2ReceiptRequest.IsV2ReceiptCaseFlagHandWritten0x0008() ||
                v2ReceiptRequest.IsV2ReceiptCaseFlagIssuerIsSmallBusiness0x0010() ||
                v2ReceiptRequest.IsV2ReceiptCaseFlagReceiverIsBusiness0x0020() ||
                v2ReceiptRequest.IsV2ReceiptCaseFlagSaleInForeignCountry0x0080() ||
                v2ReceiptRequest.IsV2ReceiptCaseFlagReturn0x0100() ||
                v2ReceiptRequest.IsV2ReceiptCaseFlagAdditionalInformationRequested0x0200() ||
                v2ReceiptRequest.IsV2ReceiptCaseFlagSCUDataDownloadRequested0x0400() ||
                v2ReceiptRequest.IsV2ReceiptCaseFlagEnforceServiceOperations0x0800() ||
                v2ReceiptRequest.IsV2ReceiptCaseFlagCleanupOpenTransactions0x1000() ||
                v2ReceiptRequest.IsV2ReceiptCaseFlagPreventEnablingOrDisablingSigningDevices0x2000())
            {
                throw new NotImplementedException();
            }
         
        }
        public void ConvertftSignatureFormatToV2(SignaturItem signaturItem) => throw new NotImplementedException();
        public void ConvertftSignatureTypeToV2(SignaturItem signaturItem) => throw new NotImplementedException();
        public void ConvertftStateToV2(ReceiptResponse receiptResponse) => throw new NotImplementedException();
    }
}
