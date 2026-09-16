using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePL.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePL.Factories;

public static class SignaturItemFactory
{
    public static SignatureItem CreateInitialOperationSignature(ftQueue queue)
    {
        return new SignatureItem()
        {
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypePL.InitialOperationReceipt.As<SignatureType>(),
            Caption = $"Initial-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}"
        };
    }

    public static SignatureItem CreateOutOfOperationSignature(ftQueue queue)
    {
        return new SignatureItem()
        {
            ftSignatureType = SignatureTypePL.OutOfOperationReceipt.As<SignatureType>(),
            ftSignatureFormat = SignatureFormat.Text,
            Caption = $"Out-of-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}"
        };
    }

    public static SignatureItem CreateStoredNotFiscalizedSignature()
    {
        return new SignatureItem()
        {
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypePL.StoredNotFiscalized.As<SignatureType>(),
            Caption = "Stored, not fiscalized",
            Data = "The invoice was persisted by the middleware but not transmitted to KSeF. Configure an SCU.PL.KSeF to fiscalize invoice cases."
        };
    }
}
