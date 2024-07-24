using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Closings
{
    public class DailyClosingTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly SignProcessorDependenciesFixture _fixture;
        private readonly ClosingTests _closingTests;
        private readonly ReceiptTests _receiptTests;

        public DailyClosingTests(SignProcessorDependenciesFixture fixture)
        {
            _fixture = fixture;
            _closingTests = new ClosingTests(fixture);
            _receiptTests = new ReceiptTests(fixture);
        }

        [Fact]
        public async Task SignProcessor_DailyClosingReceipt_ShouldFinishAllOpenTransactions()
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(File.ReadAllText(Path.Combine("Data", "DailyClosingReceipt", "Request.json")));
            var expectedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(File.ReadAllText(Path.Combine("Data", "DailyClosingReceipt", "Response.json")));
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

            await AddOpenOrdersAsync();

            var journalRepositoryMock = new Mock<InMemoryJournalDERepository>()
            {
                CallBase = true
            };
            journalRepositoryMock.Setup(x => x.InsertAsync(It.IsAny<ftJournalDE>())).CallBase().Verifiable();
            var actionJournalRepositoryMock = new Mock<IMiddlewareActionJournalRepository>(MockBehavior.Strict);
            var config = new MiddlewareConfiguration { Configuration = new Dictionary<string, object>(), QueueId = queue.ftQueueId, ServiceFolder = Directory.GetCurrentDirectory() };


            var tarFileCleanupService = new TarFileCleanupService(Mock.Of<ILogger<TarFileCleanupService>>(), journalRepositoryMock.Object, config, QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config));
            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(Mock.Of<ILogger<SignProcessorDE>>(), _fixture.CreateConfigurationRepository(), journalRepositoryMock.Object, actionJournalRepositoryMock.Object,
                _fixture.DeSSCDProvider, new DSFinVKTransactionPayloadFactory(Mock.Of<ILogger<DSFinVKTransactionPayloadFactory>>()), new InMemoryFailedFinishTransactionRepository(), new InMemoryFailedStartTransactionRepository(),
                _fixture.openTransactionRepository, Mock.Of<IMasterDataService>(), config, new InMemoryQueueItemRepository(), new SignatureFactoryDE(QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config)), tarFileCleanupService);
            
            
            var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, queue, queueItem);

            receiptResponse.Should().BeEquivalentTo(expectedResponse, x => x.Excluding(x => x.ftReceiptMoment));
            (await _fixture.openTransactionRepository.GetAsync()).Should().BeEmpty();
            journalRepositoryMock.Verify();
        }

        [Fact]
        public async Task SignProcessor_DailyClosingReceipt_ShouldFinishAllOpenTransactionsThatAreNotOnTse()
        {
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1), null, null, false, true);
            await AddOpenOrdersAsync();
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "DailyClosingReceipt"));
            receiptRequest.ftReceiptCase = 0x4445000120000007;
            var response = await signProcessor.ProcessAsync(receiptRequest);
            (await _fixture.openTransactionRepository.GetAsync()).Should().BeEmpty();
            await ReceiptTestResults.IsResponseValidAsync(_fixture, response, receiptRequest, "Removed open transaction 3 from the database, which was not open on the TSE anymore.");

        }
        [Fact]
        public async Task DailyClosing_MasterDataChange_CheckAccountOutlet() => await _closingTests.ClosingTests_MasterDataChange_CheckAccountOutlet("DailyClosingReceipt", $"Daily-Closing receipt was processed, and a master data update was performed.", 0x4445000108000007).ConfigureAwait(false);
        [Fact]
        public async Task Closing_IsNoImplicitFlow_ExpectArgumentException() => await DailyClosing_ExpectArgumentException(0x4445000000000007, "ReceiptCase {0:x} (Daily-closing receipt) must use implicit-flow flag.").ConfigureAwait(false);
        [Fact]
        public async Task DailyClosing_IsTraining_ExpectArgumentException() => await DailyClosing_ExpectArgumentException(0x4445000100020007, "ReceiptCase {0:x} can not use 'TrainingMode' flag.").ConfigureAwait(false);
        [Fact]
        public async Task DailyClosing_HasFailOnOpenTransaction_ExpectFailedMode()
        {
            await AddOpenOrdersAsync();
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "DailyClosingReceipt"));
            receiptRequest.ftReceiptCase = 0x4445000110000007;
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1), null, null, false, true);
            var response =  signProcessor.ProcessAsync(receiptRequest).Result;
            var isFailedMode = (response.ftState & 0x0000_0000_0000_0002) > 0x0000;
            isFailedMode.Should().BeTrue();
        }
        private async Task DailyClosing_ExpectArgumentException(long receiptCase, string errorMessage)
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "DailyClosingReceipt"));
            receiptRequest.ftReceiptCase = receiptCase;
            await _receiptTests.ExpectArgumentExceptionReceiptcase(receiptRequest, errorMessage);
        }

        private async Task AddOpenOrdersAsync()
        {
            await _fixture.AddOpenOrders("A1", 1);
            await _fixture.AddOpenOrders("A2", 2);
            await _fixture.AddOpenOrders("A3", 3);
        }
    }
}
