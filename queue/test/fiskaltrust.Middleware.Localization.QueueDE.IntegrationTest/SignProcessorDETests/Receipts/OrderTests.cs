using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers;
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

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class OrderTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly SignProcessorDependenciesFixture _fixture;
        public OrderTests(SignProcessorDependenciesFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task SignProcessor_ImplicitOrderRequest_ShouldReturnValidResponse()
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(File.ReadAllText(Path.Combine("Data", "ImplicitOrderRequest", "Request.json")));
            var expectedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(File.ReadAllText(Path.Combine("Data", "ImplicitOrderRequest", "Response.json")));
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
            var config = new MiddlewareConfiguration { Configuration = new Dictionary<string, object>() };
            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(Mock.Of<ILogger<SignProcessorDE>>(), _fixture.CreateConfigurationRepository(), journalRepositoryMock.Object,
                actionJournalRepositoryMock.Object, _fixture.DeSSCDProvider, new DSFinVKTransactionPayloadFactory(), new InMemoryFailedFinishTransactionRepository(),
                new InMemoryFailedStartTransactionRepository(), new InMemoryOpenTransactionRepository(), Mock.Of<IMasterDataService>(), config,
                new InMemoryQueueItemRepository(), new SignatureFactoryDE(config));

            var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, queue, queueItem);

            receiptResponse.Should().BeEquivalentTo(expectedResponse, x => x
                .Excluding(x => x.ftReceiptMoment)
                .Excluding(x => x.ftReceiptIdentification)
                .Excluding(x => x.ftSignatures));
            receiptResponse.ftSignatures.Length.Should().Be(expectedResponse.ftSignatures.Length);
        }
    }
}
