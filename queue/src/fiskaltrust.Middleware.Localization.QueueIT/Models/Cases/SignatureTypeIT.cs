using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueIT.Models.Cases;

/// <summary>
/// The Italian signature types. The RT* values are what the Italian SCUs (see SCU.IT.Abstraction
/// <c>SignatureTypesIT</c>) put on a response; the queue reads them to build the receipt identification,
/// the journal entry and the reference signatures of refunds, voids and reprints.
/// </summary>
public enum SignatureTypeIT : long
{
    InitialOperationReceipt = 0x4954_2000_0000_1001,
    OutOfOperationReceipt = 0x4954_2000_0000_1002,

    PosReceiptPrimarySignature = 0x4954_2000_0000_0001,
    PosReceiptSecondarySignature = 0x4954_2000_0000_0002,

    RTSerialNumber = 0x4954_2000_0000_0010,
    RTZNumber = 0x4954_2000_0000_0011,
    RTDocumentNumber = 0x4954_2000_0000_0012,
    RTDocumentMoment = 0x4954_2000_0000_0013,
    RTDocumentType = 0x4954_2000_0000_0014,
    RTLotteryID = 0x4954_2000_0000_0015,
    RTCustomerID = 0x4954_2000_0000_0016,
    RTServerShaMetadata = 0x4954_2000_0000_0017,
    RTAmount = 0x4954_2000_0000_0018,

    RTReferenceZNumber = 0x4954_2000_0000_0020,
    RTReferenceDocumentNumber = 0x4954_2000_0000_0021,
    RTReferenceDocumentMoment = 0x4954_2000_0000_0022,
}

public static class SignatureTypeITExt
{
    public static T As<T>(this SignatureTypeIT self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);

    public static bool IsType(this SignatureType self, SignatureTypeIT signatureTypeIT) => ((long) self & 0xFFFF) == ((long) signatureTypeIT & 0xFFFF);

    public static SignatureType WithType(this SignatureType self, SignatureTypeIT type) => (SignatureType) (((ulong) self & 0xFFFF_FFFF_FFFF_0000) | ((ulong) type & 0xFFFF));
}
