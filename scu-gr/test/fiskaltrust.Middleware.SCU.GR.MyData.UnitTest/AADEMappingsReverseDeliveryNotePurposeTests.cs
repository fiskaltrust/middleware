using System;
using fiskaltrust.Middleware.SCU.GR.MyData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest.SCU.MyData;

public class AADEMappingsReverseDeliveryNotePurposeTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void GetReverseDeliveryNotePurpose_ValidValues_ShouldReturnSameValue(int purpose)
    {
        var result = AADEMappings.GetReverseDeliveryNotePurpose(purpose);

        result.Should().Be(purpose);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    [InlineData(100)]
    public void GetReverseDeliveryNotePurpose_InvalidValues_ShouldThrow(int invalidPurpose)
    {
        var act = () => AADEMappings.GetReverseDeliveryNotePurpose(invalidPurpose);

        act.Should().Throw<Exception>()
            .WithMessage($"*{invalidPurpose}*");
    }
}