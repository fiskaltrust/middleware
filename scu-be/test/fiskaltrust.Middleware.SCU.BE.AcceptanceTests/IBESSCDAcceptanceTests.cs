using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueBE.BESSCD;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace fiskaltrust.Middleware.SCU.BE.AcceptanceTests;

/// <summary>
/// Base class for Belgian SCU acceptance tests that provides common functionality 
/// for testing different SCU implementations against Belgian receipt cases.
/// </summary>
public abstract class IBESSCDAcceptanceTests
{
    protected readonly ITestOutputHelper _output;
    protected readonly ILogger _logger;

    protected IBESSCDAcceptanceTests(ITestOutputHelper output)
    {
        _output = output;
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var serviceProvider = serviceCollection.BuildServiceProvider();
        _logger = serviceProvider.GetRequiredService<ILogger<IBESSCDAcceptanceTests>>();
    }

    protected abstract IBESSCD GetSystemUnderTest(Dictionary<string, object>? configuration = null);

    protected virtual ReceiptRequest CreateBasicReceiptRequest(ReceiptCase receiptCase)
    {
        return new ReceiptRequest
        {
            ftCashBoxID = Guid.NewGuid(),
            ftQueueID = Guid.NewGuid(),
            ftPosSystemId = Guid.NewGuid(),
            cbTerminalID = "Terminal001",
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ftChargeItemId = Guid.NewGuid(),
                    Position = 1,
                    Description = "Test Item",
                    Quantity = 1.0m,
                    Amount = 10.00m,
                    VATRate = 21.0m,
                    ftChargeItemCase = (ChargeItemCase)0x4245000000000001 // Basic Belgian charge item case
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    ftPayItemId = Guid.NewGuid(),
                    Position = 1,
                    Description = "Cash Payment",
                    Quantity = 1.0m,
                    Amount = 10.00m,
                    ftPayItemCase = (PayItemCase)0x4245000000000001 // Basic Belgian pay item case
                }
            },
            ftReceiptCase = receiptCase
        };
    }

    protected virtual ReceiptResponse CreateBasicReceiptResponse(ReceiptRequest request)
    {
        return new ReceiptResponse
        {
            ftCashBoxID = request.ftCashBoxID,
            ftQueueID = request.ftQueueID ?? Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            cbTerminalID = request.cbTerminalID,
            cbReceiptReference = request.cbReceiptReference,
            ftCashBoxIdentification = "BE-TestCashBox-001",
            ftReceiptIdentification = $"ft{DateTime.UtcNow:yyyyMMddHHmmss}#{1:X}",
            ftReceiptMoment = request.cbReceiptMoment,
            ftSignatures = new List<SignatureItem>(),
            ftState = (State)0x4245000000000000, // Basic Belgian state
            ftStateData = null
        };
    }

    [Fact]
    public virtual void CreateInstance_ShouldSucceed()
    {
        // Act
        var scu = GetSystemUnderTest();

        // Assert
        scu.Should().NotBeNull();
        scu.Should().BeAssignableTo<IBESSCD>();
    }

    [Fact]
    public virtual async Task GetInfoAsync_ShouldReturnValidInfo()
    {
        // Arrange
        var scu = GetSystemUnderTest();

        // Act
        var info = await scu.GetInfoAsync();

        // Assert
        info.Should().NotBeNull();
    }


    [Theory]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    [InlineData(ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003)]
    [InlineData(ReceiptCase.ECommerce0x0004)]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    [InlineData(ReceiptCase.ZeroReceipt0x2000)]
    [InlineData(ReceiptCase.OneReceipt0x2001)]
    [InlineData(ReceiptCase.ShiftClosing0x2010)]
    [InlineData(ReceiptCase.DailyClosing0x2011)]
    [InlineData(ReceiptCase.MonthlyClosing0x2012)]
    [InlineData(ReceiptCase.YearlyClosing0x2013)]
    [InlineData(ReceiptCase.ProtocolUnspecified0x3000)]
    [InlineData(ReceiptCase.ProtocolTechnicalEvent0x3001)]
    [InlineData(ReceiptCase.ProtocolAccountingEvent0x3002)]
    [InlineData(ReceiptCase.InternalUsageMaterialConsumption0x3003)]
    [InlineData(ReceiptCase.Order0x3004)]
    [InlineData(ReceiptCase.Pay0x3005)]
    [InlineData(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010)]
    [InlineData(ReceiptCase.InitialOperationReceipt0x4001)]
    [InlineData(ReceiptCase.OutOfOperationReceipt0x4002)]
    [InlineData(ReceiptCase.InitSCUSwitch0x4011)]
    [InlineData(ReceiptCase.FinishSCUSwitch0x4012)]
    public async Task ProcessReceiptAsync_ShouldSucceed_WithReceiptCase(ReceiptCase receiptCase)
    {
        // Arrange
        var scu = GetSystemUnderTest();
        var request = CreateBasicReceiptRequest(receiptCase);
        var response = CreateBasicReceiptResponse(request);
        var processRequest = new ProcessRequest { ReceiptRequest = request, ReceiptResponse = response };

        // Act
        var result = await scu.ProcessReceiptAsync(processRequest);

        // Assert
        result.Should().NotBeNull();
        result.ReceiptResponse.Should().NotBeNull();
        result.ReceiptResponse.ftSignatures.Should().NotBeNull();
        // Check that the state doesn't indicate an error (using bit flags to check for error state)
        ((ulong) result.ReceiptResponse.ftState & 0xEEEE_EEEE).Should().Be(0, because: "Expected 0 but got 0x" + result.ReceiptResponse.ftState.ToString("x"));
    }


    [Fact]
    public virtual async Task ProcessReceiptAsync_InitialOperationReceipt_ShouldSucceed()
    {
        // Arrange
        var scu = GetSystemUnderTest();
        var request = CreateBasicReceiptRequest(ReceiptCase.InitialOperationReceipt0x4001);
        var response = CreateBasicReceiptResponse(request);
        var processRequest = new ProcessRequest { ReceiptRequest = request, ReceiptResponse = response };

        // Act
        var result = await scu.ProcessReceiptAsync(processRequest);

        // Assert
        result.Should().NotBeNull();
        result.ReceiptResponse.Should().NotBeNull();
        result.ReceiptResponse.ftSignatures.Should().NotBeNull();
        // Check that the state doesn't indicate an error (using bit flags to check for error state)
        ((ulong)result.ReceiptResponse.ftState & 0xEEEE_EEEE).Should().Be(0);
    }

    /// <summary>
    /// Test processing an Out of Operation Receipt (0x4002)
    /// </summary>
    [Fact]
    public virtual async Task ProcessReceiptAsync_OutOfOperationReceipt_ShouldSucceed()
    {
        // Arrange
        var scu = GetSystemUnderTest();
        var request = CreateBasicReceiptRequest(ReceiptCase.OutOfOperationReceipt0x4002);
        var response = CreateBasicReceiptResponse(request);
        var processRequest = new ProcessRequest { ReceiptRequest = request, ReceiptResponse = response };

        // Act
        var result = await scu.ProcessReceiptAsync(processRequest);

        // Assert
        result.Should().NotBeNull();
        result.ReceiptResponse.Should().NotBeNull();
        result.ReceiptResponse.ftSignatures.Should().NotBeNull();
        // Check that the state doesn't indicate an error
        ((ulong)result.ReceiptResponse.ftState & 0xEEEE_EEEE).Should().Be(0);
    }

    /// <summary>
    /// Test processing a Zero Receipt (0x2000)
    /// </summary>
    [Fact]
    public virtual async Task ProcessReceiptAsync_ZeroReceipt_ShouldSucceed()
    {
        // Arrange
        var scu = GetSystemUnderTest();
        var request = CreateBasicReceiptRequest(ReceiptCase.ZeroReceipt0x2000);
        // Zero receipt should have no charge or pay items
        request.cbChargeItems = new List<ChargeItem>();
        request.cbPayItems = new List<PayItem>();
        var response = CreateBasicReceiptResponse(request);
        var processRequest = new ProcessRequest { ReceiptRequest = request, ReceiptResponse = response };

        // Act
        var result = await scu.ProcessReceiptAsync(processRequest);

        // Assert
        result.Should().NotBeNull();
        result.ReceiptResponse.Should().NotBeNull();
        // Check that the state doesn't indicate an error
        ((ulong)result.ReceiptResponse.ftState & 0xEEEE_EEEE).Should().Be(0);
    }

    /// <summary>
    /// Test processing a Point of Sale Receipt (0x0001)
    /// </summary>
    [Fact]
    public virtual async Task ProcessReceiptAsync_PointOfSaleReceipt_ShouldSucceed()
    {
        // Arrange
        var scu = GetSystemUnderTest();
        var request = CreateBasicReceiptRequest(ReceiptCase.PointOfSaleReceipt0x0001);
        var response = CreateBasicReceiptResponse(request);
        var processRequest = new ProcessRequest { ReceiptRequest = request, ReceiptResponse = response };

        // Act
        var result = await scu.ProcessReceiptAsync(processRequest);

        // Assert
        result.Should().NotBeNull();
        result.ReceiptResponse.Should().NotBeNull();
        // Check that the state doesn't indicate an error
        ((ulong)result.ReceiptResponse.ftState & 0xEEEE_EEEE).Should().Be(0);
    }

    /// <summary>
    /// Test processing a Daily Closing Receipt (0x2011)
    /// </summary>
    [Fact]
    public virtual async Task ProcessReceiptAsync_DailyClosingReceipt_ShouldSucceed()
    {
        // Arrange
        var scu = GetSystemUnderTest();
        var request = CreateBasicReceiptRequest(ReceiptCase.DailyClosing0x2011);
        // Daily closing typically has no charge or pay items
        request.cbChargeItems = new List<ChargeItem>();
        request.cbPayItems = new List<PayItem>();
        var response = CreateBasicReceiptResponse(request);
        var processRequest = new ProcessRequest { ReceiptRequest = request, ReceiptResponse = response };

        // Act
        var result = await scu.ProcessReceiptAsync(processRequest);

        // Assert
        result.Should().NotBeNull();
        result.ReceiptResponse.Should().NotBeNull();
        // Check that the state doesn't indicate an error
        ((ulong)result.ReceiptResponse.ftState & 0xEEEE_EEEE).Should().Be(0);
    }

    /// <summary>
    /// Test processing multiple receipt types in sequence to validate basic workflow
    /// </summary>
    [Fact]
    public virtual async Task ProcessReceiptAsync_SequentialReceipts_ShouldSucceed()
    {
        // Arrange
        var scu = GetSystemUnderTest();

        // Process Initial Operation
        var initRequest = CreateBasicReceiptRequest(ReceiptCase.InitialOperationReceipt0x4001);
        initRequest.cbChargeItems = new List<ChargeItem>();
        initRequest.cbPayItems = new List<PayItem>();
        var initResponse = CreateBasicReceiptResponse(initRequest);
        var initProcessRequest = new ProcessRequest { ReceiptRequest = initRequest, ReceiptResponse = initResponse };

        // Process a regular receipt
        var posRequest = CreateBasicReceiptRequest(ReceiptCase.PointOfSaleReceipt0x0001);
        var posResponse = CreateBasicReceiptResponse(posRequest);
        var posProcessRequest = new ProcessRequest { ReceiptRequest = posRequest, ReceiptResponse = posResponse };

        // Process Daily Closing
        var closingRequest = CreateBasicReceiptRequest(ReceiptCase.DailyClosing0x2011);
        closingRequest.cbChargeItems = new List<ChargeItem>();
        closingRequest.cbPayItems = new List<PayItem>();
        var closingResponse = CreateBasicReceiptResponse(closingRequest);
        var closingProcessRequest = new ProcessRequest { ReceiptRequest = closingRequest, ReceiptResponse = closingResponse };

        // Act & Assert
        var initResult = await scu.ProcessReceiptAsync(initProcessRequest);
        initResult.Should().NotBeNull();
        ((ulong)initResult.ReceiptResponse.ftState & 0xEEEE_EEEE).Should().Be(0);

        var posResult = await scu.ProcessReceiptAsync(posProcessRequest);
        posResult.Should().NotBeNull();
        ((ulong)posResult.ReceiptResponse.ftState & 0xEEEE_EEEE).Should().Be(0);

        var closingResult = await scu.ProcessReceiptAsync(closingProcessRequest);
        closingResult.Should().NotBeNull();
        ((ulong)closingResult.ReceiptResponse.ftState & 0xEEEE_EEEE).Should().Be(0);
    }

    /// <summary>
    /// Test error handling for unsupported receipt cases
    /// </summary>
    [Fact]
    public virtual async Task ProcessReceiptAsync_UnsupportedReceiptCase_ShouldHandleGracefully()
    {
        // Arrange
        var scu = GetSystemUnderTest();
        var request = CreateBasicReceiptRequest((ReceiptCase)0x9999); // Unsupported receipt case
        var response = CreateBasicReceiptResponse(request);
        var processRequest = new ProcessRequest { ReceiptRequest = request, ReceiptResponse = response };

        // Act
        var result = await scu.ProcessReceiptAsync(processRequest);

        // Assert
        result.Should().NotBeNull();
        result.ReceiptResponse.Should().NotBeNull();
        // The SCU should handle unsupported cases gracefully, either by processing them 
        // as default cases or by setting appropriate error flags
    }

    /// <summary>
    /// Test that processing receipts with null/empty data doesn't crash the SCU
    /// </summary>
    [Fact]
    public virtual async Task ProcessReceiptAsync_WithMinimalData_ShouldNotCrash()
    {
        // Arrange
        var scu = GetSystemUnderTest();
        var request = new ReceiptRequest
        {
            ftCashBoxID = Guid.NewGuid(),
            ftQueueID = Guid.NewGuid(),
            ftPosSystemId = Guid.NewGuid(),
            cbTerminalID = "TEST",
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>(),
            cbPayItems = new List<PayItem>(),
            ftReceiptCase = ReceiptCase.ZeroReceipt0x2000
        };
        var response = CreateBasicReceiptResponse(request);
        var processRequest = new ProcessRequest { ReceiptRequest = request, ReceiptResponse = response };

        // Act
        var result = await scu.ProcessReceiptAsync(processRequest);

        // Assert
        result.Should().NotBeNull();
        result.ReceiptResponse.Should().NotBeNull();
    }
}