using System;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.ES.UnitTest;

public class ReceiptResponseHelperTests
{
    [Fact]
    public void GetNumSerieFacturaParts_WithSingleSlash_ReturnsSerieFacturaAndNumber()
    {
        // Arrange
        var receiptResponse = new ReceiptResponse
        {
            ftReceiptIdentification = "test#SERIES/12345"
        };

        // Act
        var (serieFactura, numFactura) = receiptResponse.GetNumSerieFacturaParts();

        // Assert
        serieFactura.Should().Be("SERIES");
        numFactura.Should().Be(12345);
    }

    [Fact]
    public void GetNumSerieFacturaParts_WithMultipleSlashes_UseLastAsNumber()
    {
        // Arrange
        var receiptResponse = new ReceiptResponse
        {
            ftReceiptIdentification = "test#SERIES/A/B/67890"
        };

        // Act
        var (serieFactura, numFactura) = receiptResponse.GetNumSerieFacturaParts();

        // Assert
        serieFactura.Should().Be("SERIES/A/B");
        numFactura.Should().Be(67890);
    }

    [Fact]
    public void GetNumSerieFacturaParts_WithSingleDash_ReturnsSerieFacturaAndNumber()
    {
        // Arrange
        var receiptResponse = new ReceiptResponse
        {
            ftReceiptIdentification = "test#SERIES-12345"
        };

        // Act
        var (serieFactura, numFactura) = receiptResponse.GetNumSerieFacturaParts();

        // Assert
        serieFactura.Should().Be("SERIES");
        numFactura.Should().Be(12345);
    }

    [Fact]
    public void GetNumSerieFacturaParts_WithMultipleDashes_UseLastAsNumber()
    {
        // Arrange
        var receiptResponse = new ReceiptResponse
        {
            ftReceiptIdentification = "test#SERIES-A-B-67890"
        };

        // Act
        var (serieFactura, numFactura) = receiptResponse.GetNumSerieFacturaParts();

        // Assert
        serieFactura.Should().Be("SERIES-A-B");
        numFactura.Should().Be(67890);
    }

    [Fact]
    public void GetNumSerieFacturaParts_WithMixedSeparators_UseLastSeparatorForNumber()
    {
        // Arrange
        var receiptResponse = new ReceiptResponse
        {
            ftReceiptIdentification = "test#SERIES/A-B/C-99999"
        };

        // Act
        var (serieFactura, numFactura) = receiptResponse.GetNumSerieFacturaParts();

        // Assert
        serieFactura.Should().Be("SERIES/A-B/C");
        numFactura.Should().Be(99999);
    }

    [Fact]
    public void GetNumSerieFacturaParts_WithDashFollowedBySlash_UseSlashForNumber()
    {
        // Arrange
        var receiptResponse = new ReceiptResponse
        {
            ftReceiptIdentification = "test#SERIES-A/54321"
        };

        // Act
        var (serieFactura, numFactura) = receiptResponse.GetNumSerieFacturaParts();

        // Assert
        serieFactura.Should().Be("SERIES-A");
        numFactura.Should().Be(54321);
    }

    [Fact]
    public void GetNumSerieFacturaParts_WithNoSeparator_ThrowsException()
    {
        // Arrange
        var receiptResponse = new ReceiptResponse
        {
            ftReceiptIdentification = "test#SERIES12345"
        };

        // Act
        Action act = () => receiptResponse.GetNumSerieFacturaParts();

        // Assert
        act.Should().Throw<Exception>()
            .WithMessage("*Needs at least one '/' or '-' separator*");
    }

    [Fact]
    public void GetNumSerieFacturaParts_WithInvalidNumber_ThrowsException()
    {
        // Arrange
        var receiptResponse = new ReceiptResponse
        {
            ftReceiptIdentification = "test#SERIES/ABC"
        };

        // Act
        Action act = () => receiptResponse.GetNumSerieFacturaParts();

        // Assert
        act.Should().Throw<Exception>()
            .WithMessage("*Last part after separator is not a valid number*");
    }

    [Fact]
    public void GetNumSerieFactura_WithValidFormat_ReturnsNumSerieFactura()
    {
        // Arrange
        var receiptResponse = new ReceiptResponse
        {
            ftReceiptIdentification = "test#SERIES/12345"
        };

        // Act
        var result = receiptResponse.GetNumSerieFactura();

        // Assert
        result.Should().Be("SERIES/12345");
    }

    [Fact]
    public void GetNumSerieFactura_WithoutHashSeparator_ThrowsException()
    {
        // Arrange
        var receiptResponse = new ReceiptResponse
        {
            ftReceiptIdentification = "testSERIES/12345"
        };

        // Act
        Action act = () => receiptResponse.GetNumSerieFactura();

        // Assert
        act.Should().Throw<Exception>()
            .WithMessage("*Needs exactly one '#'*");
    }
}
