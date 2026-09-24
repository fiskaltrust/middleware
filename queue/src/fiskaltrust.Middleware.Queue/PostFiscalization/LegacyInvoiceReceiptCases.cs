using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.PostFiscalization;
using V2 = fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Queue.PostFiscalization
{
    /// <summary>
    /// Which receipts the eInvoicing service is called for on the legacy stack (RFC 712). The shared rule applies first:
    /// the <c>0x1000</c> invoice type nibble of <c>ftReceiptCase</c>, which the v2 markets and IT carry. The v1 receipt
    /// cases of the other legacy markets have no type nibble, so their invoice document types are listed here per
    /// market; a case matches on country and case number, whatever flags are set in between.
    /// </summary>
    public static class LegacyInvoiceReceiptCases
    {
        private const ulong CountryAndCaseMask = 0xFFFF_0000_0000_FFFF;

        /// <summary>The v1 receipt cases that are invoice document types, by market. AT and FR are pending decisions in the RFC.</summary>
        public static readonly IReadOnlyCollection<ulong> AllowList = new HashSet<ulong>
        {
            0x4445_0000_0000_000C, // DE B2B-invoice
            0x4445_0000_0000_000D, // DE B2C-invoice
        };

        public static bool IsInvoiceDocument(V2.ReceiptRequest request)
            => PostFiscalizationProcessor.IsInvoiceDocument(request)
               || AllowList.Contains((ulong) request.ftReceiptCase & CountryAndCaseMask);
    }
}
