using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fiskaltrust.Middleware.Localization.QueueDE.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.UnitTest.Helpers
{
    public class CalculationHelperTest
    {
        [Fact]
        public void ReviseAmountOnNegativeQuantity_Differentsign_ValidResult()
        {
            CalculationHelper.ReviseAmountOnNegativeQuantity(1, 10).Should().Be(10);
            CalculationHelper.ReviseAmountOnNegativeQuantity(1, -10).Should().Be(-10);
            CalculationHelper.ReviseAmountOnNegativeQuantity(-1, 10).Should().Be(-10);
            CalculationHelper.ReviseAmountOnNegativeQuantity(-1, -10).Should().Be(-10);

        }
    }
}
