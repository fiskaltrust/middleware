using System;
using fiskaltrust.ifPOS.v2;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest
{
    public class AADEMappingsChargeItemTests
    {

        [Theory]
        [InlineData("PieCes", 1)]
        [InlineData("pieces", 1)]
        [InlineData("PIECES", 1)]
        [InlineData("Kg", 2)]
        [InlineData("Litres", 3)]
        [InlineData("Meters", 4)]
        [InlineData("SquareMeters", 5)]
        [InlineData("CubicMeters", 6)]
        [InlineData("OtherPieces", 7)]
        // legacy / fallback behavior
        [InlineData("Unknown", 1)]
        [InlineData("", 1)]
        [InlineData(null, 1)]
        public void GetMeasurementUnit_ShouldReturnExpectedValue(string unit, int expectedValue)
        {
            // Arrange
            var chargeItem = new ChargeItem
            {
                Unit = unit
            };

            // Act
            var result = AADEMappings.GetMeasurementUnit(chargeItem);

            // Assert
            result.Should().Be(expectedValue);
        }
    }
}
