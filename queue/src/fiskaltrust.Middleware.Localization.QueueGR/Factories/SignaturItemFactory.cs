using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueGR.Interface;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueGR.Factories;

public static class SignaturItemFactory
{
    public static SignaturItem CreateInitialOperationSignature(ftQueue queue)
    {
        return new SignaturItem()
        {
            ftSignatureType = (long) SignatureTypesGR.InitialOperationReceipt,
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            Caption = $"Initial-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}"
        };
    }

    public static SignaturItem CreateOutOfOperationSignature(ftQueue queue)
    {
        return new SignaturItem()
        {
            ftSignatureType = (long) SignatureTypesGR.OutOfOperationReceipt,
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            Caption = $"Out-of-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}"
        };
    }

    public static SignaturItem CreateGRQRCode(string qrCode)
    {
        return new SignaturItem()
        {
            Caption = "[www.fiskaltrust.gr]",
            Data = qrCode,
            ftSignatureFormat = (long) SignaturItem.Formats.QR_Code,
            ftSignatureType = (long) SignatureTypesGR.PosReceipt
        };
    }
}
