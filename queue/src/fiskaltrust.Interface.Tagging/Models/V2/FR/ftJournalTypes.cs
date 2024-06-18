using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2.FR
{
    //[CaseExtensions(OnType = typeof(JournalRequest), OnField = nameof(JournalRequest.ftJournalType), Mask = 0xFFFF, Prefix = "V2", CaseName = "JournalType")]
    public enum ftJournalTypes : long
    {
        StatusInformationQueueFR0x1000 = 0x1000,
        TicketExport0x1001 = 0x1001,
        PaymentProveExport0x1002 = 0x1002,
        InvoiceExport0x1003 = 0x1003,
        GrandTotalExport0x1004 = 0x1004,
        BillExport0x1007 = 0x1007,
        ArchiveExport0x1008 = 0x1008,        
        LogExport0x1009 = 0x1009,
        CopyExport0x100A = 0x100A,
        TrainingExport0x100B = 0x100B,
        ConjunctionArchivExport0x1010 = 0x1010
    }
}