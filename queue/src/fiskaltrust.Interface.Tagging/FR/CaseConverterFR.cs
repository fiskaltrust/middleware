using fiskaltrust.Interface.Tagging.Interfaces;
using fiskaltrust.ifPOS.v1;
using V1 = fiskaltrust.Interface.Tagging.Models.V1;
using V2 = fiskaltrust.Interface.Tagging.Models.V2;
using fiskaltrust.Interface.Tagging.Models.Extensions;
using fiskaltrust.Interface.Tagging.Models.V2.Extensions;

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

            
            
           
        }
        public void ConvertftSignatureFormatToV2(SignaturItem signaturItem) => throw new NotImplementedException();
        public void ConvertftSignatureTypeToV2(SignaturItem signaturItem) => throw new NotImplementedException();
        public void ConvertftStateToV2(ReceiptResponse receiptResponse) => throw new NotImplementedException();
    }
}
