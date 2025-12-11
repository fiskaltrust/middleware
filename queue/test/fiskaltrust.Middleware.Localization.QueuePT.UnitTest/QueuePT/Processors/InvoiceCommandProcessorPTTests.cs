using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using FluentAssertions;
using Xunit;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pt;
using Moq;
using fiskaltrust.Middleware.Contracts.Repositories;
using Microsoft.Extensions.Logging;
using fiskaltrust.storage.V0;
using System.Text.Json;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT.Processors;

public class InvoiceCommandProcessorPTTests
{
    private readonly ReceiptProcessor _sut = new(Mock.Of<ILogger<ReceiptProcessor>>(), null!, null!, null!, new InvoiceCommandProcessorPT(Mock.Of<IPTSSCD>(), new ftQueuePT(), new(() => Task.FromResult(Mock.Of<IMiddlewareQueueItemRepository>()))), null!);

    [Theory]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task ProcessReceiptAsync_NoOp_Should_ReturnResponse(ReceiptCase receiptCase)
    {
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
        var result = await _sut.ProcessAsync(receiptRequest, receiptResponse, new ftQueue { }, new ftQueueItem { });

        result.receiptResponse.ftSignatures.Should().BeEmpty();
        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x5054_2000_0000_0000);
    }

    //[Theory]
    //[InlineData(ReceiptCase.InvoiceB2C0x1001, Skip = "broken")]
    //public async Task ProcessReceiptAsync_ShouldReturnEmptyList(ReceiptCase receiptCase)
    //{
    //    var queue = TestHelpers.CreateQueue();
    //    var queueItem = TestHelpers.CreateQueueItem();
    //    var receiptRequest = new ReceiptRequest
    //    {
    //        ftReceiptCase = receiptCase
    //    };
    //    var receiptResponse = new ReceiptResponse
    //    {
    //        ftState = (State) 0x5054_2000_0000_0000,
    //        ftCashBoxIdentification = "cashBoxIdentification",
    //        ftQueueID = Guid.NewGuid(),
    //        ftQueueItemID = Guid.NewGuid(),
    //        ftQueueRow = 1,
    //        ftReceiptIdentification = "receiptIdentification",
    //        ftReceiptMoment = DateTime.UtcNow,
    //    };
    //    var queuePT = new ftQueuePT
    //    {
    //        IssuerTIN = "123456789",
    //    };
    //    var signaturCreationUnitPT = new ftSignaturCreationUnitPT
    //    {
    //        PrivateKey = File.ReadAllText("PrivateKey.pem"),
    //        SoftwareCertificateNumber = "9999",
    //    };

    //    var configMock = new Mock<IConfigurationRepository>();
    //    configMock.Setup(x => x.InsertOrUpdateQueueAsync(It.IsAny<ftQueue>())).Returns(Task.CompletedTask);

    //    var queueItemRepository = new Mock<IMiddlewareQueueItemRepository>();

    //    var sut = new InvoiceCommandProcessorPT(new InMemorySCU(signaturCreationUnitPT), queuePT, new(() => Task.FromResult(queueItemRepository.Object)));
    //    var result = await sut.InvoiceB2C0x1001Async(new ProcessCommandRequest(queue, receiptRequest, receiptResponse));

    //    result.receiptResponse.Should().Be(receiptResponse);
    //    result.receiptResponse.ftState.Should().Be(0x5054_2000_0000_0000, because: JsonSerializer.Serialize(result.receiptResponse.ftSignatures, new JsonSerializerOptions
    //    {
    //        WriteIndented = true
    //    }));
    //}

    [Fact]
    public async Task ProcessReceiptAsync_ShouldReturnError()
    {
        var queue = TestHelpers.CreateQueue();
        var queuePT = new ftQueuePT();
        var scuPT = new ftSignaturCreationUnitPT();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase) 0
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

        var result = await _sut.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);
        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x5054_2000_EEEE_EEEE);
    }
}
