using System;
using System.Linq;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Storage.Base.Extensions
{
    public static class ReceiptRequestExtensions
    {
        private static readonly long[] _excludedForReference = {
            0x0002, //Zero-receipt
            0x0003, //Initial operation receipt
            0x0005, //Monthly-closing
            0x0006, //Yearly-closing
            0x0007  //Daily-closing
        };

        public static bool IsPosReceipt(this ReceiptRequest request) => !_excludedForReference.Contains(request.ftReceiptCase & 0xFFFF);
    }
}
