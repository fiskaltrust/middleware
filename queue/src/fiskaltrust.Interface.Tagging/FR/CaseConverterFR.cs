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
        public void ConvertftPayItemCaseToV1(PayItem payItem) => throw new NotImplementedException();
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
            var v2ftReceiptCase = (V2.ftReceiptCases) (receiptRequest.GetV2Case() & 0xFFFF);
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
