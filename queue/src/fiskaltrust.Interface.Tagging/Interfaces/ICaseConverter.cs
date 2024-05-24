using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Interface.Tagging.Interfaces
{
    public interface ICaseConverter
    {
        long ConvertftReceiptCaseToV1(long ftReceiptCase); // throws if the coutrycode is incorrect
        long ConvertftPayItemCaseToV1(long ftPayItemCase); // throws if the coutrycode is incorrect
        long ConvertftChargeItemCaseToV1(long ftChargeItemCase); // throws if the coutrycode is incorrect
        long ConvertftStateToV2(long ftstate); // throws if the coutrycode is incorrect
        long ConvertftSignatureFormatToV2(long ftSignatureFormat); // throws if the coutrycode is incorrect
        long ConvertftSignatureTypeToV2(long ftSignatureType); // throws if the coutrycode is incorrect
        long ConvertftJournalTypeToV1(long ftJournalType); // throws if the coutrycode is incorrect
    }
}
