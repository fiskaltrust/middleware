namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants
{
#pragma warning disable CA1028
    public enum SignatureTypesDE : long
    {
        QrCodeAccordingKassenSichV = 0x4445000000000001,
        ArchivingRequired = 0x4445000000000002,
        Notification = 0x4445000000000003,
        StartTransactionResult = 0x4445000000000010,
        FinishTransactionPayload = 0x4445000000000011,
        FinishTransactionResult = 0x4445000000000012,
        QrCodeVersion = 0x4445000000000013,
        CashBoxIdentification = 0x4445000000000014,
        ProcessType = 0x4445000000000015,
        ProcessData = 0x4445000000000016,
        TransactionNumber = 0x4445000000000017,
        SignatureCounter = 0x4445000000000018,
        StartTime = 0x4445000000000019,
        LogTime = 0x444500000000001A,
        SignaturAlgorithm = 0x444500000000001B,
        LogTimeFormat = 0x444500000000001C,
        Signature = 0x444500000000001D,
        PublicKey = 0x444500000000001E,
        VorgangsBeginn = 0x444500000000001F
    }
}
