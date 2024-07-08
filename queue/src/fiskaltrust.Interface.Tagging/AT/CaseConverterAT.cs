using fiskaltrust.Interface.Tagging.Interfaces;
using fiskaltrust.ifPOS.v1;
using V1 = fiskaltrust.Interface.Tagging.Models.V1;
using V2 = fiskaltrust.Interface.Tagging.Models.V2;
using fiskaltrust.Interface.Tagging.Models.Extensions;
using fiskaltrust.Interface.Tagging.Models.V1.AT.Extensions;
using fiskaltrust.Interface.Tagging.Models.V2.Extensions;
using V2AT = fiskaltrust.Interface.Tagging.Models.V2;

namespace fiskaltrust.Interface.Tagging.AT
{
    public class CaseConverterAT : ICaseConverter
    {
    public void ConvertftChargeItemCaseToV1(ChargeItem chargeItem)
    {
        var v2ChargeItem = new ChargeItem() { ftChargeItemCase = chargeItem.ftChargeItemCase };

        chargeItem.ftChargeItemCase = (long)((ulong)v2ChargeItem.ftChargeItemCase & 0xFFFF_0000_0000_0000);

            if (v2ChargeItem.IsV2ChargeItemCaseFlagDownPayment0x0008())
            {
                chargeItem.ftChargeItemCase |= (long) ((V2.Vat) (v2ChargeItem.ftChargeItemCase & 0xFFFF) switch
                {
                    V2.Vat.Normal0x3 => V1.AT.ftChargeItemCases.DownPaymentNormalVATRate0x001E,
                    V2.Vat.Discounted10x1 => V1.AT.ftChargeItemCases.DownPaymentDiscountedVATRate10x001C,
                    V2.Vat.Discounted20x2 => V1.AT.ftChargeItemCases.DownPaymentDiscountedVATRate20x001D,
                    V2.Vat.Special10x4 => V1.AT.ftChargeItemCases.DownPaymentSpecialVATRate10x001F,
                    V2.Vat.Zero0x7 => V1.AT.ftChargeItemCases.DownPaymentZeroVAT0x0020,
                    _ => throw new NotImplementedException()
                });
            }
            else
            {
                chargeItem.ftChargeItemCase |= (long) ((V2.ftChargeItemCases) (v2ChargeItem.ftChargeItemCase & 0xFFFF) switch
                {
                    V2.ftChargeItemCases.UnknownTypeOfService0x0000 => V1.AT.ftChargeItemCases.UnknownTypeOfService0x0000,
                    V2.ftChargeItemCases.UndefinedTypeOfServiceNormalVATRate0x0003 => V1.AT.ftChargeItemCases.UndefinedTypeOfServiceNormalVat0x0003,
                    V2.ftChargeItemCases.UndefinedTypeOfServiceDiscountedVATRate10x0001 => V1.AT.ftChargeItemCases.UndefinedTypeOfServiceDiscounted1Vat0x0001,
                    V2.ftChargeItemCases.UndefinedTypeOfServiceDiscountedVATRate20x0002 => V1.AT.ftChargeItemCases.UndefinedTypeOfServiceDiscounted2Vat0x0002,
                    V2.ftChargeItemCases.UndefinedTypeOfServiceSpecialVATRate10x0004 => V1.AT.ftChargeItemCases.UndefinedTypeOfServiceSpecial1Vat0x0004,
                    V2.ftChargeItemCases.UndefinedTypeOfServiceZeroVAT0x0007 => V1.AT.ftChargeItemCases.UndefinedTypeOfServiceZeroVat0x0005,
                    V2.ftChargeItemCases.ReverseChargeReversalOfTaxLiability0x5000 => V1.AT.ftChargeItemCases.ReverseCharge0x0006,
                    V2.ftChargeItemCases.NotOwnSales0x0060 => V1.AT.ftChargeItemCases.NotOwnSales0x0007,
                    V2.ftChargeItemCases.DeliveryNormalVATRate0x0013 => V1.AT.ftChargeItemCases.DeliveryNormalVat0x000A,
                    V2.ftChargeItemCases.DeliveryDiscountedVATRate10x0011 => V1.AT.ftChargeItemCases.DeliveryDiscounted1Vat0x0008,
                    V2.ftChargeItemCases.DeliveryDiscountedVATRate20x0012 => V1.AT.ftChargeItemCases.DeliveryDiscounted2Vat0x0009,
                    V2.ftChargeItemCases.DeliverySpecialVATRate10x0014 => V1.AT.ftChargeItemCases.DeliverySpecial1Vat0x000B,
                    V2.ftChargeItemCases.DeliveryZeroVAT0x0017 => V1.AT.ftChargeItemCases.DeliveryZeroVat0x000C,
                    V2.ftChargeItemCases.OtherServicesNormalVATRate0x0023 => V1.AT.ftChargeItemCases.OtherServicesNormalVat0x000F,
                    V2.ftChargeItemCases.OtherServicesDiscountedVATRate10x0021 => V1.AT.ftChargeItemCases.OtherServicesDiscounted1Vat0x000D,
                    V2.ftChargeItemCases.OtherServicesDiscountedVATRate20x0022 => V1.AT.ftChargeItemCases.OtherServicesDiscounted2Vat0x000E,
                    V2.ftChargeItemCases.OtherServicesSpecialVATRate10x0024 => V1.AT.ftChargeItemCases.OtherServicesSpecial1Vat0x0010,
                    V2.ftChargeItemCases.OtherServicesZeroVAT0x0027 => V1.AT.ftChargeItemCases.OtherServicesZeroVat0x0011,
                    V2.ftChargeItemCases.CatalogueServicesNormalVATRate0x0053 => V1.AT.ftChargeItemCases.CatalogueServicesNormalVat0x0014,
                    V2.ftChargeItemCases.CatalogueServicesDiscountedVATRate10x0051 => V1.AT.ftChargeItemCases.CatalogueServicesDiscounted1Vat0x0012,
                    V2.ftChargeItemCases.CatalogueServicesDiscountedVATRate20x0052 => V1.AT.ftChargeItemCases.CatalogueServicesDiscounted2Vat0x0013,
                    V2.ftChargeItemCases.CatalogueServicesSpecialVATRate10x0054 => V1.AT.ftChargeItemCases.CatalogueServicesSpecial1Vat0x0015,
                    V2.ftChargeItemCases.CatalogueServicesZeroVAT0x0057 => V1.AT.ftChargeItemCases.CatalogueServicesZeroVat0x0016,
                    V2.ftChargeItemCases.OwnConsumptionNormalVATRate0x0073 => V1.AT.ftChargeItemCases.OwnConsumptionNormalVat0x0019,
                    V2.ftChargeItemCases.OwnConsumptionDiscountedVATRate10x0071 => V1.AT.ftChargeItemCases.OwnConsumptionDiscounted1Vat0x0017,
                    V2.ftChargeItemCases.OwnConsumptionDiscountedVATRate20x0072 => V1.AT.ftChargeItemCases.OwnConsumptionDiscounted2Vat0x0018,
                    V2.ftChargeItemCases.OwnConsumptionSpecialVATRate10x0074 => V1.AT.ftChargeItemCases.OwnConsumptionSpecial1Vat0x001A,
                    V2.ftChargeItemCases.OwnConsumptionZeroVAT0x0077 => V1.AT.ftChargeItemCases.OwnConsumptionZeroVat0x001B,
                    V2.ftChargeItemCases.AccountOfAThirdParty0x0068 => V1.AT.ftChargeItemCases.AccountOfThirdParty0x0021,
                    V2.ftChargeItemCases.ObligationSigned0x0090 => V1.AT.ftChargeItemCases.ObligationSigned0x0022,
                    _ => throw new NotImplementedException(),
                });
            }      
    }

        public void ConvertftJournalTypeToV1(JournalRequest journalRequest)
        {
            var v2JournalRequest = new JournalRequest() { ftJournalType = journalRequest.ftJournalType };

            journalRequest.ftJournalType = (long)((ulong)v2JournalRequest.ftJournalType & 0xFFFF_0000_0000_0000);

            journalRequest.ftJournalType |= (long)((V2.ftJournalTypes)(v2JournalRequest.ftJournalType & 0xFFFF) switch
            {
                V2.ftJournalTypes.StatusInformationQueueAT0x1000 => V1.AT.ftJournalTypes.StatusInformationQueueAT0x0000,
                V2.ftJournalTypes.RKSVDEPExport0x1001 => V1.AT.ftJournalTypes.RKSVDEPExport0x0001,
                _ => throw new NotImplementedException()
            });
        }

         public void ConvertftPayItemCaseToV1(PayItem payItem)
        {            
            var v2PayItem = new PayItem() { ftPayItemCase = payItem.ftPayItemCase };
            payItem.ftPayItemCase = (long) ((ulong) payItem.ftPayItemCase & 0xFFFF_0000_0000_0000);

            var v2ftPayItemCase = (V2.ftPayItemCases) v2PayItem.GetV2PayItemCase();
            var v1ftPayItemCase = v2ftPayItemCase switch
            {
                V2.ftPayItemCases.UnknownPaymentType0x0000 => V1.AT.ftPayItemCases.UnknownPaymentType0x0000,
                V2.ftPayItemCases.Cash0x0001 => v2PayItem.IsV2PayItemCaseFlagTip0x0040() ? V1.AT.ftPayItemCases.TipToEmployee0x0012 : (v2PayItem.IsV2PayItemCaseFlagForeignCurrency0x0010() ? V1.AT.ftPayItemCases.CashForeignCurrency0x0002 : V1.AT.ftPayItemCases.Cash0x0001),
                V2.ftPayItemCases.CrossedCheque0x0003 => V1.AT.ftPayItemCases.CrossedCheque0x0003,
                V2.ftPayItemCases.DebitCard0x0004 => V1.AT.ftPayItemCases.DebitCard0x0004,
                V2.ftPayItemCases.CreditCard0x0005 => V1.AT.ftPayItemCases.CreditCard0x0005,
                V2.ftPayItemCases.Voucher0x0006 => V1.AT.ftPayItemCases.Voucher0x0006,
                V2.ftPayItemCases.Online0x0007 => V1.AT.ftPayItemCases.OnlinePayment0x0007,
                V2.ftPayItemCases.CustomerCard0x0008 => V1.AT.ftPayItemCases.CustomerCardPayment0x0008,
                V2.ftPayItemCases.AccountsReceivable0x0009 => v2PayItem.IsV2PayItemCaseFlagDownPayment0x0008() ? V1.AT.ftPayItemCases.DownPayment0x0010 : V1.AT.ftPayItemCases.AccountsReceivable0x000B,
                V2.ftPayItemCases.SEPATransfer0x000A => V1.AT.ftPayItemCases.SEPATransfer0x000C,
                V2.ftPayItemCases.OtherBankTransfer0x000B => V1.AT.ftPayItemCases.OtherBankTransfer0x000D,
                V2.ftPayItemCases.InternalConsumption0x000D => V1.AT.ftPayItemCases.InternalConsumption0x0011,
                V2.ftPayItemCases.TransferTo0x000C => V1.AT.ftPayItemCases.CashBookExpense0x000E,
                _ => throw new NotImplementedException()
            };
            payItem.SetV1PayItemCase((long) v1ftPayItemCase);

        }

        public void ConvertftReceiptCaseToV1(ReceiptRequest receiptRequest)
        {
            var v2ReceiptRequest = new ReceiptRequest() { ftReceiptCase = receiptRequest.ftReceiptCase };
            receiptRequest.ftReceiptCase = (long)((ulong)receiptRequest.ftReceiptCase & 0xFFFF_0000_0000_0000);

            var v2ftReceiptCase = (V2.ftReceiptCases)v2ReceiptRequest.GetV2ReceiptCase();
            var v1ftReceiptCase = v2ftReceiptCase switch
            {
                V2.ftReceiptCases.UnknownReceipt0x0000 => V1.AT.ftReceiptCases.UnknownReceiptType0x0000,
                V2.ftReceiptCases.PointOfSaleReceipt0x0001 => V1.AT.ftReceiptCases.POSReceipt0x0001,
                V2.ftReceiptCases.CashDepositCashPayIn0x000A => V1.AT.ftReceiptCases.CashDepositCashPayIn0x000A,
                V2.ftReceiptCases.CashPayOut0x000B => V1.AT.ftReceiptCases.CashPayOut0x000B,
                V2.ftReceiptCases.PaymentTransfer0x0002 => V1.AT.ftReceiptCases.PaymentTransfer0x000C,
                V2.ftReceiptCases.PointOfSaleReceiptWithoutObligation0x0003 => V1.AT.ftReceiptCases.POSReceiptWithoutCashRegisterObligation0x0007,
                V2.ftReceiptCases.ECommerce0x0004 => V1.AT.ftReceiptCases.ECommerce0x000F,
                V2.ftReceiptCases.Protocol0x0005 => V1.AT.ftReceiptCases.ProtocolArtefactHandedOutToConsumer0x0009,
                V2.ftReceiptCases.InvoiceUnknown0x1000 => V1.AT.ftReceiptCases.InvoiceUnspecifiedType0x0008,
                V2.ftReceiptCases.ZeroReceipt0x2000 => V1.AT.ftReceiptCases.ZeroReceipt0x0002,
                V2.ftReceiptCases.MonthlyClosing0x2012 => V1.AT.ftReceiptCases.MonthlyClosing0x0005,
                V2.ftReceiptCases.YearlyClosing0x2013 => V1.AT.ftReceiptCases.YearlyClosing0x0006,
                V2.ftReceiptCases.ProtocolUnspecified0x3000 => V1.AT.ftReceiptCases.ProtocolUnspecifiedType0x000D,
                V2.ftReceiptCases.InternalUsageMaterialConsumption0x3003 => V1.AT.ftReceiptCases.InternalUsageMaterialConsumption0x000E,
                V2.ftReceiptCases.InitialOperationReceipt0x4001 => V1.AT.ftReceiptCases.InitialOperationReceipt0x0003,
                V2.ftReceiptCases.OutOfOperationReceipt0x4002 => V1.AT.ftReceiptCases.OutOfOperationReceipt0x0004,
                V2.ftReceiptCases.SaleInForeignCountries0x4010 => V1.AT.ftReceiptCases.SaleInForeignCountries0x0010,
                _ => throw new NotImplementedException()
            };
            receiptRequest.SetV1ReceiptCase((long)v1ftReceiptCase);

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

        public void ConvertftSignatureFormatToV2(SignaturItem ftSignatureFormat)
        {
            var v1SignatureFormat = new SignaturItem() { ftSignatureFormat = ftSignatureFormat.ftSignatureFormat };

            ftSignatureFormat.ftSignatureFormat = (long)((ulong)v1SignatureFormat.ftSignatureFormat & 0xFFFF_2000_0000_0000);

            ftSignatureFormat.ftSignatureFormat |= (long)((V1.AT.ftSignatureFormats)(v1SignatureFormat.ftSignatureFormat & 0xFFFF) switch
            {
                V1.AT.ftSignatureFormats.Unknown0x0000 => V2.ftSignatureFormats.Unknown0x0000,
                V1.AT.ftSignatureFormats.Text0x0001 => V2.ftSignatureFormats.Text0x0001,
                V1.AT.ftSignatureFormats.Link0x0002 => V2.ftSignatureFormats.Link0x0002,
                V1.AT.ftSignatureFormats.QRCode0x0003 => V2.ftSignatureFormats.QrCode0x0003,
                V1.AT.ftSignatureFormats.Code1280x0004 => V2.ftSignatureFormats.Code1280x0004,
                V1.AT.ftSignatureFormats.OcrA0x0005 => V2.ftSignatureFormats.OcrA0x0005,
                V1.AT.ftSignatureFormats.Pdf4170x0006 => V2.ftSignatureFormats.Pdf4170x0006,
                V1.AT.ftSignatureFormats.DataMatrix0x0007 => V2.ftSignatureFormats.DataMatrix0x0007,
                V1.AT.ftSignatureFormats.Aztec0x0008 => V2.ftSignatureFormats.Aztec0x0008,
                V1.AT.ftSignatureFormats.Ean8Barcode0x0009 => V2.ftSignatureFormats.Ean8Barcode0x0009,
                V1.AT.ftSignatureFormats.Ean130x000A => V2.ftSignatureFormats.Ean130x000A,
                V1.AT.ftSignatureFormats.UPCA0x000B => V2.ftSignatureFormats.UPCA0x000B,
                V1.AT.ftSignatureFormats.Code390x000C => V2.ftSignatureFormats.Code390x000C,
                _ => throw new NotImplementedException(),
            });
        }

        public void ConvertftSignatureTypeToV2(SignaturItem ftSignatureType)
        {
            var v1SignatureType = new SignaturItem() { ftSignatureType = ftSignatureType.ftSignatureType };

            ftSignatureType.ftSignatureType = (long)((ulong)v1SignatureType.ftSignatureType & 0xFFFF_0000_0000_0000);

            ftSignatureType.SetTypeVersion(2);
            ftSignatureType.SetV2CategorySignatureType((long)V2.SignatureTypesCategory.Normal0x0);

            switch (v1SignatureType.ftSignatureType & 0xFFF)
            {
                case (long)V1.AT.ftSignatureTypes.Unknown0x0000:
                    ftSignatureType.SetV2SignatureType((long)V2.ftSignatureTypes.Unknown0x0000);
                    break;
                case (long)V1.AT.ftSignatureTypes.SignatureAccordingToRKSV0x0001:
                    ftSignatureType.SetV2SignatureType((long)V2.ftSignatureTypes.SignatureAccordingToRKSV0x0001);
                    break;
                case (long)V1.AT.ftSignatureTypes.ArchivingRequired0x0002:
                    ftSignatureType.SetV2SignatureType((long)V2.ftSignatureTypes.ArchivingRequired0x0002);
                    break;
                case (long)V1.AT.ftSignatureTypes.FinanzOnlineNotification0x0003:
                    ftSignatureType.SetV2SignatureType((long)V2.ftSignatureTypes.FinanzOnlineNotification0x0003);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void ConvertftStateToV2(ReceiptResponse ftState)
        {
            var v1State = new ReceiptResponse() { ftState = ftState.ftState };

            ftState.ftState = (long)((ulong)v1State.ftState & 0xFFFF_0000_0000_0000);

            ftState.ftState |= (long)((V1.AT.ftStates)(v1State.ftState & 0xFFFF) switch
            {
                V1.AT.ftStates.OutOfService0x0001 => V2.ftStates.OutOfService0x0001,
                V1.AT.ftStates.SSCDTemporaryOutOfService0x0002 => V2.ftStates.SSCDTemporaryOutOfService0x0002,
                V1.AT.ftStates.SSCDPermanentlyOutOfService0x0004 => V2.ftStates.SSCDPermanentlyOutOfService0x0004,
                V1.AT.ftStates.SubsequentEntryActivated0x0008 => V2.ftStates.SubsequentEntryActivated0x0008,
                V1.AT.ftStates.MonthlyReportDue0x0010 => V2.ftStates.MonthlyReportDue0x0010,
                V1.AT.ftStates.AnnualReportDue0x0020 => V2.ftStates.AnnualReportDue0x0020,
                V1.AT.ftStates.MessageNotificationPending0x0040 => V2.ftStates.MessageNotificationPending0x0040,
                V1.AT.ftStates.BackupSSCDInUse0x0080 => V2.ftStates.BackupSSCDInUse0x0080,
                _ => throw new NotImplementedException(),
            });
        }
    }
}