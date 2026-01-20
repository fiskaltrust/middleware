using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.QueueGR.Models;

namespace fiskaltrust.Middleware.Localization.QueueGR.Factories;

public static class SignaturItemFactory
{
    public static SignatureItem CreateInitialOperationSignature(ftQueue queue)
    {
        return new SignatureItem()
        {
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypeGR.InitialOperationReceipt.As<SignatureType>(),
            Caption = $"Initial-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}"
        };
    }

    public static SignatureItem CreateOutOfOperationSignature(ftQueue queue)
    {
        return new SignatureItem()
        {
            ftSignatureType = SignatureTypeGR.OutOfOperationReceipt.As<SignatureType>(),
            ftSignatureFormat = SignatureFormat.Text,
            Caption = $"Out-of-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}"
        };
    }

    public static SignatureItem CreateGRQRCode(string qrCode)
    {
        return new SignatureItem()
        {
            Caption = "[www.fiskaltrust.gr]",
            Data = qrCode,
            ftSignatureFormat = SignatureFormat.QRCode,
            ftSignatureType = SignatureTypeGR.PosReceipt.As<SignatureType>()
        };
    }
}
