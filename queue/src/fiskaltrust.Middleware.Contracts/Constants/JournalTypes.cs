namespace fiskaltrust.Middleware.Contracts.Constants
{
    public enum JournalTypes : long
    {
        VersionInformation =    0x0000000000000000,
        ActionJournal =         0x0000000000000001,
        ReceiptJournal =        0x0000000000000002,
        QueueItem =             0x0000000000000003,
        Configuration =         0x00000000000000FF,
        JournalAT =             0x0000000000004154,
        JournalDE =             0x0000000000004445,
        JournalFR =             0x0000000000004652,
        QueueDEStatus =         0x4445000000000000,
        TarExportFromTSE =      0x4445000000000001,
        DSFinVKExport =         0x4445000000000002,
        TarExportFromDatabase = 0x4445000000000003,
        CashDepositME =         0x44D5000000010007,
        CashWithdrawlME =       0x44D5000000010008,
    }
}
