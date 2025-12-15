using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;
using FluentAssertions;
using Xunit;
using System;

namespace fiskaltrust.Middleware.SCU.ES.UnitTest;

public class CountryType2MapperTests
{
    [Theory]
    [InlineData("US", CountryType2.US)]
    [InlineData("DE", CountryType2.DE)]
    [InlineData("FR", CountryType2.FR)]
    [InlineData("ES", CountryType2.ES)]
    [InlineData("GB", CountryType2.GB)]
    [InlineData("IT", CountryType2.IT)]
    [InlineData("PT", CountryType2.PT)]
    [InlineData("NL", CountryType2.NL)]
    [InlineData("BE", CountryType2.BE)]
    [InlineData("CH", CountryType2.CH)]
    public void TryParseCountryCode_WithValidCode_ReturnsTrue(string code, CountryType2 expected)
    {
        // Act
        var result = CountryType2Mapper.TryParseCountryCode(code, out var country);

        // Assert
        result.Should().BeTrue();
        country.Should().Be(expected);
    }

    [Theory]
    [InlineData("us", CountryType2.US)]
    [InlineData("de", CountryType2.DE)]
    [InlineData("fr", CountryType2.FR)]
    [InlineData(" ES ", CountryType2.ES)]
    [InlineData("  gb  ", CountryType2.GB)]
    public void TryParseCountryCode_WithVariousCases_ReturnsTrue(string code, CountryType2 expected)
    {
        // Act
        var result = CountryType2Mapper.TryParseCountryCode(code, out var country);

        // Assert
        result.Should().BeTrue();
        country.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ZZ")] // Invalid code
    [InlineData("USA")] // 3-letter code instead of 2
    [InlineData("123")] // Numeric
    public void TryParseCountryCode_WithInvalidCode_ReturnsFalse(string code)
    {
        // Act
        var result = CountryType2Mapper.TryParseCountryCode(code, out var country);

        // Assert
        result.Should().BeFalse();
        country.Should().Be(default(CountryType2));
    }

    [Theory]
    [InlineData("US", CountryType2.US)]
    [InlineData("DE", CountryType2.DE)]
    [InlineData("FR", CountryType2.FR)]
    [InlineData("ES", CountryType2.ES)]
    public void MapCustomerCountry_WithValidCode_ReturnsCorrectCountry(string code, CountryType2 expected)
    {
        // Act
        var result = CountryType2Mapper.MapCustomerCountry(code);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("us", CountryType2.US)]
    [InlineData("De", CountryType2.DE)]
    [InlineData(" FR ", CountryType2.FR)]
    public void MapCustomerCountry_WithVariousCases_NormalizesCorrectly(string code, CountryType2 expected)
    {
        // Act
        var result = CountryType2Mapper.MapCustomerCountry(code);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "Customer country code is required but was not provided or is empty.")]
    [InlineData("", "Customer country code is required but was not provided or is empty.")]
    [InlineData("   ", "Customer country code is required but was not provided or is empty.")]
    public void MapCustomerCountry_WithNullOrEmpty_ThrowsArgumentExceptionWithMessage(string code, string expectedMessage)
    {
        // Act
        var act = () => CountryType2Mapper.MapCustomerCountry(code);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"{expectedMessage}*")
            .And.ParamName.Should().Be("customerCountry");
    }

    [Theory]
    [InlineData("ZZ", "Invalid or unsupported customer country code: 'ZZ'. Expected a valid ISO 3166-1 alpha-2 country code (e.g., 'US', 'DE', 'ES', 'FR').")]
    [InlineData("USA", "Invalid or unsupported customer country code: 'USA'. Expected a valid ISO 3166-1 alpha-2 country code (e.g., 'US', 'DE', 'ES', 'FR').")]
    [InlineData("123", "Invalid or unsupported customer country code: '123'. Expected a valid ISO 3166-1 alpha-2 country code (e.g., 'US', 'DE', 'ES', 'FR').")]
    public void MapCustomerCountry_WithInvalidCode_ThrowsArgumentExceptionWithDetailedMessage(string code, string expectedMessage)
    {
        // Act
        var act = () => CountryType2Mapper.MapCustomerCountry(code);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"{expectedMessage}*")
            .And.ParamName.Should().Be("customerCountry");
    }
}
