using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using FluentAssertions;
using Xunit;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT.Processors;

public class DailyOperationsCommandProcessorPTTests
{
    private readonly DailyOperationsCommandProcessorPT _sut = new DailyOperationsCommandProcessorPT();

    [Theory]
    [InlineData(ReceiptCase.ZeroReceipt0x2000)]
    [InlineData(ReceiptCase.OneReceipt0x2001)]
    [InlineData(ReceiptCase.ShiftClosing0x2010)]
    [InlineData(ReceiptCase.DailyClosing0x2011)]
    [InlineData(ReceiptCase.MonthlyClosing0x2012)]
    [InlineData(ReceiptCase.YearlyClosing0x2013)]
    public async Task ProcessReceiptAsync_ShouldReturnEmptyList(ReceiptCase receiptCase)
    {
        var queue = TestHelpers.CreateQueue();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = receiptCase
        };
        var receiptResponse = new ReceiptResponse
        {
            ftState = (State) 0x5054_2000_0000_0000,
            ftCashBoxIdentification = "cashBoxIdentification",
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptIdentification = "receiptIdentification",
            ftReceiptMoment = DateTime.UtcNow,
        };
        var request = new ProcessCommandRequest(queue, receiptRequest, receiptResponse);

        var result = await _sut.ProcessReceiptAsync(request);
        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x5054_2000_0000_0000);
    }

    [Fact]
    public async Task ProcessReceiptAsync_ShouldReturnError_IfInvalidCaseIsUsed()
    {
        var queue = TestHelpers.CreateQueue();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase) (-1)
        };
        var receiptResponse = new ReceiptResponse
        {
            ftState = (State) 0x5054_2000_0000_0000,
            ftCashBoxIdentification = "cashBoxIdentification",
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptIdentification = "receiptIdentification",
            ftReceiptMoment = DateTime.UtcNow,
        };
        var request = new ProcessCommandRequest(queue, receiptRequest, receiptResponse);

        var result = await _sut.ProcessReceiptAsync(request);
        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x5054_2000_EEEE_EEEE);
    }
}
