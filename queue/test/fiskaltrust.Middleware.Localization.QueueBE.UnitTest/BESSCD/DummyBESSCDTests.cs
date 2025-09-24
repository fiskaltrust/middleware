using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueBE.BESSCD;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueBE.UnitTest.BESSCD;

public class DummyBESSCDTests
{
    [Fact]
    public async Task ProcessReceiptAsync_ShouldReturnSameReceiptResponse()
    {
        // Arrange
        var sut = new DummyBESSCD();
        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = Guid.NewGuid(),
            ftReceiptCase = fiskaltrust.ifPOS.v2.Cases.ReceiptCase.PointOfSaleReceipt0x0001
        };
        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftReceiptIdentification = "test-receipt",
            ftReceiptMoment = DateTime.UtcNow
        };

        var request = new ProcessRequest
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = receiptResponse
        };

        // Act
        var result = await sut.ProcessReceiptAsync(request, new List<(ReceiptRequest, ReceiptResponse)>());

        // Assert
        result.ReceiptResponse.Should().Be(receiptResponse);
    }

    [Fact]
    public async Task GetInfoAsync_ShouldReturnBESSCDInfo()
    {
        // Arrange
        var sut = new DummyBESSCD();

        // Act
        var result = await sut.GetInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BESSCDInfo>();
    }
}