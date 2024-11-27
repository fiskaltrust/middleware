using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueDE.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.UnitTest.Helpers
{
    public class CalculationHelperTest
    {
        [Fact]
        public void GetAmount_PosCancleFlagNotSet_ValidResult()
        {
            CalculationHelper.GetAmount(new PayItem() { Quantity = 1, ftPayItemCase = 0x0000_0000_0000_0000 }, 10).Should().Be(10);
            CalculationHelper.GetAmount(new PayItem() { Quantity = 1, ftPayItemCase = 0x0000_0000_0000_0000 }, -10).Should().Be(-10);
            CalculationHelper.GetAmount(new PayItem() { Quantity = -1, ftPayItemCase = 0x0000_0000_0000_0000 }, 10).Should().Be(-10);
            CalculationHelper.GetAmount(new PayItem() { Quantity = -1, ftPayItemCase = 0x0000_0000_0000_0000 }, -10).Should().Be(-10);
        }
        [Fact]
        public void GetAmount_PosCancleFlagSet_ValidResult()
        {
            CalculationHelper.GetAmount(new PayItem() { Quantity = 1, ftPayItemCase = 0x0000_0000_0020_0000 }, 10).Should().Be(10);
            CalculationHelper.GetAmount(new PayItem() { Quantity = 1, ftPayItemCase = 0x0000_0000_0020_0000 }, -10).Should().Be(-10);
            CalculationHelper.GetAmount(new PayItem() { Quantity = -1, ftPayItemCase = 0x0000_0000_0020_0000 }, 10).Should().Be(10);
            CalculationHelper.GetAmount(new PayItem() { Quantity = -1, ftPayItemCase = 0x0000_0000_0020_0000 }, -10).Should().Be(-10);
        }
    }
}
