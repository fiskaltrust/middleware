using System;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueFR.IntegrationTest.Fixtures;
using fiskaltrust.Middleware.Localization.QueueFR.RequestCommands;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.IntegrationTest.RequestCommands
{
    public class ZeroReceiptCommandTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly SignProcessorDependenciesFixture _fixture;
        public ZeroReceiptCommandTests(SignProcessorDependenciesFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task ExecuteAsync_ShouldCreateTrainingReceiptResponse_WhenRequestHasTrainingReceiptFlag()
        {
            // Arrange
            _fixture.CreateConfigurationRepository();
            var queueItemRepository = new InMemoryQueueItemRepository();
            var actionJournalRepository = new InMemoryActionJournalRepository();
            var command = new ZeroReceiptCommand(_fixture.signatureFactoryFR, queueItemRepository, actionJournalRepository);
            var (request, queueItem) = _fixture.CreateReceiptRequest(0x4652000000020001);

            // Act
            var (receiptResponse, journalFR, _) = await command.ExecuteAsync(_fixture.queue, _fixture.queueFR, _fixture.signaturCreationUnitFR, request, queueItem);

            // Assert
            _fixture.CheckResponse(request, queueItem, receiptResponse, journalFR);
            SignProcessorDependenciesFixture.CheckTraining(receiptResponse);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCreateDefaultReceiptResponse_WhenRequestDoesNotHaveTrainingReceiptFlag()
        {
            // Arrange
            _fixture.CreateConfigurationRepository();
            var queueItemRepository = new InMemoryQueueItemRepository();
            var actionJournalRepository = new InMemoryActionJournalRepository();
            var command = new ZeroReceiptCommand(_fixture.signatureFactoryFR, queueItemRepository, actionJournalRepository);
            var (request, queueItem) = _fixture.CreateReceiptRequest(0x4652000000000001);

            _fixture.queueFR.UsedFailedCount = 5;

            var failedqueueItem = new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
                response = JsonConvert.SerializeObject(new ReceiptResponse() { ftReceiptIdentification = "ftB#I1" })
            };
            await queueItemRepository.InsertAsync(failedqueueItem);
            _fixture.queueFR.UsedFailedQueueItemId = failedqueueItem.ftQueueItemId; 

            // Act
            var (receiptResponse, journalFR, actionJournals) = await command.ExecuteAsync(_fixture.queue, _fixture.queueFR, _fixture.signaturCreationUnitFR, request, queueItem);

            // Assert
            _fixture.CheckResponse(request, queueItem, receiptResponse, journalFR);
            "ftA#G1".Should().Be(receiptResponse.ftReceiptIdentification);

            journalFR.ReceiptType.Should().Be("G");
            journalFR.Number.Should().Be(11);
            actionJournals.Should().Contain(x => x.Message == $"QueueItem {queueItem.ftQueueItemId} recovered Queue {_fixture.queueFR.ftQueueFRId} from used-failed mode. closing chain of failed receipts from ftB#I1 to ftA#G1.");
            receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Failure registered");

            //reset used-fail mode
            _fixture.queueFR.UsedFailedCount.Should().Be(0);
            _fixture.queueFR.UsedFailedMomentMin.Should().BeNull();
            _fixture.queueFR.UsedFailedMomentMax.Should().BeNull();
            _fixture.queueFR.UsedFailedQueueItemId.Should().BeNull();
        }

        [Fact]
        public void Validate_ShouldReturnValidationError_WhenRequestDoesNotHaveValidPayItems()
        {
            // Arrange
            _fixture.CreateConfigurationRepository();
            var queueItemRepository = new InMemoryQueueItemRepository();
            var actionJournalRepository = new InMemoryActionJournalRepository();
            var command = new ZeroReceiptCommand(_fixture.signatureFactoryFR, queueItemRepository, actionJournalRepository);
            var (request, queueItem) = _fixture.CreateReceiptRequest(0x4652000000000001, 0x4652000000000001);

            // Act
            var result = command.Validate(_fixture.queue, _fixture.queueFR, request, queueItem);

            // Assert
            result.Count().Should().Be(2);
            result.ToList().Should().Contain(x => x.Message == "The Zero receipt must not have charge items.");
            result.ToList().Should().Contain(x => x.Message == "The Zero receipt must not have pay items.");
        }
    }
}
