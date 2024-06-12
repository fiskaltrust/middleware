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
        public void ConvertftChargeItemCaseToV1(ChargeItem chargeItem) => throw new NotImplementedException();
        public void ConvertftJournalTypeToV1(JournalRequest journalRequest) => throw new NotImplementedException();
        public void ConvertftPayItemCaseToV1(PayItem payItem)
        {
            if (!payItem.IsV2())
            {
                // TODO: create NotV2CaseException
                throw new Exception($"Not a V2 receipt case. Found V{payItem.GetVersion()}.");
            }

            if (!payItem.IsFR())
            {
                // TODO: create NotFRCaseException
                throw new Exception("Not a FR receipt case.");
            }
            var v2ftPayItemCase = (V2.ftPayItemCases) (payItem.GetV2ftPayItemCase() & 0xFFFF);
            var v1ftPayItemCase = v2ftPayItemCase switch
            {
                V2.ftPayItemCases.UnknownPaymentType0x0000 => V1.FR.ftPayItemCases.UnknownPaymentType0x0000,
                V2.ftPayItemCases.Cash0x0001 => payItem.IsTip0x0040() ? V1.FR.ftPayItemCases.TipToEmployee0x0012 : (payItem.IsForeignCurrency0x0010() ? V1.FR.ftPayItemCases.CashForeignCurrency0x0002 : V1.FR.ftPayItemCases.Cash0x0001),
                V2.ftPayItemCases.CrossedCheque0x0003 => V1.FR.ftPayItemCases.CrossedCheque0x0003,
                V2.ftPayItemCases.DebitCard0x0004 => V1.FR.ftPayItemCases.DebitCard0x0004,
                V2.ftPayItemCases.CreditCard0x0005 => V1.FR.ftPayItemCases.CreditCard0x0005,
                V2.ftPayItemCases.Voucher0x0006 => V1.FR.ftPayItemCases.Voucher0x0006,
                V2.ftPayItemCases.Online0x0007 => V1.FR.ftPayItemCases.Online0x0007,
                V2.ftPayItemCases.CustomerCard0x0008 => V1.FR.ftPayItemCases.CustomerCard0x0008,
                V2.ftPayItemCases.AccountsReceivable0x0009 => payItem.IsDownpayment0x0008() ? V1.FR.ftPayItemCases.DownPayment0x0010 : V1.FR.ftPayItemCases.AccountsReceivable0x000B,
                V2.ftPayItemCases.SEPATransfer0x000A => V1.FR.ftPayItemCases.SEPATransfer0x000C,
                V2.ftPayItemCases.OtherBankTransfer0x000B => V1.FR.ftPayItemCases.OtherBankTransfer0x000D,
                V2.ftPayItemCases.InternalConsumption0x000D => V1.FR.ftPayItemCases.InternalConsumption0x0011,
                V2.ftPayItemCases.TransferTo0x000C => V1.FR.ftPayItemCases.CashBookExpense0x000E,  
                _ => throw new NotImplementedException()
            };
            payItem.SetV1ftPayItemCase((long) v1ftPayItemCase);
        }
        public void ConvertftReceiptCaseToV1(ReceiptRequest receiptRequest)
        {
            if (!receiptRequest.IsV2())
            {
                // TODO: create NotV2CaseException
                throw new Exception($"Not a V2 receipt case. Found V{receiptRequest.GetVersion()}.");
            }

            if (!receiptRequest.IsFR())
            {
                // TODO: create NotFRCaseException
                throw new Exception("Not a FR receipt case.");
            }
            var v2ftReceiptCase = (V2.ftReceiptCases) (receiptRequest.GetV2ftReceiptCase() & 0xFFFF);
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
            receiptRequest.SetV1ftReceiptCase((long)v1ftReceiptCase);

        }
        public void ConvertftSignatureFormatToV2(SignaturItem signaturItem) => throw new NotImplementedException();
        public void ConvertftSignatureTypeToV2(SignaturItem signaturItem) => throw new NotImplementedException();
        public void ConvertftStateToV2(ReceiptResponse receiptResponse) => throw new NotImplementedException();
    }
}
