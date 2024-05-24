using fiskaltrust.Interface.Tagging.Interfaces;
using fiskaltrust.ifPOS.v1;
using V1 = fiskaltrust.Interface.Tagging.Models.V1;
using V2 = fiskaltrust.Interface.Tagging.Models.V2;
using fiskaltrust.Interface.Tagging.Models.Extensions;

namespace fiskaltrust.Interface.Tagging.DE
{
    public class CaseConverterDE : ICaseConverter
    {
        public void ConvertftChargeItemCaseToV1(ChargeItem ftChargeItemCase) => throw new NotImplementedException();
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
                V2.ftReceiptCases.UnknownReceipt0x0000 => V1.DE.ftReceiptCases.UnknownReceipt,
                V2.ftReceiptCases.PointOfSaleReceipt0x0001 => V1.DE.ftReceiptCases.PointOfSaleReceipt,
                V2.ftReceiptCases.PaymentTransfer0x0002 => V1.DE.ftReceiptCases.PaymentTransfer,
                // TODO fill out other cases
                _ => throw new NotImplementedException()
            });

            if (V2.Extensions.ReceiptRequestFlagExt.IsLateSigning(v2ReceiptRequest))
            {
                V1.DE.Extensions.ReceiptRequestFlagExt.SetFailed(receiptRequest);
            }
            if (V2.Extensions.ReceiptRequestFlagExt.IsVoid(v2ReceiptRequest))
            {
                V1.DE.Extensions.ReceiptRequestFlagExt.SetVoid(receiptRequest);
            }
            // TODO fill out other flags
        }
        public void ConvertftSignatureFormatToV2(SignaturItem ftSignatureFormat) => throw new NotImplementedException();
        public void ConvertftSignatureTypeToV2(SignaturItem ftSignatureType) => throw new NotImplementedException();
        public void ConvertftStateToV2(ReceiptResponse ftstate) => throw new NotImplementedException();
    }
}
