namespace fiskaltrust.Middleware.Localization.QueueDE
{
    public enum SignatureTypesDE
    {
        QrCodeAccordingKassenSichV = 0x01,
        ArchivingRequired = 0x02,
        Notification = 0x03,
        StartTransactionResult = 0x10,
        FinishTransactionPayload = 0x11,
        FinishTransactionResult = 0x12,
        QrCodeVersion = 0x13,
        CashBoxIdentification = 0x14,
        ProcessType = 0x15,
        ProcessData = 0x16,
        TransactionNumber = 0x17,
        SignatureCounter = 0x18,
        StartTime = 0x19,
        LogTime = 0x1A,
        SignaturAlgorithm = 0x1B,
        LogTimeFormat = 0x1C,
        Signature = 0x1D,
        PublicKey = 0x1E,
        VorgangsBeginn = 0x1F,
        UpdateTransactionPayload = 0x20,
        UpdateTransactionResult = 0x21,
        CertificationId = 0x22
    }
}
