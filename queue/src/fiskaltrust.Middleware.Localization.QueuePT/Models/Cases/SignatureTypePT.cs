using System;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;

// _CCCC_vlll_gggg_tsss 
public enum SignatureTypePT : long
{
    InitialOperationReceipt = 0x5054_2000_0001_1001,
    OutOfOperationReceipt = 0x5054_2000_0001_1002,
    PosReceipt = 0x5054_2000_0000_0001,

    ATCUD = 0x5054_2000_0000_0010,
    Hash = 0x5054_2000_0000_0012,
    HashPrint = 0x5054_2000_0000_0013,
    CertificationNo = 0x5054_2000_0000_0014,
    ReferenceForCreditNote = 0x5054_2000_0000_0015,
    PTAdditional = 0x5054_2000_0000_0016
}

public static class SignatureTypePTExt
{
    public static T As<T>(this SignatureTypePT self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);

    public static bool IsType(this SignatureType self, SignatureTypePT signatureTypePT) => ((long) self & 0xFFFF) == ((long) signatureTypePT & 0xFFFF);
    public static SignatureType WithType(this SignatureType self, SignatureTypePT state) => (SignatureType) ((ulong) self & 0xFFFF_FFFF_FFFF_0000 | (ulong) state & 0xFFFF);
    public static SignatureTypePT Type(this SignatureType self) => (SignatureTypePT) ((long) self & 0xFFFF);
}
