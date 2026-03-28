using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation.Integration;

public class ReceiptProcessorValidationTests
{
    private readonly Mock<ILogger<ReceiptProcessor>> _loggerMock = new();
    private readonly Mock<ILifecycleCommandProcessor> _lifecycleMock = new();
    private readonly Mock<IReceiptCommandProcessor> _receiptMock = new();
    private readonly Mock<IDailyOperationsCommandProcessor> _dailyOpsMock = new();
    private readonly Mock<IInvoiceCommandProcessor> _invoiceMock = new();
    private readonly Mock<IProtocolCommandProcessor> _protocolMock = new();

    private static ReceiptReferenceProvider CreateMockProvider()
    {
        var mockRepo = new Mock<IMiddlewareQueueItemRepository>();
        mockRepo.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Returns(AsyncEnumerable.Empty<ftQueueItem>());
        mockRepo.Setup(x => x.GetByReceiptReferenceAsync(It.IsAny<string>(), null))
            .Returns(AsyncEnumerable.Empty<ftQueueItem>());
        mockRepo.Setup(x => x.GetLastQueueItemAsync())
            .ReturnsAsync((ftQueueItem?)null);
        return new ReceiptReferenceProvider(
            new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(mockRepo.Object)));
    }

    private ReceiptProcessor CreateProcessor(MarketValidator validator)
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

    #region Validation fails -> processors never called

    [Fact]
    public async Task InvalidRequest_EmptyDescription_ProcessorsNeverCalled()
    {
        var validator = new QueueES.ValidationFV.ReceiptValidator(CreateMockProvider());
        var processor = CreateProcessor(validator);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("ES"),
            cbReceiptAmount = 10.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "", VATRate = 21.0m, Amount = 10.00m, VATAmount = 1.74m }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 10.00m }
            }
        };

        var (response, _) = await processor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());

        _receiptMock.VerifyNoOtherCalls();
        _invoiceMock.VerifyNoOtherCalls();
        _protocolMock.VerifyNoOtherCalls();
        _dailyOpsMock.VerifyNoOtherCalls();
        _lifecycleMock.VerifyNoOtherCalls();

        Assert.NotEmpty(response.ftSignatures);
        Assert.Contains(response.ftSignatures, s => s.Caption == "FAILURE" && s.Data.Contains("NotEmptyValidator"));
    }

    [Fact]
    public async Task InvalidRequest_NegativeVATRate_ProcessorsNeverCalled()
    {
        var validator = new QueuePT.ValidationFV.ReceiptValidator(CreateMockProvider());
        var processor = CreateProcessor(validator);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbReceiptAmount = 10.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Test Product", VATRate = -5m, Amount = 10.00m }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 10.00m }
            }
        };

        var (response, _) = await processor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());

        _receiptMock.VerifyNoOtherCalls();
        Assert.NotEmpty(response.ftSignatures);
        Assert.Contains(response.ftSignatures, s => s.Data.Contains("GreaterThanOrEqualValidator"));
    }

    #endregion

    #region Multiple errors -> all errors in response

    [Fact]
    public async Task MultipleValidationErrors_AllErrorsReturnedInResponse()
    {
        var validator = new QueueES.ValidationFV.ReceiptValidator(CreateMockProvider());
        var processor = CreateProcessor(validator);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("ES"),
            cbReceiptAmount = 0m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "", VATRate = -1m, Amount = 0m, VATAmount = 0m }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 0m }
            }
        };

        var (response, _) = await processor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());

        var failureSignatures = response.ftSignatures.Where(s => s.Caption == "FAILURE").ToList();
        Assert.True(failureSignatures.Count >= 3, $"Expected at least 3 errors, got {failureSignatures.Count}");
        Assert.Contains(failureSignatures, s => s.Data.Contains("NotEmptyValidator"));
        Assert.Contains(failureSignatures, s => s.Data.Contains("GreaterThanOrEqualValidator"));
        Assert.Contains(failureSignatures, s => s.Data.Contains("NotEqualValidator"));
    }

    [Fact]
    public async Task MultipleChargeItems_EachWithErrors_AllErrorsReturned()
    {
        var validator = new QueuePT.ValidationFV.ReceiptValidator(CreateMockProvider());
        var processor = CreateProcessor(validator);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbReceiptAmount = 20.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "", VATRate = 23.0m, Amount = 10.00m },
                new ChargeItem { Description = "", VATRate = 23.0m, Amount = 10.00m }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 20.00m }
            }
        };

        var (response, _) = await processor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());

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
        var validator = new QueueES.ValidationFV.ReceiptValidator(CreateMockProvider());
        var processor = CreateProcessor(validator);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("ES"),
            cbReceiptAmount = 10.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", VATRate = 21.0m, Amount = 10.00m, VATAmount = null }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 10.00m }
            }
        };

        var (response, _) = await processor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());

        _receiptMock.VerifyNoOtherCalls();
        Assert.Contains(response.ftSignatures, s => s.Data.Contains("NotNullValidator"));
    }

    [Fact]
    public async Task ESMarket_WithVATAmount_ValidationPasses()
    {
        var validator = new QueueES.ValidationFV.ReceiptValidator(CreateMockProvider());
        var processor = CreateProcessor(validator);

        _receiptMock
            .Setup(x => x.PointOfSaleReceipt0x0001Async(It.IsAny<ProcessCommandRequest>()))
            .ReturnsAsync((ProcessCommandRequest req) =>
                new ProcessCommandResponse(req.ReceiptResponse, new List<ftActionJournal>()));

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("ES"),
            cbReceiptAmount = 10.00m,
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", VATRate = 21.0m, Amount = 10.00m, VATAmount = 1.74m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 10.00m }
            }
        };

        var (response, _) = await processor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());

        _receiptMock.Verify(x => x.PointOfSaleReceipt0x0001Async(It.IsAny<ProcessCommandRequest>()), Times.Once);
        Assert.DoesNotContain(response.ftSignatures, s => s.Caption == "FAILURE");
    }

    #endregion

    #region PT market-specific validation

    [Fact]
    public async Task PTMarket_ShortDescription_ValidationFails()
    {
        var validator = new QueuePT.ValidationFV.ReceiptValidator(CreateMockProvider());
        var processor = CreateProcessor(validator);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbReceiptAmount = 10.00m,
            cbReceiptMoment = DateTime.UtcNow,
            cbUser = "Operator1",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "AB", VATRate = 23.0m, Amount = 10.00m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 10.00m }
            }
        };

        var (response, _) = await processor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());

        _receiptMock.VerifyNoOtherCalls();
        Assert.Contains(response.ftSignatures, s => s.Data.Contains("MinimumLengthValidator"));
    }

    [Fact]
    public async Task PTMarket_ValidDescription_ValidationPasses()
    {
        var validator = new QueuePT.ValidationFV.ReceiptValidator(CreateMockProvider());
        var processor = CreateProcessor(validator);

        _receiptMock
            .Setup(x => x.PointOfSaleReceipt0x0001Async(It.IsAny<ProcessCommandRequest>()))
            .ReturnsAsync((ProcessCommandRequest req) =>
                new ProcessCommandResponse(req.ReceiptResponse, new List<ftActionJournal>()));

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbReceiptAmount = 10.00m,
            cbReceiptMoment = DateTime.UtcNow,
            cbUser = "Operator1",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product Name", VATRate = 23.0m, Amount = 10.00m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 10.00m }
            }
        };

        var (response, _) = await processor.ProcessAsync(request, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());

        _receiptMock.Verify(x => x.PointOfSaleReceipt0x0001Async(It.IsAny<ProcessCommandRequest>()), Times.Once);
        Assert.DoesNotContain(response.ftSignatures, s => s.Caption == "FAILURE");
    }

    #endregion

    #region Cross-market: same request, different rules

    [Fact]
    public async Task SameRequest_NullVATAmount_FailsES_PassesPT()
    {
        // ES - should fail (VATAmount is null, ES requires it)
        var esRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("ES"),
            cbReceiptAmount = 10.00m,
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", VATRate = 21.0m, Amount = 10.00m, VATAmount = null, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 10.00m }
            }
        };

        var esValidator = new QueueES.ValidationFV.ReceiptValidator(CreateMockProvider());
        var esProcessor = CreateProcessor(esValidator);

        var (esResponse, _) = await esProcessor.ProcessAsync(esRequest, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());
        Assert.Contains(esResponse.ftSignatures, s => s.Caption == "FAILURE");

        // PT - should pass (VATAmount not required in PT)
        var ptReceiptMock = new Mock<IReceiptCommandProcessor>();
        ptReceiptMock
            .Setup(x => x.PointOfSaleReceipt0x0001Async(It.IsAny<ProcessCommandRequest>()))
            .ReturnsAsync((ProcessCommandRequest req) =>
                new ProcessCommandResponse(req.ReceiptResponse, new List<ftActionJournal>()));

        var ptProcessor = new ReceiptProcessor(
            new Mock<ILogger<ReceiptProcessor>>().Object,
            new QueuePT.ValidationFV.ReceiptValidator(CreateMockProvider()),
            new Mock<ILifecycleCommandProcessor>().Object,
            ptReceiptMock.Object,
            new Mock<IDailyOperationsCommandProcessor>().Object,
            new Mock<IInvoiceCommandProcessor>().Object,
            new Mock<IProtocolCommandProcessor>().Object);

        var ptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbReceiptAmount = 10.00m,
            cbReceiptMoment = DateTime.UtcNow,
            cbUser = "Operator1",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", VATRate = 23.0m, Amount = 10.00m, VATAmount = null, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 10.00m }
            }
        };

        var (ptResponse, _) = await ptProcessor.ProcessAsync(ptRequest, CreateReceiptResponse(), CreateQueue(), CreateQueueItem());
        Assert.DoesNotContain(ptResponse.ftSignatures, s => s.Caption == "FAILURE");
        ptReceiptMock.Verify(x => x.PointOfSaleReceipt0x0001Async(It.IsAny<ProcessCommandRequest>()), Times.Once);
    }

    #endregion
}
