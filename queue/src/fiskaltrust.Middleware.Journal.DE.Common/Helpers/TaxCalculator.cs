namespace fiskaltrust.Middleware.Journal.DE.Common.Helpers
{
    public static class TaxCalculator
    {
        public static decimal CalculateVAT(decimal amount, decimal vatRate) => amount - (amount / (1 + (vatRate / 100)));
    }
}
