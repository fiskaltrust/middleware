using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Helpers
{
    public class TaxCalculatorTests
    {
        [Theory]
        [InlineData(11, 10, 1)]
        [InlineData(-11, 10, -1)]
        [InlineData(20, 7, 1.308411)]
        [InlineData(0, 7, 0)]
        public void CalculateVAT_Should_Return_CorrectVAT(decimal amount, decimal vatRate, decimal expectedVat) 
            => TaxCalculator.CalculateVAT(amount, vatRate).Should().BeApproximately(expectedVat, 0.0001M);
    }
}
