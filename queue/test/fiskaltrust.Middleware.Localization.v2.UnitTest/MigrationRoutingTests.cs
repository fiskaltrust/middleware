using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest;

/// <summary>The migration receipts (0x4021 initiate, 0x4022 finish) reach the market's lifecycle processor; a market that does not implement them still answers "not supported".</summary>
public class MigrationRoutingTests
{
    [Theory]
    [InlineData(0x4021L)]
    [InlineData(0x4022L)]
    public async Task MigrationReceipt_MarketWithoutIt_IsNotSupported(long receiptCase)
    {
        var (response, _) = await Processor(new LifecycleWithoutMigration()).ProcessAsync(Request(receiptCase), new ReceiptResponse(), new ftQueue { CountryCode = "AT" }, new ftQueueItem());

        response.ftState.IsState(State.Error).Should().BeTrue();
        response.ftSignatures.Should().Contain(s => s.Data.Contains("is not supported"));
    }

    [Theory]
    [InlineData(0x4021L, 1, 0)]
    [InlineData(0x4022L, 0, 1)]
    public async Task MigrationReceipt_MarketWithIt_IsRoutedThere(long receiptCase, int initiate, int finish)
    {
        var lifecycle = new LifecycleWithMigration();

        var (response, _) = await Processor(lifecycle).ProcessAsync(Request(receiptCase), new ReceiptResponse(), new ftQueue { CountryCode = "AT" }, new ftQueueItem());

        lifecycle.InitiateCalls.Should().Be(initiate);
        lifecycle.FinishCalls.Should().Be(finish);
        response.ftState.IsState(State.Error).Should().BeFalse();
    }

    private static ReceiptRequest Request(long receiptCase) => new()
    {
        ftReceiptCase = ((ReceiptCase) receiptCase).WithCountry("AT"),
        cbChargeItems = [],
        cbPayItems = [],
    };

    private static ReceiptProcessor Processor(ILifecycleCommandProcessor lifecycle)
    {
        var validator = new Mock<IMarketValidator>();
        validator.Setup(x => x.ValidateAsync(It.IsAny<ReceiptRequest>(), It.IsAny<ftQueue>(), It.IsAny<ReceiptResponse>(), It.IsAny<object>()))
            .ReturnsAsync(new ValidationResult());
        validator.Setup(x => x.LastResult).Returns(new ValidationResult());

        return new ReceiptProcessor(
            NullLogger<ReceiptProcessor>.Instance,
            validator.Object,
            lifecycle,
            Mock.Of<IReceiptCommandProcessor>(),
            Mock.Of<IDailyOperationsCommandProcessor>(),
            Mock.Of<IInvoiceCommandProcessor>(),
            Mock.Of<IProtocolCommandProcessor>(),
            null);
    }

    private class LifecycleWithoutMigration : ILifecycleCommandProcessor
    {
        public Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request) => throw new InvalidOperationException();
        public Task<ProcessCommandResponse> OutOfOperationReceipt0x4002Async(ProcessCommandRequest request) => throw new InvalidOperationException();
        public Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request) => throw new InvalidOperationException();
        public Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request) => throw new InvalidOperationException();
    }

    private sealed class LifecycleWithMigration : LifecycleWithoutMigration, ILifecycleCommandProcessor
    {
        public int InitiateCalls { get; private set; }

        public int FinishCalls { get; private set; }

        public Task<ProcessCommandResponse?> InitiateMigration0x4021Async(ProcessCommandRequest request)
        {
            InitiateCalls++;
            return Task.FromResult<ProcessCommandResponse?>(new ProcessCommandResponse(request.ReceiptResponse, []));
        }

        public Task<ProcessCommandResponse?> FinishMigration0x4022Async(ProcessCommandRequest request)
        {
            FinishCalls++;
            return Task.FromResult<ProcessCommandResponse?>(new ProcessCommandResponse(request.ReceiptResponse, []));
        }
    }
}
