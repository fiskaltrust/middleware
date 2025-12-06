namespace fiskaltrust.Middleware.SCU.GR.Abstraction;

public enum SignatureTypesGR
{
    PosReceipt = 0x01,
    MyDataInfo = 0x10,
    QRCode = 0x20,
    InvoiceMark = 0x30,
    AuthenticationCode = 0x31,
    TransmissionFailure = 0x40
}