using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueBE.Models;
using fiskaltrust.Middleware.Localization.QueueBE.Processors;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using Xunit;
using fiskaltrust.ifPOS.v2.Cases;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace fiskaltrust.Middleware.Localization.QueueBE.UnitTest.Processors;

public class LifecycleCommandProcessorBETests
{
    private readonly ReceiptProcessor _sut = new(Mock.Of<ILogger<ReceiptProcessor>>(), new LifecycleCommandProcessorBE(Mock.Of<ILocalizedQueueStorageProvider>()), null!, null!, null!, null!);

    [Theory]
    [InlineData(ReceiptCase.InitSCUSwitch0x4011)]
    [InlineData(ReceiptCase.FinishSCUSwitch0x4012)]
    public async Task ProcessReceiptAsync_NoOp_Should_ReturnResponse(ReceiptCase receiptCase)
    {
        var queue = TestHelpers.CreateQueue();
        var queueItem = TestHelpers.CreateQueueItem();

        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = Guid.NewGuid(),
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
        result.actionJournals.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessReceiptAsync_ShouldReturnError()
    {
        var queue = TestHelpers.CreateQueue();
        var queueItem = TestHelpers.CreateQueueItem();

        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase) 0
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
        result.receiptResponse.ftState.Should().Be(0x4245_2000_EEEE_EEEE); // BE error state
    }

    [Fact]
    public async Task InitialOperationReceipt0x4001Async_ShouldReturnActionJournal_AndSignature()
    {
        var queue = TestHelpers.CreateQueue();
        var queueItem = TestHelpers.CreateQueueItem();

        var configMock = new Mock<ILocalizedQueueStorageProvider>();
        configMock.Setup(x => x.ActivateQueueAsync()).Returns(() =>
        {
            queue.StartMoment = DateTime.UtcNow;
            return Task.CompletedTask;
        });
        var sut = new LifecycleCommandProcessorBE(configMock.Object);

        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) (0x4245_2000_0000_0000 | (long) ReceiptCase.InitialOperationReceipt0x4001) // BE case
        };
        var receiptResponse = new ReceiptResponse
        {
            ftCashBoxID = receiptRequest.ftCashBoxID,
            ftState = (State) 0x4245_2000_0000_0000, // BE state
            ftCashBoxIdentification = "cashBoxIdentification",
            ftQueueID = queue.ftQueueId,
            ftQueueItemID = queueItem.ftQueueItemId,
            ftQueueRow = 1,
            ftReceiptIdentification = "receiptIdentification",
            ftReceiptMoment = DateTime.UtcNow,
        };

        var request = new ProcessCommandRequest(queue, receiptRequest, receiptResponse);

        var result = await sut.InitialOperationReceipt0x4001Async(request);

        queue.StartMoment.Should().BeCloseTo(DateTime.UtcNow, 1000);
        result.receiptResponse.Should().Be(receiptResponse);
        result.actionJournals.Should().NotBeEmpty();
        result.receiptResponse.ftSignatures.Should().NotBeEmpty();

        result.receiptResponse.ftState.Should().Be(0x4245_2000_0000_0000);

        var expectedSignaturItem = new SignatureItem
        {
            Caption = "Initial-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = (SignatureType) 0x4245_2000_0001_1001 // BE signature type
        };

        result.receiptResponse.ftSignatures[0].Should().BeEquivalentTo(expectedSignaturItem);

        result.actionJournals[0].Type.Should().Be("4245200000004001-ActivateQueueBE"); // BE type

        var data = JsonSerializer.Deserialize<ActivateQueueBE>(result.actionJournals[0].DataJson)!;
        data.CashBoxId.Should().Be(receiptRequest.ftCashBoxID.GetValueOrDefault());
        data.IsStartReceipt.Should().Be(true);
        data.Version.Should().Be("V0");

        configMock.Verify(x => x.ActivateQueueAsync(), Times.Exactly(1));
    }

    [Fact]
    public async Task OutOfOperationReceipt0x4002Async_ShouldReturnActionJournal_AndSignature()
    {
        var queue = TestHelpers.CreateQueue();
        queue.StartMoment = DateTime.UtcNow;
        var queueItem = TestHelpers.CreateQueueItem();

        var configMock = new Mock<ILocalizedQueueStorageProvider>();
        configMock.Setup(x => x.DeactivateQueueAsync()).Returns(() =>
        {
            queue.StopMoment = DateTime.UtcNow;
            return Task.CompletedTask;
        });
        var sut = new LifecycleCommandProcessorBE(configMock.Object);

        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) (0x4245_2000_0000_0000 | (long) ReceiptCase.OutOfOperationReceipt0x4002) // BE case
        };
        var receiptResponse = new ReceiptResponse
        {
            ftState = (State) 0x4245_2000_0000_0000, // BE state
            ftCashBoxIdentification = "cashBoxIdentification",
            ftQueueID = queue.ftQueueId,
            ftQueueItemID = queueItem.ftQueueItemId,
            ftQueueRow = 1,
            ftReceiptIdentification = "receiptIdentification",
            ftReceiptMoment = DateTime.UtcNow,
        };

        var request = new ProcessCommandRequest(queue, receiptRequest, receiptResponse);

        var result = await sut.OutOfOperationReceipt0x4002Async(request);

        queue.StopMoment.Should().BeCloseTo(DateTime.UtcNow, 1000);
        result.receiptResponse.Should().Be(receiptResponse);
        result.actionJournals.Should().NotBeEmpty();
        result.receiptResponse.ftSignatures.Should().NotBeEmpty();

        result.receiptResponse.ftState.Should().Be(0x4245_2000_0000_0001); // BE disabled state

        var expectedSignaturItem = new SignatureItem
        {
            ftSignatureType = (SignatureType) 0x4245_2000_0001_1002, // BE signature type
            ftSignatureFormat = SignatureFormat.Text,
            Caption = "Out-of-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}"
        };

        result.receiptResponse.ftSignatures[0].Should().BeEquivalentTo(expectedSignaturItem);

        result.actionJournals[0].Type.Should().Be("4245200000004002-DeactivateQueueBE"); // BE type

        var data = JsonSerializer.Deserialize<DeactivateQueueBE>(result.actionJournals[0].DataJson)!;
        data.CashBoxId.Should().Be(receiptRequest.ftCashBoxID.GetValueOrDefault());
        data.IsStopReceipt.Should().Be(true);
        data.Version.Should().Be("V0");

        configMock.Verify(x => x.DeactivateQueueAsync(), Times.Exactly(1));
    }
}