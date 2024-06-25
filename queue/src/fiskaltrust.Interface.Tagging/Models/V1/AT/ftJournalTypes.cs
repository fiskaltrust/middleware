using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.AT
{
    [CaseExtensions(OnType = typeof(JournalRequest), OnField = nameof(JournalRequest.ftJournalType), Mask = 0xFFFF, Prefix = "V1", CaseName = "JournalType")]
    public enum ftJournalTypes : long
    {
        RKSVDEPExport0x0001 =  0x0001,
        StatusInformationQueueAT0x0000 =  0x0000,
    }
}