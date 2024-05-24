using fiskaltrust.Interface.Tagging.Interfaces;
using fiskaltrust.ifPOS.v1;
using V1 = fiskaltrust.Interface.Tagging.Models.V1;
using V2 = fiskaltrust.Interface.Tagging.Models.V2;

namespace fiskaltrust.Interface.Tagging.DE
{
    public class CaseConverterDE : ICaseConverter
    {
        public void ConvertftChargeItemCaseToV1(ChargeItem ftChargeItemCase) => throw new NotImplementedException();
        public void ConvertftJournalTypeToV1(JournalRequest ftJournalType) => throw new NotImplementedException();
        public void ConvertftPayItemCaseToV1(PayItem ftPayItemCase) => throw new NotImplementedException();
        public void ConvertftReceiptCaseToV1(ReceiptRequest receiptRequest)
        {
            if ((receiptRequest.ftReceiptCase & 0x0000_F000_0000_0000) != 0x0000_2000_0000_0000)
            {
                // TODO: create NotV2CaseException
                throw new Exception("Not a V2 receipt case.");
            }

            if (((ulong) receiptRequest.ftReceiptCase & 0xFFFF_0000_0000_0000) != 0x4445_0000_0000_0000)
            {
                // TODO: create NotDECaseException
                throw new Exception("Not a DE receipt case.");
            }

            var v2ReceiptRequest = new ReceiptRequest() { ftReceiptCase = receiptRequest.ftReceiptCase };

            receiptRequest.ftReceiptCase = (long) ((ulong) v2ReceiptRequest.ftReceiptCase & 0xFFFF_0000_0000_0000);

            receiptRequest.ftReceiptCase |= (long) ((V2.ftReceiptCases) (v2ReceiptRequest.ftReceiptCase & 0xFFFF) switch
            {
                V2.ftReceiptCases.UnknownReceipt0x0000 => V1.DE.ftReceiptCases.UnknownReceipt,
                V2.ftReceiptCases.PointOfSaleReceipt0x0001 => V1.DE.ftReceiptCases.PointOfSaleReceipt,
                V2.ftReceiptCases.PaymentTransfer0x0002 => V1.DE.ftReceiptCases.PaymentTransfer,
                // TODO fill out other cases
                _ => throw new NotImplementedException()
            });

            if (V2.Extensions.ReceiptRequestIsExt.IsLateSigning(v2ReceiptRequest))
            {
                V1.DE.Extensions.ReceiptRequestSetExt.SetFailed(receiptRequest);
            }
            if (V2.Extensions.ReceiptRequestIsExt.IsVoid(v2ReceiptRequest))
            {
                V1.DE.Extensions.ReceiptRequestSetExt.SetVoid(receiptRequest);
            }
            // TODO fill out other flags
        }
        public void ConvertftSignatureFormatToV2(SignaturItem ftSignatureFormat) => throw new NotImplementedException();
        public void ConvertftSignatureTypeToV2(SignaturItem ftSignatureType) => throw new NotImplementedException();
        public void ConvertftStateToV2(ReceiptResponse ftstate) => throw new NotImplementedException();
    }
}
