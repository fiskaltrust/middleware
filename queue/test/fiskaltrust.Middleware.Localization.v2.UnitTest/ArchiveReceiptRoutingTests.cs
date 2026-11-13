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

/// <summary>The archive receipt (0x3011) reaches the market's protocol processor; a market that does not implement it still answers "not supported".</summary>
public class ArchiveReceiptRoutingTests
{
    private const long ArchiveReceipt = 0x3011;

    [Fact]
    public async Task ArchiveReceipt_MarketWithoutIt_IsNotSupported()
    {
        var (response, _) = await Processor(new ProtocolWithoutArchive()).ProcessAsync(Request(), new ReceiptResponse(), new ftQueue { CountryCode = "AT" }, new ftQueueItem());

        response.ftState.IsState(State.Error).Should().BeTrue();
        response.ftSignatures.Should().Contain(s => s.Data.Contains("is not supported"));
    }

    [Fact]
    public async Task ArchiveReceipt_MarketWithIt_IsRoutedThere()
    {
        var protocol = new ProtocolWithArchive();

        var (response, _) = await Processor(protocol).ProcessAsync(Request(), new ReceiptResponse(), new ftQueue { CountryCode = "AT" }, new ftQueueItem());

        protocol.ArchiveCalls.Should().Be(1);
        response.ftState.IsState(State.Error).Should().BeFalse();
    }

    private static ReceiptRequest Request() => new()
    {
        ftReceiptCase = ((ReceiptCase) ArchiveReceipt).WithCountry("AT"),
        cbChargeItems = [],
        cbPayItems = [],
    };

    private static ReceiptProcessor Processor(IProtocolCommandProcessor protocol)
    {
        var validator = new Mock<IMarketValidator>();
        validator.Setup(x => x.ValidateAsync(It.IsAny<ReceiptRequest>(), It.IsAny<ftQueue>(), It.IsAny<ReceiptResponse>(), It.IsAny<object>()))
            .ReturnsAsync(new ValidationResult());
        validator.Setup(x => x.LastResult).Returns(new ValidationResult());

        return new ReceiptProcessor(
            NullLogger<ReceiptProcessor>.Instance,
            validator.Object,
            Mock.Of<ILifecycleCommandProcessor>(),
            Mock.Of<IReceiptCommandProcessor>(),
            Mock.Of<IDailyOperationsCommandProcessor>(),
            Mock.Of<IInvoiceCommandProcessor>(),
            protocol,
            null);
    }

    private class ProtocolWithoutArchive : IProtocolCommandProcessor
    {
        public Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request) => throw new InvalidOperationException();
        public Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => throw new InvalidOperationException();
        public Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => throw new InvalidOperationException();
        public Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request) => throw new InvalidOperationException();
        public Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request) => throw new InvalidOperationException();
        public Task<ProcessCommandResponse> Pay0x3005Async(ProcessCommandRequest request) => throw new InvalidOperationException();
        public Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request) => throw new InvalidOperationException();
    }

    private sealed class ProtocolWithArchive : ProtocolWithoutArchive, IProtocolCommandProcessor
    {
        public int ArchiveCalls { get; private set; }

        public Task<ProcessCommandResponse?> ArchiveReceipt0x3011Async(ProcessCommandRequest request)
        {
            ArchiveCalls++;
            return Task.FromResult<ProcessCommandResponse?>(new ProcessCommandResponse(request.ReceiptResponse, []));
        }
    }
}
