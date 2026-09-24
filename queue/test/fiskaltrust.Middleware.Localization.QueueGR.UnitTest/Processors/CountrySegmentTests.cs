using fiskaltrust.Middleware.Localization.QueueGR.Processors;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.Processors;

public class CountrySegmentTests
{
    [Theory]
    [InlineData("ft1#CB-A-42", "CB-A", 42L)]
    [InlineData("ft1A#CB-A-26", "CB-A", 26L)]                 // hex prefix, decimal aa
    [InlineData("ft1#S-1", "S", 1L)]
    [InlineData("ft1#e9c02867-fbd6-4c50-88e6-6b6892dcbf2b-105", "e9c02867-fbd6-4c50-88e6-6b6892dcbf2b", 105L)] // dashed series (typical CashBoxIdentification)
    [InlineData("ft1#CB-A-007", "CB-A", 7L)]                  // leading zeros
    [InlineData("ft1#CB-A--5", "CB-A-", 5L)]                  // split is at the LAST dash — the series may even end with one
    [InlineData("#S-1", "S", 1L)]                             // no prefix before '#'
    public void TryParse_ValidSegment_ReturnsSeriesAndAa(string identification, string expectedSeries, long expectedAa)
    {
        CountrySegment.TryParse(identification, out var series, out var aa).Should().BeTrue();
        series.Should().Be(expectedSeries);
        aa.Should().Be(expectedAa);
    }

    [Theory]
    [InlineData(null)]                              // nothing at all
    [InlineData("")]
    [InlineData("ft1")]                             // no '#'
    [InlineData("ft1#")]                            // nothing after '#'
    [InlineData("ft1#CBA42")]                       // no dash in segment
    [InlineData("ft1#-42")]                         // empty series
    [InlineData("ft1#CB-A-")]                       // empty aa
    [InlineData("ft1#CB-A-4x2")]                    // non-numeric aa
    [InlineData("ft1#CB-A- 42")]                    // whitespace not allowed (NumberStyles.None)
    [InlineData("ft1#CB-A-+42")]                    // sign not allowed
    [InlineData("ft1#CB-A-99999999999999999999")]   // long overflow
    public void TryParse_InvalidSegment_ReturnsFalse(string? identification)
    {
        CountrySegment.TryParse(identification, out _, out _).Should().BeFalse();
    }
}
