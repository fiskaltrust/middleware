using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.DE
{
    [CaseExtensions(OnType = typeof(JournalRequest), OnField = nameof(JournalRequest.ftJournalType), Mask = 0xFFFF, Prefix = "V1", CaseName = "JournalType")]
    public enum ftJournalTypes : long
    {
        StatusInformationQueueDE0x0000 = 0x0000,
        TARFileExportTse0x0001 = 0x0001,
        DsfinVKExport0x0002 = 0x0002,
        TARFileExport0x0003 = 0x0003
    }
}