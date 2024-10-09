using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Interface;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePT.Factories;

public static class SignaturItemFactory
{
    public static SignatureItem CreateInitialOperationSignature(ftQueue queue)
    {
        return new SignatureItem()
        {
            ftSignatureType = (long) SignatureTypesPT.InitialOperationReceipt,
            ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.Text,
            Caption = $"Initial-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}"
        };
    }

    public static SignatureItem CreateOutOfOperationSignature(ftQueue queue)
    {
        return new SignatureItem()
        {
            ftSignatureType = (long) SignatureTypesPT.OutOfOperationReceipt,
            ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.Text,
            Caption = $"Out-of-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}"
        };
    }

    public static SignatureItem CreatePTQRCode(string qrCode)
    {
        return new SignatureItem()
        {
            Caption = "[www.fiskaltrust.pt]",
            Data = qrCode,
            ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.QR_Code,
            ftSignatureType = (long) SignatureTypesPT.PosReceipt
        };
    }
}
