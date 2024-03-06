using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class InitiateScuSwitchTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly SignProcessorDependenciesFixture _fixture;
        private readonly ReceiptTests _receiptTests;
        private readonly ReceiptProcessorHelper _receiptProcessorHelper;

        public InitiateScuSwitchTests(SignProcessorDependenciesFixture fixture)
        {
            _receiptTests = new ReceiptTests(fixture);
            _fixture = fixture;
            _receiptProcessorHelper = new ReceiptProcessorHelper(_fixture.SignProcessor);
        }

        [Fact]
        public async Task SignProcessor_InitScuSwitchReceipt_SCUReachableShouldSign()
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(File.ReadAllText(Path.Combine("Data", "InitiateScuSwitchReceipt", "Request.json")));
            var expectedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(File.ReadAllText(Path.Combine("Data", "InitiateScuSwitchReceipt", "Response.json")));
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = receiptRequest.cbReceiptMoment,
                cbReceiptReference = receiptRequest.cbReceiptReference,
                cbTerminalID = receiptRequest.cbTerminalID,
                country = "DE",
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

            var journalRepositoryMock = new Mock<IMiddlewareJournalDERepository>(MockBehavior.Strict);
            var actionJournalRepositoryMock = new Mock<IActionJournalRepository>(MockBehavior.Strict);

            actionJournalRepositoryMock.Setup(a => a.GetAsync()).ReturnsAsync(new List<ftActionJournal>
            {
                CreateftActionJournal(queue.ftQueueId, queueItem.ftQueueItemId, 0)
            });

            var config = new MiddlewareConfiguration { Configuration = new Dictionary<string, object>() };
            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(Mock.Of<ILogger<SignProcessorDE>>(), _fixture.CreateConfigurationRepository(false, null, null, true, true), journalRepositoryMock.Object,
                actionJournalRepositoryMock.Object, _fixture.DeSSCDProvider, new DSFinVKTransactionPayloadFactory(Mock.Of<ILogger<DSFinVKTransactionPayloadFactory>>()), new InMemoryFailedFinishTransactionRepository(),
                new InMemoryFailedStartTransactionRepository(), new InMemoryOpenTransactionRepository(), Mock.Of<IMasterDataService>(), config,
                new InMemoryQueueItemRepository(), new SignatureFactoryDE(QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config)));

            var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, queue, queueItem);

            receiptResponse.Should().BeEquivalentTo(expectedResponse, x => x
                .Excluding(x => x.ftReceiptMoment)
                .Excluding(x => x.ftReceiptIdentification)
                .Excluding(x => x.ftSignatures));
            receiptResponse.ftSignatures.Length.Should().Be(expectedResponse.ftSignatures.Length);
            receiptResponse.ftSignatures.Where(x => x.Caption.Equals("TSE-Trennungs-Beleg")).Should().HaveCount(1);
        }

        [Fact]
        public async Task SignProcessor_InitScuSwitchReceipt_SCUUnreachableShouldSign()
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(File.ReadAllText(Path.Combine("Data", "InitiateScuSwitchReceipt", "Request.json")));
            var expectedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(File.ReadAllText(Path.Combine("Data", "InitiateScuSwitchReceipt", "UnreachableResponse.json")));
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = receiptRequest.cbReceiptMoment,
                cbReceiptReference = receiptRequest.cbReceiptReference,
                cbTerminalID = receiptRequest.cbTerminalID,
                country = "DE",
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

            var journalRepositoryMock = new Mock<IMiddlewareJournalDERepository>(MockBehavior.Strict);
            var actionJournalRepositoryMock = new Mock<IActionJournalRepository>(MockBehavior.Strict);

            actionJournalRepositoryMock.Setup(a => a.GetAsync()).ReturnsAsync(new List<ftActionJournal>
            {
                CreateftActionJournal(queue.ftQueueId, queueItem.ftQueueItemId, 0)
            });

            var config = new MiddlewareConfiguration { Configuration = new Dictionary<string, object>() };
            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(Mock.Of<ILogger<SignProcessorDE>>(), _fixture.CreateConfigurationRepository(false, null, null, true, true), journalRepositoryMock.Object,
                actionJournalRepositoryMock.Object, _fixture.DeSSCDProvider, new DSFinVKTransactionPayloadFactory(Mock.Of<ILogger<DSFinVKTransactionPayloadFactory>>()), new InMemoryFailedFinishTransactionRepository(),
                new InMemoryFailedStartTransactionRepository(), new InMemoryOpenTransactionRepository(), Mock.Of<IMasterDataService>(), config,
                new InMemoryQueueItemRepository(), new SignatureFactoryDE(QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config)));

            _fixture.InMemorySCU.ShouldFail = true;
            var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, queue, queueItem);
            _fixture.InMemorySCU.ShouldFail = false;

            var tmp = JsonConvert.SerializeObject(receiptResponse);
            receiptResponse.Should().BeEquivalentTo(expectedResponse, x => x
                .Excluding(x => x.ftReceiptMoment)
                .Excluding(x => x.ftReceiptIdentification)
                .Excluding(x => x.ftSignatures));
            receiptResponse.ftSignatures.Length.Should().Be(expectedResponse.ftSignatures.Length);
        }

        [Fact]
        public async Task InitScuSwitchReceipt_IsNoImplicitFlow_ExpectErrorState()
        {
            var receiptRequest = _receiptTests.GetReceipt("InitiateScuSwitchReceipt", "InitiateScuSwitchNoImplFlow", 0x4445000000000017);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);
        }

        [Fact]
        public async Task InitScuSwitchReceipt_IsTraining_ExpectErrorState()
        {
            var receiptRequest = _receiptTests.GetReceipt("InitiateScuSwitchReceipt", "InitiateScuSwitchNoImplFlow", 0x4445000100020017);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);
        }

        [Fact]
        public async Task InitScuSwitchReceipt_NolastDailyClosingJournal_ExpectErrorState()
        {
            _fixture.actionJournalRepository = new InMemoryActionJournalRepository(); // Ustawienie repozytorium, jeśli potrzebne
            var receiptRequest = _receiptTests.GetReceipt("InitiateScuSwitchReceipt", "InitiateScuSwitchNoImplFlow", 0x4445000100000017);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);
        }

        [Fact]
        public async Task InitScuSwitchReceipt_SourceScuIsNoSwitchSource_ExpectErrorState()
        {
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = DateTime.Now,
                cbReceiptReference = "IsNoScuSwitchDC",
                cbTerminalID = "369a013a-37e2-4c23-8614-6a8f282e6330",
                country = "DE",
                ftQueueId = _fixture.QUEUEID,
                ftQueueItemId = Guid.NewGuid(),
                ftQueueRow = 10,
                request = "",
                requestHash = "test request hash"
            };
            await _fixture.queueItemRepository.InsertOrUpdateAsync(queueItem); // Wstawienie queueItem do repozytorium
            var receiptRequest = _receiptTests.GetReceipt("InitiateScuSwitchReceipt", "InitiateScuSwitchNoImplFlow", 0x4445000100000017);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);
        }
        
        [Fact]
        public async Task InitScuSwitchReceipt_TargetScuIsNoSwitchSource_ExpectErrorState()
        {
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = DateTime.Now,
                cbReceiptReference = "IsNoScuSwitchDC",
                cbTerminalID = "369a013a-37e2-4c23-8614-6a8f282e6330",
                country = "DE",
                ftQueueId = _fixture.QUEUEID,
                ftQueueItemId = Guid.NewGuid(),
                ftQueueRow = 10,
                request = "",
                requestHash = "test request hash"
            };
            await _fixture.queueItemRepository.InsertOrUpdateAsync(queueItem); // Wstawienie queueItem do repozytorium
            var receiptRequest = _receiptTests.GetReceipt("InitiateScuSwitchReceipt", "InitiateScuSwitchNoImplFlow", 0x4445000100000017);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);
        }

        private ftActionJournal CreateftActionJournal(Guid ftQueueId, Guid ftQueueItemId, int ftReceiptNumerator)
        {
            return new ftActionJournal
            {
                Type = "4445000000000007",
                Moment = DateTime.Now,
                DataJson = "{\"ftReceiptNumerator\": " + ftReceiptNumerator + "}",
                DataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"ftReceiptNumerator\": " + ftReceiptNumerator + "}")),
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = ftQueueId,
                ftQueueItemId = ftQueueItemId,
                Message = "",
                Priority = 0,
                TimeStamp = DateTime.Now.Ticks
            };
        }
    }
}
