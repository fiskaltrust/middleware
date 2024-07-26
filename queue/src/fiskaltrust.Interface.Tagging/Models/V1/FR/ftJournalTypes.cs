using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;
namespace fiskaltrust.Interface.Tagging.Models.V1.FR
{
    [CaseExtensions(OnType = typeof(JournalRequest), OnField = nameof(JournalRequest.ftJournalType), Mask = 0xFFFF, Prefix = "V1", CaseName = "JournalType")]
    public enum ftJournalTypes : long
    {
        StatusInformationQueueFR0x0000 = 0x0000,
        TicketExport0x0001 = 0x0001,
        PaymentProveExport0x0002 = 0x0002,
        InvoiceExport0x0003 = 0x0003,
        GrandTotalExport0x0004 = 0x0004,
        BillExport0x0007 = 0x0007,
        ArchiveExport0x0008 = 0x0008,
        LogExport0x0009 = 0x0009,
        CopyExport0x000A = 0x000A,
        TrainingExport0x000B = 0x000B,
        ConjunctionArchivExport0x0010 = 0x0010
    }
}