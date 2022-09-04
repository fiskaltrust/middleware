namespace fiskaltrust.Middleware.Contracts.Constants
{
    public enum JournalTypes : long
    {
        VersionInformation =     0x0000000000000000,
        ActionJournal =          0x0000000000000001,
        ReceiptJournal =         0x0000000000000002,
        QueueItem =              0x0000000000000003,
        Configuration =          0x00000000000000FF,
        JournalAT =              0x0000000000004154,
        JournalDE =              0x0000000000004445,
        JournalFR =              0x0000000000004652,
        QueueDEStatus =          0x4445000000000000,
        TarExportFromTSE =       0x4445000000000001,
        DSFinVKExport =          0x4445000000000002,
        TarExportFromDatabase =  0x4445000000000003,
        QueueFRStatus =          0x4652000000000000,
        TicketJournalsFR =       0x4652000000000001,
        PaymentProveJournalsFR = 0x4652000000000002,
        InvoiceJournalsFR =      0x4652000000000003,
        GrandTotalJournalsFR =   0x4652000000000004,
        BillJournalsFR =         0x4652000000000005,
        ArchiveJournalsFR =      0x4652000000000006,
        LogJournalsFR =          0x4652000000000007,
        CopyJournalsFR =         0x4652000000000008,
        TrainingJournalsFR =     0x4652000000000009,
        Archive =                0x4652000000000010,
        RKSV =                   0x4154000000000001
    }
}
