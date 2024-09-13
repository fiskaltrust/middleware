using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Localization.QueuePT.Interface;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePT.Factories
{
    public static class SignaturItemFactory
    {
        public static SignaturItem CreateInitialOperationSignature(ftQueue queue)
        {
            return new SignaturItem()
            {
                ftSignatureType = (long) SignatureTypesPT.InitialOperationReceipt,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                Caption = $"Initial-operation receipt",
                Data = $"Queue-ID: {queue.ftQueueId}"
            };
        }

        public static SignaturItem CreateOutOfOperationSignature(ftQueue queue)
        {
            return new SignaturItem()
            {
                ftSignatureType = (long) SignatureTypesPT.OutOfOperationReceipt,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                Caption = $"Out-of-operation receipt",
                Data = $"Queue-ID: {queue.ftQueueId}"
            };
        }

        public static SignaturItem CreatePTQRCode(string qrCode)
        {
            return new SignaturItem()
            {
                Caption = "[www.fiskaltrust.pt]",
                Data = qrCode,
                ftSignatureFormat = (long) SignaturItem.Formats.QR_Code,
                ftSignatureType = (long) SignatureTypesPT.PosReceipt
            };
        }
    }
}
