using System;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Queue
{
    public class SignatureFactory
    {
        public SignaturItem CreateSandboxSignature(Guid queueId) =>
            new SignaturItem
            {
                Caption = "S A N D B O X",
                Data = queueId.ToString(),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = (long) SignaturItem.Types.Unknown
            };

        public SignaturItem CreateEReceiptLinkSignature(bool isSandbox, Guid queueId, Guid queueItemId) =>
            new SignaturItem
            {
                Caption = "Electronic receipt",
                Data = isSandbox ? $"https://receipts-sandbox.fiskaltrust.cloud/v0/{queueId}/{queueItemId}" : $"https://receipts.fiskaltrust.cloud/v0/{queueId}/{queueItemId}",
                ftSignatureFormat = (long) SignaturItem.Formats.QR_Code,
                ftSignatureType = 0x4445000000000001
            };
    }
}
