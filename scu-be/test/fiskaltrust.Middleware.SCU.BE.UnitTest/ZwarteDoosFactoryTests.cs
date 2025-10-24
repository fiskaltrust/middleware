using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.BE.UnitTest;

public class ZwarteDoosFactoryTests
{
    private readonly Mock<ILogger<ZwarteDoosFactory>> _loggerMock;
    private readonly ZwarteDoosScuConfiguration _configuration;

    public ZwarteDoosFactoryTests()
    {
        _loggerMock = new Mock<ILogger<ZwarteDoosFactory>>();
        _configuration = new ZwarteDoosScuConfiguration
        {
            ServiceUrl = "https://test.zwartedoos.be",
            ApiKey = "test-api-key",
            CompanyId = "BE123456789",
            SandboxMode = true,
            TimeoutSeconds = 30
        };
    }

    [Fact]
    public async Task SubmitInvoiceAsync_WithSuccessfulResponse_ShouldReturnSuccess()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var successResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"success":true,"signature":"test-signature","qrCode":"https://qr.test","transactionId":"tx-123","timestamp":"2024-01-01T12:00:00Z","errors":[]}""")
        };

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(successResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var factory = new ZwarteDoosFactory(_loggerMock.Object, httpClient, _configuration);

        var request = new ZwarteDoosInvoiceRequest
        {
            CompanyId = "BE123456789",
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.UtcNow,
            TotalAmount = 121.00m,
            VatAmount = 21.00m,
            Lines = new List<ZwarteDoosInvoiceLine>
            {
                new ZwarteDoosInvoiceLine
                {
                    Description = "Test Item",
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    VatRate = 21.00m,
                    Amount = 100.00m
                }
            }
        };

        // Act
        var result = await factory.SubmitInvoiceAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Signature.Should().Be("test-signature");
        result.QrCode.Should().Be("https://qr.test");
        result.TransactionId.Should().Be("tx-123");
    }

    [Fact]
    public async Task SubmitInvoiceAsync_WithHttpError_ShouldReturnFailure()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request")
        };

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(errorResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var factory = new ZwarteDoosFactory(_loggerMock.Object, httpClient, _configuration);

        var request = new ZwarteDoosInvoiceRequest
        {
            CompanyId = "BE123456789",
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.UtcNow,
            TotalAmount = 121.00m,
            VatAmount = 21.00m
        };

        // Act
        var result = await factory.SubmitInvoiceAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Contains("HTTP BadRequest"));
    }

    [Fact]
    public async Task CheckServiceStatusAsync_WithSuccessfulResponse_ShouldReturnTrue()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var successResponse = new HttpResponseMessage(HttpStatusCode.OK);

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(successResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var factory = new ZwarteDoosFactory(_loggerMock.Object, httpClient, _configuration);

        // Act
        var result = await factory.CheckServiceStatusAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckServiceStatusAsync_WithErrorResponse_ShouldReturnFalse()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(errorResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var factory = new ZwarteDoosFactory(_loggerMock.Object, httpClient, _configuration);

        // Act
        var result = await factory.CheckServiceStatusAsync();

        // Assert
        result.Should().BeFalse();
    }
}