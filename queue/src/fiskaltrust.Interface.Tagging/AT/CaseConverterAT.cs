using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.Interface.Tagging.Interfaces;

namespace fiskaltrust.Interface.Tagging.AT
{
    public class CaseConverterAT : ICaseConverter
    {
        long ICaseConverter.ConvertftChargeItemCaseToV1(long ftChargeItemCase) => throw new NotImplementedException();
        long ICaseConverter.ConvertftJournalTypeToV1(long ftJournalType) => throw new NotImplementedException();
        long ICaseConverter.ConvertftPayItemCaseToV1(long ftPayItemCase) => throw new NotImplementedException();
        long ICaseConverter.ConvertftReceiptCaseToV1(long ftReceiptCase) => throw new NotImplementedException();
        long ICaseConverter.ConvertftSignatureFormatToV2(long ftSignatureFormat) => throw new NotImplementedException();
        long ICaseConverter.ConvertftSignatureTypeToV2(long ftSignatureType) => throw new NotImplementedException();
        long ICaseConverter.ConvertftStateToV2(long ftstate) => throw new NotImplementedException();
    }
}
