using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.storage.V0;
namespace fiskaltrust.Middleware.Localization.QueueIT.Constants
{
    public static class SignaturItemFactory
    {
        public static SignaturItem CreateInitialOperationSignature(ftQueueIT queueIT, RTInfo rtInfo)
        {
            return new SignaturItem()
            {
                ftSignatureType = Cases.BASE_STATE & 0x3,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                Caption = $"Initial-operation receipt",
                Data = $"Queue-ID: {queueIT.ftQueueITId} Serial-Nr: {rtInfo.SerialNumber}"
            };
        }

        public static SignaturItem CreateOutOfOperationSignature(ftQueueIT queueIT)
        {
            return new SignaturItem()
            {
                ftSignatureType = Cases.BASE_STATE & 0x4,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                Caption = $"Out-of-operation receipt",
                Data = $"Queue-ID: {queueIT.ftQueueITId}"
            };
        }
    }
}
