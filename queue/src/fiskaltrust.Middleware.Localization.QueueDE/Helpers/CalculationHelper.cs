using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;

namespace fiskaltrust.Middleware.Localization.QueueDE.Helpers
{
    public static class CalculationHelper
    {
        public static decimal GetAmount(PayItem payItem, decimal amount)
        {
            if (payItem.IsPositionCancellation())
            {
                return amount;
            }
            else
            {
                return payItem.Quantity < 0 && amount > 0 ? -Math.Abs(amount) : amount;
            }
        }
    }
}
