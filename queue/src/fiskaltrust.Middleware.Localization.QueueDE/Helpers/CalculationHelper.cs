using System;

namespace fiskaltrust.Middleware.Localization.QueueDE.Helpers
{
    public static class CalculationHelper
    {
        public static decimal ReviseAmountOnNegativeQuantity(decimal quantity, decimal amount) => quantity < 0 && amount > 0 ? -Math.Abs(amount) : amount;
    }
}
