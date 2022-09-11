using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.Factories
{
    public interface ISignatureFactoryFR
    {
        SignaturItem CreateFailureRegisteredSignature(string fromReceipt, string toReceipt);
        SignaturItem CreateMessagePendingSignature();
        SignaturItem CreatePerpetualTotalSignature(ftQueueFR queueFR);
        (string hash, SignaturItem signatureItem, ftJournalFR journalFR) CreateTotalsSignature(ReceiptResponse receiptResponse, ftQueue queue, ftSignaturCreationUnitFR signaturCreationUnitFR, string payload, string description, SignaturItem.Formats format, SignaturItem.Types type);
        SignaturItem CreateTotalsSignatureWithoutSigning(string payload, string description, SignaturItem.Formats format, SignaturItem.Types type);
    }
}