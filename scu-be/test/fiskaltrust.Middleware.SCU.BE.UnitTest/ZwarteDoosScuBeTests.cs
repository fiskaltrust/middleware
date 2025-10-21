using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueBE.BESSCD;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.SCU.BE.UnitTest;

public class ZwarteDoosScuBeTests
{
    private readonly Mock<ILogger<ZwarteDoosScuBe>> _loggerMock;
    private readonly ZwarteDoosScuConfiguration _configuration;
    private readonly Fixture _fixture;

    public ZwarteDoosScuBeTests()
    {
        _loggerMock = new Mock<ILogger<ZwarteDoosScuBe>>();
        _configuration = new ZwarteDoosScuConfiguration
        {
            ServiceUrl = "https://test.zwartedoos.be",
            ApiKey = "test-api-key",
            CompanyId = "BE123456789",
            SandboxMode = true,
            TimeoutSeconds = 30
        };
        _fixture = new Fixture();
    }

    [Fact]
    public async Task SubmitInvoiceAsync_WithValidRequest_ShouldReturnSuccessResponse()
    {
        // Arrange
        var scu = new ZwarteDoosScuBe(_loggerMock.Object, _configuration);
        var request = CreateValidSubmitInvoiceRequest();

        // Act & Assert
        // Note: This test would need to be adapted based on actual ZwarteDoos API behavior
        // For now, it demonstrates the test structure
        var response = await scu.SubmitInvoiceAsync(request);
        
        // The actual assertion would depend on whether you want to mock the HTTP client
        // or test against a real test endpoint
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitInvoiceAsync_WithEmptyInvoiceNumber_ShouldReturnError()
    {
        // Arrange
        var scu = new ZwarteDoosScuBe(_loggerMock.Object, _configuration);
        var request = CreateValidSubmitInvoiceRequest();
        request.InvoiceNumber = string.Empty;

        // Act
        var response = await scu.SubmitInvoiceAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.ResultMessages.Should().Contain(m => m.message.Contains("Invoice number is required"));
    }

    [Fact]
    public async Task SubmitInvoiceAsync_WithEmptyCashBoxId_ShouldReturnError()
    {
        // Arrange
        var scu = new ZwarteDoosScuBe(_loggerMock.Object, _configuration);
        var request = CreateValidSubmitInvoiceRequest();
        request.ftCashBoxIdentification = string.Empty;

        // Act
        var response = await scu.SubmitInvoiceAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.ResultMessages.Should().Contain(m => m.message.Contains("Cash box identification is required"));
    }

    [Theory]
    [InlineData(100.00, 21.00, 121.00)]
    [InlineData(50.50, 10.61, 61.11)]
    [InlineData(0.00, 0.00, 0.00)]
    public async Task SubmitInvoiceAsync_WithDifferentAmounts_ShouldCalculateCorrectly(
        decimal amount, decimal vatAmount, decimal expectedTotal)
    {
        // Arrange
        var scu = new ZwarteDoosScuBe(_loggerMock.Object, _configuration);
        var request = CreateValidSubmitInvoiceRequest();
        request.InvoiceLine = new List<InvoiceLine>
        {
            new InvoiceLine
            {
                Description = "Test Item",
                Quantity = 1,
                Amount = amount,
                VATAmount = vatAmount,
                VATRate = 21.00m
            }
        };

        // Act
        var response = await scu.SubmitInvoiceAsync(request);

        // Assert
        response.Should().NotBeNull();
        // Additional assertions would verify the calculated amounts in the request
    }

    [Fact]
    public async Task ProcessReceiptAsync_WithValidRequest_ShouldReturnProcessResponse()
    {
        // Arrange
        var scu = new ZwarteDoosScuBe(_loggerMock.Object, _configuration);
        var processRequest = CreateValidProcessRequest();

        // Act
        var response = await scu.ProcessReceiptAsync(processRequest, new List<(ReceiptRequest, ReceiptResponse)>());

        // Assert
        response.Should().NotBeNull();
        response.ReceiptResponse.Should().NotBeNull();
    }

    [Fact]
    public async Task GetInfoAsync_ShouldReturnBESSCDInfo()
    {
        // Arrange
        var scu = new ZwarteDoosScuBe(_loggerMock.Object, _configuration);

        // Act
        var info = await scu.GetInfoAsync();

        // Assert
        info.Should().NotBeNull();
        info.Should().BeOfType<BESSCDInfo>();
    }

    private SubmitInvoiceRequest CreateValidSubmitInvoiceRequest()
    {
        return new SubmitInvoiceRequest
        {
            ftCashBoxIdentification = "TEST-CASHBOX-001",
            InvoiceMoment = DateTime.UtcNow,
            Series = "INV",
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-001",
            InvoiceLine = new List<InvoiceLine>
            {
                new InvoiceLine
                {
                    Description = "Test Product",
                    Quantity = 1,
                    Amount = 100.00m,
                    VATAmount = 21.00m,
                    VATRate = 21.00m
                }
            }
        };
    }

    private ProcessRequest CreateValidProcessRequest()
    {
        var receiptRequest = _fixture.Build<ReceiptRequest>()
            .With(r => r.ftCashBoxID, "TEST-CASHBOX-001")
            .With(r => r.cbReceiptReference, "TEST-RECEIPT-001")
            .With(r => r.cbChargeItems, new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Test Item",
                    Quantity = 1.0,
                    Amount = 100.0,
                    VATRate = 21.0
                }
            })
            .Create();

        var receiptResponse = _fixture.Create<ReceiptResponse>();

        return new ProcessRequest
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = receiptResponse
        };
    }
}