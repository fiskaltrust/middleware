namespace fiskaltrust.Middleware.Localization.QueuePT.Interface;

// _CCCC_vlll_gggg_tsss 
public enum SignatureTypesPT : long
{
    InitialOperationReceipt = 0x5054_2000_0001_1001,
    OutOfOperationReceipt = 0x5054_2000_0001_1002,
    PosReceipt = 0x5054_2000_0000_0001,

    ATCUD = 0x5054_2000_0000_0010,
    Hash = 0x5054_2000_0000_0012,
    // TBD define signaturetypes => interface ??
}
