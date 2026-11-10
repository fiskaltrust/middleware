using System;

namespace fiskaltrust.Middleware.Journal.DE.Common.Helpers
{
    public static class CalculationHelper
    {
        public static decimal GetAmount(long payOrChargeCase, decimal quantity, decimal amount)
        {
            if (IsPositionCancellation(payOrChargeCase))
            {
                return amount;
            }
            else
            {
                return quantity < 0 && amount > 0 ? -Math.Abs(amount) : amount;
            }
        }

        public static int GetSignOfAmount(long payOrChargeCase, decimal quantity, decimal amount) => Math.Sign(GetAmount(payOrChargeCase, quantity, amount));

        public static decimal InverseAmountOnPayitemReceivable(decimal amount) => amount * -1;

        /// <summary>
        /// Checks the position cancellation flag (bit 0x200000) on ftChargeItemCase/ftPayItemCase.
        /// Used internally for amount sign logic in <see cref="GetAmount"/>.
        /// Note: This does NOT determine P_STORNO. Once a transaction is TSE-signed P_STORNO must not be set (DSFinV-K 4.2.3);
        /// the only exception is the AbortReceipt 0x001A, whose positions were never signed (see IsAbortedTransaction in the DSFinV-K export).
        /// </summary>
        public static bool IsPositionCancellation(long payOrChargeCase) => (payOrChargeCase & 0x0000_0000_0020_0000) > 0x0000;
    }
}
