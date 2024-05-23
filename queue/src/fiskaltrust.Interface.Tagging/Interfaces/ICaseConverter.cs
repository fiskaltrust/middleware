using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Interface.Tagging.Interfaces
{
    public interface ICaseConverter
    {
        long ConvertftReceiptCaseToV1(long ftReceiptCase);// throws if the coutrycode is incorrect
        long ConvertftPayItemCaseToV1(long ftPayItemCase); 
        long ConvertftChargeItemCaseToV1(long ftChargeItemCase);
        long ConvertftStateToV2(long ftstate);
        long ConvertftSignatureFormatToV2(long ftSignatureFormat);
        long ConvertftSignatureTypeToV2(long ftSignatureType);
        long ConvertftJournalTypeToV1(long ftJournalType);
    }
}
