using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants
{
    public static class Precisions
    {
        // Introduced in DSFinV-K 2.3 - the field allows 5 digits, but only 2 must be used
        public const int AggregatedCurrencyAmount2Digits = 2;
        public const int AggregatedCurrencyAmount = 5;
        public const int PricePerUnit = 5;
        public const int Quantity = 3;
        public const int CurrencyAmount = 2;
        public const int VatRate = 2;
    }
}
