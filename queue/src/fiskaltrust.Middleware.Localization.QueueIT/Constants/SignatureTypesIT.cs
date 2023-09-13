namespace fiskaltrust.Middleware.Localization.QueueIT.Constants
{
    public enum SignatureTypesIT
    {
        PosReceiptPrimarySignature = 0x01,
        PosReceiptSecondarySignature = 0x02,
        RTSerialNumber = 0x010,
        RTZNumber = 0x11,
        RTDocumentNumber = 0x12,
        RTDocumentMoment = 0x13,
        RTDocumentType = 0x14,
        RTLotteryID = 0x15,
        RTCustomerID = 0x16,
        RTServerShaMetadata = 0x17,
        RTAmount = 0x18,

        RTReferenceZNumber = 0x20,
        RTReferenceDocumentNumber = 0x21,
        RTReferenceDocumentMoment = 0x22
    }
}
