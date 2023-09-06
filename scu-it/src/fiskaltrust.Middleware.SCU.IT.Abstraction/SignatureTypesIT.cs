namespace fiskaltrust.Middleware.SCU.IT.Abstraction;

public enum SignatureTypesIT
{
    PosPayloadToPrintReceipt = 0x01,
    RTSerialNumber = 0x010,
    RTZNumber = 0x11,
    RTDocumentNumber = 0x12,
    RTDocumentMoment = 0x13,
    RTDocumentType = 0x14,
    RTLotteryID = 0x15,
    RTCustomerID = 0x16,
    RTAmount = 0x17,
    CustomRTServerInfo = 0x18,
    CustomRTServerShaMetadata = 0x19
}