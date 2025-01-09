namespace fiskaltrust.Middleware.Localization.QueueES.Interface;

public enum SignatureTypesES : long
{
    InitialOperationReceipt = 0x4553_2000_0001_1001,
    OutOfOperationReceipt = 0x4553_2000_0001_1002,
    QRCode = 0x4553_2000_0000_0001,
    NIF = 0x4553_2000_0000_0002,
    Signature = 0x4553_2000_0000_0003,
    Huella = 0x4553_2000_0000_0004,
    SignatureScope = 0x4553_2000_0000_0005,
}