using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Exceptions;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Extensions
{
    /// <summary>
    /// Provides extensions for working with ReceiptRequest. These extensions should be used to get
    /// information from the ReceiptRequest, such as which ftReceiptCase flags are set.
    /// For a more advanced example, refer to <see cref="fiskaltrust.Middleware.Localization.QueueIT.Extensions.ReceiptRequestExtensions"/> in the Italian market folder.
    /// </summary>
    public static class ReceiptRequestExtensions
    {
        public static bool IsSampleFlagSet(this ReceiptRequest receiptRequest)
        {
            return (receiptRequest.ftReceiptCase & 0x10000) > 0;
        }
    }
}