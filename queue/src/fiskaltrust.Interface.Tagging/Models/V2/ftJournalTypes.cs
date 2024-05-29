using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [CaseExtensions(OnType = typeof(JournalRequest), OnField = nameof(JournalRequest.ftJournalType), Mask = 0xFFFF)]
    public enum ftJournalTypes : long
    {
        VersionInformation0x0000 = 0x0000,
        ActionJournal0x0001 = 0x0001,
        ReceiptJournal0x0002 = 0x0002,
        QueueItemJournal0x0003 = 0x0003,
    }
}