using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.UnitTest.Extensions
{
    public class ReceiptRequestExtensionsTest
    {

        [Fact]
        public void GetAmount_ShouldAmountSign_WhenFlagIsSet()
        {
            ReceiptRequestExtensions.GetAmount(0x0000_0000_0020_0000, -1, 10).Should().Be(10);
            ReceiptRequestExtensions.GetAmount(0x0000_0000_0020_0000, 1, -10).Should().Be(-10);
            ReceiptRequestExtensions.GetAmount(0x0000_0000_0020_0000, -1, -10).Should().Be(-10);
            ReceiptRequestExtensions.GetAmount(0x0000_0000_0020_0000, 1, 10).Should().Be(10);
        }

        [Fact]
        public void GetAmount_ShouldConsiderQuantityAndAmountSign_WhenFlagIsNotSet()
        {
            ReceiptRequestExtensions.GetAmount(0x0000_0000_0000_0000, -1, 10).Should().Be(-10);
            ReceiptRequestExtensions.GetAmount(0x0000_0000_0000_0000, 1, -10).Should().Be(-10);
            ReceiptRequestExtensions.GetAmount(0x0000_0000_0000_0000, -1, -10).Should().Be(-10);
            ReceiptRequestExtensions.GetAmount(0x0000_0000_0000_0000, 1, 10).Should().Be(10);
        }

    }
}
