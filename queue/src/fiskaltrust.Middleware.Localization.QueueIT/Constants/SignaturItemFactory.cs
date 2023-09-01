using System;
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
                ftSignatureType = 0x4954_2000_0001_1001,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                Caption = $"Initial-operation receipt",
                Data = $"Queue-ID: {queueIT.ftQueueITId} Serial-Nr: {rtInfo.SerialNumber}"
            };
        }

        public static SignaturItem CreateOutOfOperationSignature(ftQueueIT queueIT)
        {
            return new SignaturItem()
            {
                ftSignatureType = 0x4954_2000_0001_1002,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                Caption = $"Out-of-operation receipt",
                Data = $"Queue-ID: {queueIT.ftQueueITId}"
            };
        }
    }
}
