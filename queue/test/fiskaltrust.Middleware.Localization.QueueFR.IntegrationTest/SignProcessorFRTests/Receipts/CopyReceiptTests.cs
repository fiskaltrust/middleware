using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.IntegrationTest.Fixtures;
using fiskaltrust.Middleware.Localization.QueueFR.IntegrationTest.Helpers;
using fiskaltrust.Middleware.Queue.Helpers;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.FR;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using ftJournalFRCopyPayload = fiskaltrust.Middleware.Contracts.Models.FR.ftJournalFRCopyPayload;

namespace fiskaltrust.Middleware.Localization.QueueFR.IntegrationTest.SignProcessorFRTests.Receipts
{
    public class PosReceiptTests: IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly SignProcessorDependenciesFixture _fixture;
        public PosReceiptTests(SignProcessorDependenciesFixture fixture) => _fixture = fixture;
        [Fact]
        public async Task SignProcessor_CopyReceipt_ShouldReturnValidResponse()
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(File.ReadAllText(Path.Combine("Data", "CopyReceipt", "Request.json")));
            var expectedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(File.ReadAllText(Path.Combine("Data", "CopyReceipt", "Response.json")));
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = receiptRequest.cbReceiptMoment,
                cbReceiptReference = receiptRequest.cbReceiptReference,
                cbTerminalID = receiptRequest.cbTerminalID,
                country = "FR",
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                ftQueueItemId = Guid.Parse(expectedResponse.ftQueueItemID),
                ftQueueRow = expectedResponse.ftQueueRow,
                request = JsonConvert.SerializeObject(receiptRequest),
                requestHash = "test request hash"
            };
            var queue = new ftQueue
            {
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                StartMoment = DateTime.UtcNow
            };

            var journalRepositoryMock = new Mock<IMiddlewareJournalFRRepository>(MockBehavior.Strict);
            var actionJournalRepositoryMock = new Mock<IActionJournalRepository>(MockBehavior.Strict);
            actionJournalRepositoryMock.Setup(x => x.InsertAsync(It.IsAny<ftActionJournal>())).Returns(Task.CompletedTask);
            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(Mock.Of<ILogger<SignProcessorFR>>(),
                _fixture.CreateConfigurationRepository(),
                journalRepositoryMock.Object,
                actionJournalRepositoryMock.Object,
                new InMemoryQueueItemRepository(),
                new SignatureFactoryFR(new CryptoHelper()),
                new InMemoryJournalFRCopyPayloadRepository());

            var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, queue, queueItem);

            receiptResponse.Should().BeEquivalentTo(expectedResponse, x => x
                .Excluding(x => x.ftReceiptMoment)
                .Excluding(x => x.ftReceiptIdentification)
                .Excluding(x => x.ftSignatures));
            receiptResponse.ftSignatures.Length.Should().Be(expectedResponse.ftSignatures.Length);
        }
        [Fact]
        public async Task SignProcessor_SecondCopyReceipt_ShouldReturnValidResponse()
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(File.ReadAllText(Path.Combine("Data", "CopyReceipt", "Request.json")));
            var expectedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(File.ReadAllText(Path.Combine("Data", "CopyReceipt", "Response.json")));
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = receiptRequest.cbReceiptMoment,
                cbReceiptReference = receiptRequest.cbReceiptReference,
                cbTerminalID = receiptRequest.cbTerminalID,
                country = "FR",
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                ftQueueItemId = Guid.Parse(expectedResponse.ftQueueItemID),
                ftQueueRow = expectedResponse.ftQueueRow,
                request = JsonConvert.SerializeObject(receiptRequest),
                requestHash = "test request hash"
            };
            var queue = new ftQueue
            {
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                StartMoment = DateTime.UtcNow
            };

            var journalRepositoryMock = new Mock<IMiddlewareJournalFRRepository>(MockBehavior.Strict);
            var actionJournalRepositoryMock = new Mock<IActionJournalRepository>(MockBehavior.Strict);
            var inMemoryJournalFRCopyPayloadRepository = new InMemoryJournalFRCopyPayloadRepository();
            await inMemoryJournalFRCopyPayloadRepository.InsertAsync(new ftJournalFRCopyPayload()
            {
                CopiedReceiptReference = receiptRequest.cbPreviousReceiptReference,
                QueueItemId = Guid.NewGuid()
            });
            actionJournalRepositoryMock.Setup(x => x.InsertAsync(It.IsAny<ftActionJournal>())).Returns(Task.CompletedTask);
            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(Mock.Of<ILogger<SignProcessorFR>>(),
                _fixture.CreateConfigurationRepository(),
                journalRepositoryMock.Object,
                actionJournalRepositoryMock.Object,
                new InMemoryQueueItemRepository(),
                new SignatureFactoryFR(new CryptoHelper()),
                inMemoryJournalFRCopyPayloadRepository);

            var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, queue, queueItem);

            receiptResponse.ftSignatures.Where(s => s.ftSignatureType == 5067112530745229312).First().Caption =
                "2. Duplicata de 23334";
            receiptResponse.Should().BeEquivalentTo(expectedResponse, x => x
                .Excluding(x => x.ftReceiptMoment)
                .Excluding(x => x.ftReceiptIdentification)
                .Excluding(x => x.ftSignatures));
            receiptResponse.ftSignatures.Length.Should().Be(expectedResponse.ftSignatures.Length);

            receiptRequest.cbReceiptReference = "2333466";
            queueItem.ftQueueItemId = Guid.NewGuid();
            var (receiptResponse2, actionJournals2) = await sut.ProcessAsync(receiptRequest, queue, queueItem);
            expectedResponse.cbReceiptReference = receiptRequest.cbReceiptReference;
            expectedResponse.ftQueueItemID = queueItem.ftQueueItemId.ToString();
            receiptResponse2.ftSignatures.Where(s => s.ftSignatureType == 5067112530745229312).First().Caption =
                "3. Duplicata de 23334";
            receiptResponse2.Should().BeEquivalentTo(expectedResponse, x => x
                .Excluding(x => x.ftReceiptMoment)
                .Excluding(x => x.ftReceiptIdentification)
                .Excluding(x => x.ftSignatures));
            receiptResponse2.ftSignatures.Length.Should().Be(expectedResponse.ftSignatures.Length);
        }
    }
}
