using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Queue.PostFiscalization
{
    /// <summary>
    /// Whether a legacy market fiscalized a receipt (RFC 712). The market processors return no explicit outcome: a receipt
    /// they do not process is answered with a non-error response whose <c>ftState</c> carries a market-specific flag or
    /// code, and the error state marks a failed one. Those answers are listed here per market, so the finalize phase runs
    /// only for fiscalized receipts. Anything else a market returns with a non-error state is a fiscalized receipt,
    /// including the modes in which a market records receipts although its security mechanism failed (SSCD failed
    /// mode, late signing) and the informational flags of AT, DE and FR. AT is the one market whose non-fiscalizing
    /// answers carry no state at all; there the signature items tell. The receipt journal policy, which the markets'
    /// answers shaped long before this, is not affected by this predicate.
    /// </summary>
    public static class LegacyFiscalizationOutcome
    {
        /// <summary>The error state of a failed receipt, in the low 32 bits of <c>ftState</c>.</summary>
        public const long ErrorState = 0xEEEE_EEEE;

        /// <summary>Bit 0 on every market: the answer to a receipt on a queue that is not operational, not yet activated or already deactivated.</summary>
        public const long SecurityMechanismDeactivatedFlag = 0x0000_0000_0000_0001;

        /// <summary>DE, bit 8: an SCU switch is in progress; the receipt was not signed and the POS repeats it after the switch.</summary>
        public const long DeScuSwitchInProgressFlag = 0x0000_0000_0000_0100;

        /// <summary>ME: the whole low 32 bits of a receipt that was not processed, as <c>SetStateToError</c> writes them (error, cash deposit outstanding, invoice already received).</summary>
        public static readonly IReadOnlyCollection<long> MeNotProcessedStates = new[] { 3L, 4L, 5L };

        /// <summary>A queue that has been activated and not deactivated since: the only state in which the market processors fiscalize.</summary>
        public static bool IsOperational(ftQueue queue) => queue.StartMoment.HasValue && !queue.StopMoment.HasValue;

        /// <param name="country">The market that answered, as the queue item records it.</param>
        /// <param name="queueWasOperational">Whether the queue was operational when the receipt arrived.</param>
        /// <param name="queueIsOperational">Whether the queue is operational after the market processor returned.</param>
        public static bool IsFiscalized(string country, ReceiptResponse receiptResponse, bool queueWasOperational, bool queueIsOperational)
        {
            var state = receiptResponse.ftState & 0xFFFF_FFFF;
            if (state == ErrorState)
            {
                return false;
            }

            // Every market answers a receipt on a queue that is not operational with this flag (IT's queue-state checks,
            // DE's and AT's DisabledQueueReceiptCommand, FR's queue-state validation). The flag alone cannot tell, because
            // the out-of-operation receipt carries it too and is fiscalized, on the still operational queue; the
            // initial-operation receipt is what makes a queue operational.
            if ((state & SecurityMechanismDeactivatedFlag) != 0 && !queueWasOperational && !queueIsOperational)
            {
                return false;
            }

            switch (country?.ToUpperInvariant())
            {
                case "AT":
                    // AT signs every receipt it processes, in SSCD failed mode included (the "Sicherheitseinrichtung
                    // ausgefallen" QR code), so the signature items are its explicit outcome. The lifecycle commands
                    // (initial operation, out of operation, monthly and yearly closing) answer a request that carries
                    // charge or pay items with the unsigned default response and an action journal entry only.
                    return receiptResponse.ftSignatures != null && receiptResponse.ftSignatures.Length > 0;
                case "DE":
                    return (state & DeScuSwitchInProgressFlag) == 0;
                case "ME":
                    return !MeNotProcessedStates.Contains(state);
                default:
                    // FR and IT answer without fiscalizing only on a queue that is not operational.
                    return true;
            }
        }
    }
}
