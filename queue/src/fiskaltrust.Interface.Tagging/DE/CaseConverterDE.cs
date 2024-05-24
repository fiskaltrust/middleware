using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using V1=fiskaltrust.Interface.Tagging.Models.V1;
using fiskaltrust.Interface.Tagging.Interfaces;
using V2=fiskaltrust.Interface.Tagging.Models.V2;

namespace fiskaltrust.Interface.Tagging.DE
{
    public class CaseConverterDE : ICaseConverter
    {
        long ICaseConverter.ConvertftChargeItemCaseToV1(long ftChargeItemCase) => throw new NotImplementedException();
        long ICaseConverter.ConvertftJournalTypeToV1(long ftJournalType) => throw new NotImplementedException();
        long ICaseConverter.ConvertftPayItemCaseToV1(long ftPayItemCase) => throw new NotImplementedException();
        long ICaseConverter.ConvertftReceiptCaseToV1(long ftReceiptCase)
        {
            //sample 
            
            var v2Key = Enum.GetName(typeof(V2.ftReceiptCases), 0xFFFF & ftReceiptCase);
            var v1Value = (V1.DE.ftReceiptCases) Enum.Parse(typeof(V1.DE.ftReceiptCases), v2Key);

            var result = ((ulong)ftReceiptCase & 0xFFFFFFFFFFFF0000) | (ulong) v1Value;

            //do the same for ftReceiptCaseflag

            return (long)result;
        }
        long ICaseConverter.ConvertftSignatureFormatToV2(long ftSignatureFormat) => throw new NotImplementedException();
        long ICaseConverter.ConvertftSignatureTypeToV2(long ftSignatureType) => throw new NotImplementedException();
        long ICaseConverter.ConvertftStateToV2(long ftstate) => throw new NotImplementedException();

    }
}
