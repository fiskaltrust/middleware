using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2.AT
{
    [CaseExtensions(OnType = typeof(JournalRequest), OnField = nameof(JournalRequest.ftJournalType), Mask = 0xFFFF, Prefix = "V2", CaseName = "JournalType")]
    public enum ftJournalTypesDE : long
    {
        StatusInformationQueueAT0x1000 = 0x1000,
        RksvDepExport0x1001 = 0x1001,
    }
}


