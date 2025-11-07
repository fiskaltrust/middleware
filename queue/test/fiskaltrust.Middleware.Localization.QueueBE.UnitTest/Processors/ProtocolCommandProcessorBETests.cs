using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueBE.Processors;
using fiskaltrust.Middleware.Localization.v2;
using FluentAssertions;
using Xunit;
using fiskaltrust.ifPOS.v2.Cases;
using Microsoft.Extensions.Logging;
using Moq;
using fiskaltrust.ifPOS.v2.be;

namespace fiskaltrust.Middleware.Localization.QueueBE.UnitTest.Processors;

public class ProtocolCommandProcessorBETests
{
    private readonly Mock<IBESSCD> _mockBESSCD = new();
    private readonly ReceiptProcessor _sut;

    public ProtocolCommandProcessorBETests()
    {
        _mockBESSCD.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>()))
                   .ReturnsAsync((ProcessRequest req, List<(ReceiptRequest, ReceiptResponse)> refs) => 
                       new ProcessResponse { ReceiptResponse = req.ReceiptResponse });

        _sut = new(Mock.Of<ILogger<ReceiptProcessor>>(), null!, null!, null!, null!, 
                   new ProtocolCommandProcessorBE(_mockBESSCD.Object));
    }

    [Theory]
    [InlineData(ReceiptCase.ProtocolUnspecified0x3000)]
    [InlineData(ReceiptCase.ProtocolTechnicalEvent0x3001)]
    [InlineData(ReceiptCase.ProtocolAccountingEvent0x3002)]
    [InlineData(ReceiptCase.InternalUsageMaterialConsumption0x3003)]
    [InlineData(ReceiptCase.Order0x3004)]
    [InlineData(ReceiptCase.Pay0x3005)]
    [InlineData(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010)]
    public async Task ProcessReceiptAsync_NoOp_Should_ReturnResponse(ReceiptCase receiptCase)
    {
        var queue = TestHelpers.CreateQueue();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = receiptCase
        };
        var receiptResponse = new ReceiptResponse
        {
            ftState = (State) 0x4245_2000_0000_0000, // BE state
            ftCashBoxIdentification = "cashBoxIdentification",
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptIdentification = "receiptIdentification",
            ftReceiptMoment = DateTime.UtcNow,
        };

        var result = await _sut.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4245_2000_0000_0000);
    }
}