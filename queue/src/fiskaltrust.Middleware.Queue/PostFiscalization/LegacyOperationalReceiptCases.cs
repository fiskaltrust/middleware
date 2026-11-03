using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;
using V2 = fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Queue.PostFiscalization
{
    /// <summary>
    /// The operational receipts of the legacy markets (RFC 712): lifecycle receipts (activation, deactivation, closings)
    /// and zero receipts, which keep a queue operable and are never invoices. The preflight tolerates an unreachable
    /// service for them, and before a queue is operational only its activation receipt is presented to the services at
    /// all. IT carries the v2 type nibbles; the v1 markets number their cases per market, listed here.
    /// </summary>
    public static class LegacyOperationalReceiptCases
    {
        private const long CaseMask = 0xFFFF;

        private static readonly IReadOnlyDictionary<string, HashSet<long>> _operationalCases = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase)
        {
            ["DE"] = new HashSet<long> { 0x0002, 0x0003, 0x0004, 0x0005, 0x0006, 0x0007 }, // zero, initial operation, out of operation, monthly, yearly and daily closing
            ["AT"] = new HashSet<long> { 0x0002, 0x0003, 0x0004, 0x0005, 0x0006 },         // zero, initial operation, out of operation, monthly and yearly closing
            ["ME"] = new HashSet<long> { 0x0002, 0x0003, 0x0004, 0x0005, 0x0006 },         // zero, initial operation, out of operation, monthly and yearly closing
            ["FR"] = new HashSet<long> { 0x0004, 0x0005, 0x0006, 0x0007, 0x000F, 0x0010, 0x0011 }, // shift, daily, monthly and yearly receipts, zero receipt, start, stop
        };

        /// <summary>The receipt that makes a new queue operational, per market.</summary>
        private static readonly IReadOnlyDictionary<string, long> _activationCases = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
        {
            ["DE"] = 0x0003,
            ["AT"] = 0x0003,
            ["ME"] = 0x0003,
            ["FR"] = 0x0010,
            ["IT"] = 0x4001,
        };

        /// <summary>The processor's filter for the legacy stack, applied to the request mapped to the v2 contract.</summary>
        public static bool IsOperationalReceipt(V2.ReceiptRequest request)
        {
            var ftReceiptCase = unchecked((long) request.ftReceiptCase);
            return IsOperationalReceipt(Country(ftReceiptCase), ftReceiptCase);
        }

        public static bool IsOperationalReceipt(string country, long ftReceiptCase)
        {
            if (string.Equals(country, "IT", StringComparison.OrdinalIgnoreCase))
            {
                var receiptCase = (ReceiptCase) unchecked((ulong) ftReceiptCase);
                return receiptCase.IsType(ReceiptCaseType.Lifecycle) || receiptCase.IsType(ReceiptCaseType.DailyOperations);
            }

            // A countryless v1 case uses the numbering DE, AT and ME share.
            var cases = country != null && _operationalCases.TryGetValue(country, out var known) ? known : _operationalCases["DE"];
            return cases.Contains(ftReceiptCase & CaseMask);
        }

        public static bool IsActivationReceipt(string country, long ftReceiptCase)
            => (country != null && _activationCases.TryGetValue(country, out var activation) ? activation : _activationCases["DE"]) == (ftReceiptCase & CaseMask);

        /// <summary>
        /// Whether the market may fiscalize the receipt as far as the queue state tells: the queue is operational, or it is
        /// new and the receipt is its activation receipt. The markets answer every other receipt on a queue that is not
        /// operational without fiscalizing it, so the services are not asked about such a receipt.
        /// </summary>
        public static bool MayBeFiscalized(ftQueue queue, string country, long ftReceiptCase)
            => LegacyFiscalizationOutcome.IsOperational(queue)
               || (!queue.StartMoment.HasValue && !queue.StopMoment.HasValue && IsActivationReceipt(country, ftReceiptCase));

        private static string Country(long ftReceiptCase)
        {
            switch ((ulong) ftReceiptCase & 0xFFFF_0000_0000_0000)
            {
                case 0x4445_0000_0000_0000: return "DE";
                case 0x4154_0000_0000_0000: return "AT";
                case 0x4652_0000_0000_0000: return "FR";
                case 0x4954_0000_0000_0000: return "IT";
                case 0x4D45_0000_0000_0000: return "ME";
                default: return null;
            }
        }
    }
}
