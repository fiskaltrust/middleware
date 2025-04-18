using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueES.Models;
using fiskaltrust.Middleware.Localization.QueueES.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using Newtonsoft.Json;
using Xunit;
using AutoFixture;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueES.ESSSCD;
using fiskaltrust.Middleware.Storage.ES;
using fiskaltrust.storage.V0.MasterData;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueES.UnitTest.QueueES.Processors
{
    public class LifecycleCommandProcessorESTests
    {
        private readonly Fixture _fixture = new Fixture();
        private readonly LifecycleCommandProcessorES _sut = new(Mock.Of<ILocalizedQueueStorageProvider>());

        [Theory]
        [InlineData(ReceiptCase.InitialOperationReceipt0x4001)]
        [InlineData(ReceiptCase.OutOfOperationReceipt0x4002)]
        [InlineData(ReceiptCase.InitSCUSwitch0x4011)]
        [InlineData(ReceiptCase.FinishSCUSwitch0x4012)]
        public async Task ProcessReceiptAsync_ShouldReturnEmptyList(ReceiptCase receiptCase)
        {
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();

            var signaturCreationUnitES = new Storage.ES.ftSignaturCreationUnitES
            {

            };

            var masterDataConfiguration = _fixture.Create<MasterDataConfiguration>();

            var configMock = new Mock<ILocalizedQueueStorageProvider>();
            configMock.Setup(x => x.ActivateQueueAsync()).Returns(Task.CompletedTask);
            var sut = new LifecycleCommandProcessorES(configMock.Object);

            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftReceiptCase = receiptCase
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State) 0x4752_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "0#0",
                ftReceiptMoment = DateTime.UtcNow,
            };
            var request = new ProcessCommandRequest(queue, receiptRequest, receiptResponse);

            var result = await sut.ProcessReceiptAsync(request);

            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().NotBe(0x4752_2000_EEEE_EEEE);
        }

        [Fact]
        public async Task ProcessReceiptAsync_ShouldReturnError()
        {
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();

            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = (ReceiptCase) (-1)
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State) 0x4752_2000_0000_0000,
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
            result.receiptResponse.ftState.Should().Be(0x4752_2000_EEEE_EEEE);
        }

        [Fact(Skip = "not fully implemented")]
        public async Task InitialOperationReceipt0x4001Async_ShouldReturnActionJournal_InitOperationSignature_AndSetStateInQueue()
        {
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();

            var signaturCreationUnitES = new Storage.ES.ftSignaturCreationUnitES
            {

            };

            var masterDataConfiguration = _fixture.Create<MasterDataConfiguration>();

            var configMock = new Mock<ILocalizedQueueStorageProvider>();
            configMock.Setup(x => x.ActivateQueueAsync()).Returns(Task.CompletedTask);
            var sut = new LifecycleCommandProcessorES(configMock.Object);

            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftReceiptCase = (ReceiptCase) (0x4752_2000_0000_0000 | (long) ReceiptCase.InitialOperationReceipt0x4001)
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State) 0x4752_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "0#0",
                ftReceiptMoment = DateTime.UtcNow,
            };

            var request = new ProcessCommandRequest(queue, receiptRequest, receiptResponse);

            var result = await sut.InitialOperationReceipt0x4001Async(request);

            using var scope = new AssertionScope();

            queue.StartMoment.Should().BeCloseTo(DateTime.UtcNow, 1000);

            result.receiptResponse.Should().Be(receiptResponse);
            result.actionJournals.Should().NotBeEmpty();
            result.receiptResponse.ftSignatures.Should().NotBeEmpty();

            result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000, because: $"ftState {result.receiptResponse.ftState.ToString("X")} is different than expected.");

            var expectedSignaturItem = new SignatureItem
            {
                Caption = "Initial-operation receipt",
                Data = $"Queue-ID: {queue.ftQueueId}",
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = (SignatureType) 0x4752_2000_0001_1001
            };

            result.receiptResponse.ftSignatures[0].Should().BeEquivalentTo(expectedSignaturItem);

            var expectedActionJournal = new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                Moment = DateTime.UtcNow,
                Priority = -1,
                Type = "4752200000004001-ActivateQueueES",
                Message = $"Initial-Operation receipt. Queue-ID: {queue.ftQueueId}",
                DataBase64 = null,
                TimeStamp = 0
            };
            result.actionJournals[0].ftActionJournalId.Should().NotBe(Guid.Empty);
            result.actionJournals[0].ftQueueId.Should().Be(expectedActionJournal.ftQueueId);
            result.actionJournals[0].ftQueueItemId.Should().Be(expectedActionJournal.ftQueueItemId);
            result.actionJournals[0].Moment.Should().BeCloseTo(expectedActionJournal.Moment, 1000);
            result.actionJournals[0].Priority.Should().Be(expectedActionJournal.Priority);
            result.actionJournals[0].Type.Should().Be(expectedActionJournal.Type);
            result.actionJournals[0].Message.Should().Be(expectedActionJournal.Message);
            result.actionJournals[0].DataBase64.Should().Be(expectedActionJournal.DataBase64);
            result.actionJournals[0].TimeStamp.Should().Be(expectedActionJournal.TimeStamp);

            var data = JsonConvert.DeserializeObject<ActivateQueueES>(result.actionJournals[0].DataJson);
            data.CashBoxId.Should().Be(receiptRequest.ftCashBoxID.GetValueOrDefault());
            data.IsStartReceipt.Should().Be(true);
            data.Moment.Should().BeCloseTo(DateTime.UtcNow, 1000);
            data.QueueId.Should().Be(queueItem.ftQueueId);
            data.Version.Should().Be("V0");

            configMock.Verify(x => x.ActivateQueueAsync(), Times.Exactly(1));
        }

        [Fact(Skip = "not fully implemented")]
        public async Task OutOfOperationReceipt0x4002Async_ShouldReturnActionJournal_InitOperationSignature_AndSetStateInQueue()
        {
            var queue = TestHelpers.CreateQueue();
            queue.StartMoment = DateTime.UtcNow;

            var queueItem = TestHelpers.CreateQueueItem();

            var signaturCreationUnitES = new Storage.ES.ftSignaturCreationUnitES
            {

            };

            var masterDataConfiguration = new MasterDataConfiguration
            {

            };

            var configMock = new Mock<ILocalizedQueueStorageProvider>();
            var sut = new LifecycleCommandProcessorES(configMock.Object);

            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftReceiptCase = (ReceiptCase) (0x4752_2000_0000_0000 | (long) ReceiptCase.OutOfOperationReceipt0x4002)
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State) 0x4752_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "receiptIdentification",
                ftReceiptMoment = DateTime.UtcNow,
            };

            var request = new ProcessCommandRequest(queue, receiptRequest, receiptResponse);

            var result = await sut.OutOfOperationReceipt0x4002Async(request);

            using var scope = new AssertionScope();
            queue.StopMoment.Should().BeCloseTo(DateTime.UtcNow, 1000);
            result.receiptResponse.Should().Be(receiptResponse);
            result.actionJournals.Should().NotBeEmpty();
            result.receiptResponse.ftSignatures.Should().NotBeEmpty();

            result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0001, because: $"ftState {result.receiptResponse.ftState.ToString("X")} is different than expected.");

            var expectedSignaturItem = new SignatureItem
            {
                ftSignatureType = (SignatureType) 0x4752_2000_0001_1002,
                ftSignatureFormat = SignatureFormat.Text,
                Caption = "Out-of-operation receipt",
                Data = $"Queue-ID: {queue.ftQueueId}"
            };

            result.receiptResponse.ftSignatures[0].Should().BeEquivalentTo(expectedSignaturItem);

            var expectedActionJournal = new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                Moment = DateTime.UtcNow,
                Priority = -1,
                Type = "4752200000004002-DeactivateQueueES",
                Message = $"Out-of-Operation receipt. Queue-ID: {queue.ftQueueId}",
                DataBase64 = null,
                TimeStamp = 0
            };
            result.actionJournals[0].ftActionJournalId.Should().NotBe(Guid.Empty);
            result.actionJournals[0].ftQueueId.Should().Be(expectedActionJournal.ftQueueId);
            result.actionJournals[0].ftQueueItemId.Should().Be(expectedActionJournal.ftQueueItemId);
            result.actionJournals[0].Moment.Should().BeCloseTo(expectedActionJournal.Moment, 1000);
            result.actionJournals[0].Priority.Should().Be(expectedActionJournal.Priority);
            result.actionJournals[0].Type.Should().Be(expectedActionJournal.Type);
            result.actionJournals[0].Message.Should().Be(expectedActionJournal.Message);
            result.actionJournals[0].DataBase64.Should().Be(expectedActionJournal.DataBase64);
            result.actionJournals[0].TimeStamp.Should().Be(expectedActionJournal.TimeStamp);

            var data = JsonConvert.DeserializeObject<DeactivateQueueES>(result.actionJournals[0].DataJson);
            data.CashBoxId.Should().Be(receiptRequest.ftCashBoxID.GetValueOrDefault());
            data.IsStopReceipt.Should().Be(true);
            data.Moment.Should().BeCloseTo(DateTime.UtcNow, 1000);
            data.QueueId.Should().Be(queueItem.ftQueueId);
            data.Version.Should().Be("V0");

            configMock.Verify(x => x.ActivateQueueAsync(), Times.Exactly(1));
        }

        [Fact]
        public async Task InitSCUSwitch0x4011Async_ShouldDoNothing()
        {
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();

            var signaturCreationUnitES = new Storage.ES.ftSignaturCreationUnitES
            {

            };

            var masterDataConfiguration = new MasterDataConfiguration
            {

            };

            var configMock = new Mock<ILocalizedQueueStorageProvider>();
            var sut = new LifecycleCommandProcessorES(configMock.Object);

            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftReceiptCase = (ReceiptCase) (0x4752_2000_0000_0000 | (long) ReceiptCase.InitialOperationReceipt0x4001)
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State) 0x4752_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "receiptIdentification",
                ftReceiptMoment = DateTime.UtcNow,
            };

            var request = new ProcessCommandRequest(queue, receiptRequest, receiptResponse);
            var result = await sut.InitSCUSwitch0x4011Async(request);

            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000);
            result.receiptResponse.ftSignatures.Should().BeEmpty();
            result.actionJournals.Should().BeEmpty();
        }

        [Fact]
        public async Task FinishSCUSwitch0x4012Async_ShouldDoNothing()
        {
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();

            var signaturCreationUnitES = new Storage.ES.ftSignaturCreationUnitES
            {

            };

            var masterDataConfiguration = new MasterDataConfiguration
            {

            };

            var configMock = new Mock<ILocalizedQueueStorageProvider>();
            var sut = new LifecycleCommandProcessorES(configMock.Object);

            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftReceiptCase = (ReceiptCase) (0x4752_2000_0000_0000 | (long) ReceiptCase.InitialOperationReceipt0x4001)
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State) 0x4752_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "receiptIdentification",
                ftReceiptMoment = DateTime.UtcNow,
            };
            var request = new ProcessCommandRequest(queue, receiptRequest, receiptResponse);
            var result = await sut.FinishSCUSwitch0x4012Async(request);

            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000);
            result.receiptResponse.ftSignatures.Should().BeEmpty();
            result.actionJournals.Should().BeEmpty();
        }
    }
}
