using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Interfaces;

namespace fiskaltrust.Interface.Tagging.AT
{
    public class CaseConverterAT : ICaseConverter
    {
        public void ConvertftChargeItemCaseToV1(ChargeItem chargeItem) => throw new NotImplementedException();
        public void ConvertftJournalTypeToV1(JournalRequest journalRequest) => throw new NotImplementedException();
        public void ConvertftPayItemCaseToV1(PayItem payItem) => throw new NotImplementedException();
        public void ConvertftReceiptCaseToV1(ReceiptRequest receiptRequest) => throw new NotImplementedException();
        public void ConvertftSignatureFormatToV2(SignaturItem signaturItem) => throw new NotImplementedException();
        public void ConvertftSignatureTypeToV2(SignaturItem signaturItem) => throw new NotImplementedException();
        public void ConvertftStateToV2(ReceiptResponse receiptResponse) => throw new NotImplementedException();
    }
}
