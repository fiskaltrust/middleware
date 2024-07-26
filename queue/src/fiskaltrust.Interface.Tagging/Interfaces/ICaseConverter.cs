using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Interface.Tagging.Interfaces
{
    public interface ICaseConverter
    {
        void ConvertftReceiptCaseToV1(ReceiptRequest receiptRequest); // throws if the coutrycode is incorrect
        void ConvertftPayItemCaseToV1(PayItem payItem); // throws if the coutrycode is incorrect
        void ConvertftChargeItemCaseToV1(ChargeItem chargeItem); // throws if the coutrycode is incorrect
        void ConvertftStateToV2(ReceiptResponse receiptResponse); // throws if the coutrycode is incorrect
        void ConvertftSignatureFormatToV2(SignaturItem signaturItem); // throws if the coutrycode is incorrect
        void ConvertftSignatureTypeToV2(SignaturItem signaturItem); // throws if the coutrycode is incorrect
        void ConvertftJournalTypeToV1(JournalRequest journalRequest); // throws if the coutrycode is incorrect
    }
}
