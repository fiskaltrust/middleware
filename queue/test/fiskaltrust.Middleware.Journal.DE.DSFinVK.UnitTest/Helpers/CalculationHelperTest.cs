using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Helpers
{
    public class CalculationHelperTest
    {
        [Fact]
        public void GetAmount_NoCanccleFlag_ReviseAmountWhenNegativeTest()
        {
            CalculationHelper.GetAmount(0x0000_0000_0000_0000, 1, 5).Should().Be(5);
            CalculationHelper.GetAmount(0x0000_0000_0000_0000, -1, 5).Should().Be(-5);
            CalculationHelper.GetAmount(0x0000_0000_0000_0000, 1, -5).Should().Be(-5);
            CalculationHelper.GetAmount(0x0000_0000_0000_0000, -1, -5).Should().Be(-5);
        }

        [Fact]
        public void GetAmount_WithCanccleFlag_ReviseAmountWhenNegativeTest()
        {
            CalculationHelper.GetAmount(0x0000_0000_0020_0000, 1, 5).Should().Be(5);
            CalculationHelper.GetAmount(0x0000_0000_0020_0000, -1, 5).Should().Be(5);
            CalculationHelper.GetAmount(0x0000_0000_0020_0000, 1, -5).Should().Be(-5);
            CalculationHelper.GetAmount(0x0000_0000_0020_0000, -1, -5).Should().Be(-5);
        }
    }
}
