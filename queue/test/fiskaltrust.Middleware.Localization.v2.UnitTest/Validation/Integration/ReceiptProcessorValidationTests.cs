using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation.Integration;

/// <summary>
/// Integration tests that verify ReceiptProcessor calls validation before any processor runs,
/// returns all errors when validation fails, and routes to the correct processor when validation passes.
/// </summary>
public class ReceiptProcessorValidationTests
{
    private readonly Mock<ILogger<ReceiptProcessor>> _loggerMock = new();
    private readonly Mock<ILifecycleCommandProcessor> _lifecycleMock = new();
    private readonly Mock<IReceiptCommandProcessor> _receiptMock = new();
    private readonly Mock<IDailyOperationsCommandProcessor> _dailyOpsMock = new();
    private readonly Mock<IInvoiceCommandProcessor> _invoiceMock = new();
    private readonly Mock<IProtocolCommandProcessor> _protocolMock = new();

    private ReceiptProcessor CreateProcessor(v2.Validation.MarketValidator validator)
    {
        return new ReceiptProcessor(
            _loggerMock.Object,
            validator,
            _lifecycleMock.Object,
            _receiptMock.Object,
            _dailyOpsMock.Object,
            _invoiceMock.Object,
            _protocolMock.Object);
    }

    private static ReceiptResponse CreateReceiptResponse()
    {
        return new ReceiptResponse
        {
            ftState = default,
            ftSignatures = new List<SignatureItem>()
        };
    }

    private static ftQueue CreateQueue() => new() { ftQueueId = Guid.NewGuid() };
    private static ftQueueItem CreateQueueItem() => new() { ftQueueItemId = Guid.NewGuid() };

    #region Validation fails → processors never called

    [Fact]
    public async Task InvalidRequest_EmptyDescription_ProcessorsNeverCalled()
    {
        // Arrange — empty description violates global NotEmpty rule
        var validator = new QueueES.ValidationFV.ReceiptValidator();
        var processor = CreateProcessor(validator);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("ES"),
            cbReceiptAmount = 10.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "", VATRate = 21.0m, Amount = 10.00m, VATAmount = 1.74m }
            }
        };

        // Act
        var (response, _) = await processor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());

        // Assert — no processor was called
        _receiptMock.VerifyNoOtherCalls();
        _invoiceMock.VerifyNoOtherCalls();
        _protocolMock.VerifyNoOtherCalls();
        _dailyOpsMock.VerifyNoOtherCalls();
        _lifecycleMock.VerifyNoOtherCalls();

        // Assert — response has error signature
        Assert.NotEmpty(response.ftSignatures);
        Assert.Contains(response.ftSignatures, s => s.Caption == "FAILURE" && s.Data.Contains("NotEmptyValidator"));
    }

    [Fact]
    public async Task InvalidRequest_NegativeVATRate_ProcessorsNeverCalled()
    {
        // Arrange — negative VAT rate violates global GreaterThanOrEqualTo(0) rule
        var validator = new QueuePT.ValidationFV.ReceiptValidator();
        var processor = CreateProcessor(validator);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbReceiptAmount = 10.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Test Product", VATRate = -5m, Amount = 10.00m }
            }
        };

        // Act
        var (response, _) = await processor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());

        // Assert
        _receiptMock.VerifyNoOtherCalls();
        Assert.NotEmpty(response.ftSignatures);
        Assert.Contains(response.ftSignatures, s => s.Data.Contains("GreaterThanOrEqualValidator"));
    }

    #endregion

    #region Multiple errors → all errors in response (bug fix verification)

    [Fact]
    public async Task MultipleValidationErrors_AllErrorsReturnedInResponse()
    {
        // Arrange — 3 violations: empty description, negative VAT rate, zero amount
        var validator = new QueueES.ValidationFV.ReceiptValidator();
        var processor = CreateProcessor(validator);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("ES"),
            cbReceiptAmount = 0m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "", VATRate = -1m, Amount = 0m, VATAmount = 0m }
            }
        };

        // Act
        var (response, _) = await processor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());

        // Assert — all 3 errors must be present (before the fix, only the last one survived)
        var failureSignatures = response.ftSignatures.Where(s => s.Caption == "FAILURE").ToList();
        Assert.True(failureSignatures.Count >= 3, $"Expected at least 3 errors, got {failureSignatures.Count}");
        Assert.Contains(failureSignatures, s => s.Data.Contains("NotEmptyValidator"));
        Assert.Contains(failureSignatures, s => s.Data.Contains("GreaterThanOrEqualValidator"));
        Assert.Contains(failureSignatures, s => s.Data.Contains("NotEqualValidator"));
    }

    [Fact]
    public async Task MultipleChargeItems_EachWithErrors_AllErrorsReturned()
    {
        // Arrange — 2 charge items, each with empty description
        var validator = new QueuePT.ValidationFV.ReceiptValidator();
        var processor = CreateProcessor(validator);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbReceiptAmount = 20.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "", VATRate = 23.0m, Amount = 10.00m },
                new ChargeItem { Description = "", VATRate = 23.0m, Amount = 10.00m }
            }
        };

        // Act
        var (response, _) = await processor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());

        // Assert — both charge items should generate errors
        var failureSignatures = response.ftSignatures.Where(s => s.Caption == "FAILURE").ToList();
        Assert.True(failureSignatures.Count >= 2, $"Expected at least 2 errors, got {failureSignatures.Count}");
        Assert.Contains(failureSignatures, s => s.Data.Contains("cbChargeItems[0]"));
        Assert.Contains(failureSignatures, s => s.Data.Contains("cbChargeItems[1]"));
    }

    #endregion

    #region ES market-specific validation

    [Fact]
    public async Task ESMarket_MissingVATAmount_ValidationFails()
    {
        // Arrange — ES requires VATAmount, null should fail
        var validator = new QueueES.ValidationFV.ReceiptValidator();
        var processor = CreateProcessor(validator);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("ES"),
            cbReceiptAmount = 10.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", VATRate = 21.0m, Amount = 10.00m, VATAmount = null }
            }
        };

        // Act
        var (response, _) = await processor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());

        // Assert
        _receiptMock.VerifyNoOtherCalls();
        Assert.Contains(response.ftSignatures, s => s.Data.Contains("NotNullValidator"));
    }

    [Fact]
    public async Task ESMarket_WithVATAmount_ValidationPasses()
    {
        // Arrange — valid ES request (VATAmount provided)
        var validator = new QueueES.ValidationFV.ReceiptValidator();
        var processor = CreateProcessor(validator);

        _receiptMock
            .Setup(x => x.PointOfSaleReceipt0x0001Async(It.IsAny<ProcessCommandRequest>()))
            .ReturnsAsync((ProcessCommandRequest req) =>
                new ProcessCommandResponse(req.ReceiptResponse, new List<ftActionJournal>()));

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("ES"),
            cbReceiptAmount = 10.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", VATRate = 21.0m, Amount = 10.00m, VATAmount = 1.74m }
            }
        };

        // Act
        var (response, _) = await processor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());

        // Assert — processor was called (validation passed)
        _receiptMock.Verify(x => x.PointOfSaleReceipt0x0001Async(It.IsAny<ProcessCommandRequest>()), Times.Once);

        // Assert — no FAILURE signatures
        Assert.DoesNotContain(response.ftSignatures, s => s.Caption == "FAILURE");
    }

    #endregion

    #region PT market-specific validation

    [Fact]
    public async Task PTMarket_ShortDescription_ValidationFails()
    {
        // Arrange — PT requires description min 3 chars, "AB" should fail
        var validator = new QueuePT.ValidationFV.ReceiptValidator();
        var processor = CreateProcessor(validator);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbReceiptAmount = 10.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "AB", VATRate = 23.0m, Amount = 10.00m }
            }
        };

        // Act
        var (response, _) = await processor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());

        // Assert
        _receiptMock.VerifyNoOtherCalls();
        Assert.Contains(response.ftSignatures, s => s.Data.Contains("MinimumLengthValidator"));
    }

    [Fact]
    public async Task PTMarket_ValidDescription_ValidationPasses()
    {
        // Arrange — valid PT request (description 3+ chars)
        var validator = new QueuePT.ValidationFV.ReceiptValidator();
        var processor = CreateProcessor(validator);

        _receiptMock
            .Setup(x => x.PointOfSaleReceipt0x0001Async(It.IsAny<ProcessCommandRequest>()))
            .ReturnsAsync((ProcessCommandRequest req) =>
                new ProcessCommandResponse(req.ReceiptResponse, new List<ftActionJournal>()));

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbReceiptAmount = 10.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product Name", VATRate = 23.0m, Amount = 10.00m }
            }
        };

        // Act
        var (response, _) = await processor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());

        // Assert — processor was called
        _receiptMock.Verify(x => x.PointOfSaleReceipt0x0001Async(It.IsAny<ProcessCommandRequest>()), Times.Once);
        Assert.DoesNotContain(response.ftSignatures, s => s.Caption == "FAILURE");
    }

    #endregion

    #region Cross-market: same request, different rules

    [Fact]
    public async Task SameRequest_NullVATAmount_FailsES_PassesPT()
    {
        // Arrange — null VATAmount: ES requires it (fails), PT does not (passes)
        var request = new ReceiptRequest
        {
            cbReceiptAmount = 10.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", VATRate = 21.0m, Amount = 10.00m, VATAmount = null }
            }
        };

        // ES — should fail
        var esValidator = new QueueES.ValidationFV.ReceiptValidator();
        var esProcessor = CreateProcessor(esValidator);
        request.ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("ES");

        var (esResponse, _) = await esProcessor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());
        Assert.Contains(esResponse.ftSignatures, s => s.Caption == "FAILURE");

        // PT — should pass (need fresh mocks since ES test may have verified them)
        var ptReceiptMock = new Mock<IReceiptCommandProcessor>();
        ptReceiptMock
            .Setup(x => x.PointOfSaleReceipt0x0001Async(It.IsAny<ProcessCommandRequest>()))
            .ReturnsAsync((ProcessCommandRequest req) =>
                new ProcessCommandResponse(req.ReceiptResponse, new List<ftActionJournal>()));

        var ptProcessor = new ReceiptProcessor(
            new Mock<ILogger<ReceiptProcessor>>().Object,
            new QueuePT.ValidationFV.ReceiptValidator(),
            new Mock<ILifecycleCommandProcessor>().Object,
            ptReceiptMock.Object,
            new Mock<IDailyOperationsCommandProcessor>().Object,
            new Mock<IInvoiceCommandProcessor>().Object,
            new Mock<IProtocolCommandProcessor>().Object);

        request.ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");

        var (ptResponse, _) = await ptProcessor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());
        Assert.DoesNotContain(ptResponse.ftSignatures, s => s.Caption == "FAILURE");
        ptReceiptMock.Verify(x => x.PointOfSaleReceipt0x0001Async(It.IsAny<ProcessCommandRequest>()), Times.Once);
    }

    #endregion
}
