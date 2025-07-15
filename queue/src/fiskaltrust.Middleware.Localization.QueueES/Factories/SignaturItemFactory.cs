using System.Web;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es.Cases;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueES.Factories;

public static class SignaturItemFactory
{
    public static SignatureItem CreateInitialOperationSignature(ftQueue queue)
    {
        return new SignatureItem()
        {
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypeES.InitialOperationReceipt.As<SignatureType>().WithFlag(SignatureTypeFlags.ArchivingRequired).WithCategory(SignatureTypeCategory.Information),
            Caption = $"Initial-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}"
        };
    }

    public static SignatureItem CreateOutOfOperationSignature(ftQueue queue)
    {
        return new SignatureItem()
        {
            ftSignatureType = SignatureTypeES.OutOfOperationReceipt.As<SignatureType>().WithFlag(SignatureTypeFlags.ArchivingRequired).WithCategory(SignatureTypeCategory.Information),
            ftSignatureFormat = SignatureFormat.Text,
            Caption = $"Out-of-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}"
        };
    }
}
