using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest;

/// <summary>
/// market-gr#309: a caller-supplied VATAmount of 0 on a 24% charge item reached AADE unchecked.
/// The GR queue passes ValidationConfiguration.FromConfiguration(configuration) to its ReceiptProcessor,
/// so a cashbox configured with ValidationLevel=Error rejects such receipts before they reach the SCU.
/// </summary>
public class VatAmountValidationTests
{
    private static ReceiptRequest CreateRequest(decimal? vatAmount) => new()
    {
        cbReceiptReference = "testreceiptreference1",
        cbReceiptMoment = DateTime.UtcNow,
        Currency = Currency.EUR,
        ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0001,
        cbChargeItems =
        [
            new ChargeItem
            {
                Position = 1,
                Amount = 1.24m,
                VATRate = 24,
                VATAmount = vatAmount,
                Description = "product x",
                Currency = Currency.EUR,
                ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013
            }
        ],
        cbPayItems =
        [
            new PayItem
            {
                Position = 1,
                Amount = 1.24m,
                Description = "cash-payment",
                Currency = Currency.EUR,
                ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001
            }
        ]
    };

    private static ReceiptProcessor CreateProcessor(Dictionary<string, object> configuration)
    {
        var receiptCommandProcessor = new Mock<IReceiptCommandProcessor>();
        receiptCommandProcessor.Setup(x => x.PointOfSaleReceipt0x0001Async(It.IsAny<ProcessCommandRequest>()))
            .ReturnsAsync((ProcessCommandRequest request) => new ProcessCommandResponse(request.ReceiptResponse, []));

        return new ReceiptProcessor(
            LoggerFactory.Create(x => { }).CreateLogger<ReceiptProcessor>(),
            new ReceiptReferenceProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(Mock.Of<IMiddlewareQueueItemRepository>()))),
            Mock.Of<ILifecycleCommandProcessor>(),
            receiptCommandProcessor.Object,
            Mock.Of<IDailyOperationsCommandProcessor>(),
            Mock.Of<IInvoiceCommandProcessor>(),
            Mock.Of<IProtocolCommandProcessor>(),
            ValidationConfiguration.FromConfiguration(configuration));
    }

    private static ReceiptResponse CreateResponse() => new()
    {
        ftState = (State) 0x4752_2000_0000_0000,
        ftQueueID = Guid.NewGuid(),
        ftQueueItemID = Guid.NewGuid(),
        ftReceiptMoment = DateTime.UtcNow,
    };

    private static ftQueue CreateQueue() => new() { CountryCode = "GR" };

    [Fact]
    public async Task MarketValidator_ZeroVatAmountOnNormalVatRate_ReportsVatAmountMismatch()
    {
        var validator = new MarketValidator(new ReceiptReferenceProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(Mock.Of<IMiddlewareQueueItemRepository>()))));

        var result = await validator.ValidateAsync(CreateRequest(vatAmount: 0), CreateQueue());

        result.Errors.Should().Contain(x => x.ErrorCode == "VatAmountMismatch");
    }

    [Fact]
    public async Task ValidationLevelError_ZeroVatAmountOnNormalVatRate_ReceiptFails()
    {
        var sut = CreateProcessor(new Dictionary<string, object> { { "ValidationLevel", "Error" } });

        var (response, _) = await sut.ProcessAsync(CreateRequest(vatAmount: 0), CreateResponse(), CreateQueue(), new ftQueueItem());

        response.ftState.IsState(State.Error).Should().BeTrue();
        response.ftSignatures.Should().Contain(x => x.Caption!.Contains("VatAmountMismatch"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0.24)]
    public async Task ValidationLevelError_ConsistentVatAmount_ReceiptProceeds(double? vatAmount)
    {
        var sut = CreateProcessor(new Dictionary<string, object> { { "ValidationLevel", "Error" } });

        var (response, _) = await sut.ProcessAsync(CreateRequest((decimal?) vatAmount), CreateResponse(), CreateQueue(), new ftQueueItem());

        response.ftState.IsState(State.Error).Should().BeFalse();
    }

    [Fact]
    public async Task ValidationLevelNotConfigured_ZeroVatAmount_ReceiptProceedsToSCU()
    {
        // Without a configured ValidationLevel the queue only logs; the GR SCU (ValidationGR) is the hard stop.
        var sut = CreateProcessor([]);

        var (response, _) = await sut.ProcessAsync(CreateRequest(vatAmount: 0), CreateResponse(), CreateQueue(), new ftQueueItem());

        response.ftState.IsState(State.Error).Should().BeFalse();
    }
}
