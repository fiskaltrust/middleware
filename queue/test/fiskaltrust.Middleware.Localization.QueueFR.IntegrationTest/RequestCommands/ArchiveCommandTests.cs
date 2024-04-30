using System;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Internal;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueFR.IntegrationTest.Fixtures;
using fiskaltrust.Middleware.Localization.QueueFR.RequestCommands;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.FR;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;


namespace fiskaltrust.Middleware.Localization.QueueFR.IntegrationTest.RequestCommands
{
    public class ArchiveCommandTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly SignProcessorDependenciesFixture _fixture;
        public ArchiveCommandTests(SignProcessorDependenciesFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task ExecuteAsync_ShouldCreateTrainingReceiptResponse_WhenRequestHasTrainingReceiptFlag()
        {
            // Arrange
            _fixture.CreateConfigurationRepository();

            var queueItemRepository = new InMemoryQueueItemRepository();
            var actionJournalRepository = new InMemoryActionJournalRepository();
            var journalRepository = new InMemoryReceiptJournalRepository();
            var journalFRRepository = new InMemoryJournalFRRepository();
            var archiveProcessor = new Mock<IArchiveProcessor>();
            var middlewareconfig = new MiddlewareConfiguration
            {
                Configuration = new System.Collections.Generic.Dictionary<string, object>()
            };
            middlewareconfig.Configuration.Add("frarchiveexport", false);
            var logger = new Mock<ILogger<ArchiveCommand>>();

            var command = new ArchiveCommand(_fixture.signatureFactoryFR, middlewareconfig, archiveProcessor.Object, actionJournalRepository, queueItemRepository, journalRepository, journalFRRepository, logger.Object);

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
            var item1 = new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = _fixture.queueFR.ftQueueFRId,
                TimeStamp = DateTime.Now.Ticks,
            };
            var item2 = new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = _fixture.queueFR.ftQueueFRId,
                TimeStamp = DateTime.Now.Ticks,
            };
            queueItemRepository.InsertAsync(item1).Wait();
            queueItemRepository.InsertAsync(item2).Wait();
            var actionJournalRepository = new InMemoryActionJournalRepository();
            var journalRepository = new InMemoryReceiptJournalRepository();

            await journalRepository.InsertAsync(new ftReceiptJournal() { ftReceiptJournalId = Guid.NewGuid(), ftQueueItemId = item1.ftQueueItemId, ftReceiptNumber = 1 });
            await journalRepository.InsertAsync( new ftReceiptJournal() { ftReceiptJournalId = Guid.NewGuid(), ftQueueItemId = item2.ftQueueItemId, ftReceiptNumber =2 });

            var journalFRRepository = new InMemoryJournalFRRepository();
            var archiveProcessor = new Mock<IArchiveProcessor>();
            var middlewareconfig = new MiddlewareConfiguration
            {
                Configuration = new System.Collections.Generic.Dictionary<string, object>()
            };
            middlewareconfig.Configuration.Add("frarchiveexport", false);
            var logger = new Mock<ILogger<ArchiveCommand>>();

            var command = new ArchiveCommand(_fixture.signatureFactoryFR, middlewareconfig, archiveProcessor.Object, actionJournalRepository, queueItemRepository, journalRepository, journalFRRepository, logger.Object);

            var (request, queueItem) = _fixture.CreateReceiptRequest(0x4652000000000001);

            // Act
            var (receiptResponse, journalFR, actionJournals) = await command.ExecuteAsync(_fixture.queue, _fixture.queueFR, _fixture.signaturCreationUnitFR, request, queueItem);

            // Assert
            _fixture.CheckResponse(request, queueItem, receiptResponse, journalFR);
            "ftA#A1".Should().Be(receiptResponse.ftReceiptIdentification);
            receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Archive Totals");
            receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Day Totals");

            journalFR.ReceiptType.Should().Be("A");
            journalFR.Number.Should().Be(11);
            actionJournals.Should().Contain(x => x.Message == "Archive requested");

        }

        [Fact]
        public void Validate_ShouldReturnValidationError_WhenRequestDoesNotHaveValidPayItems()
        {
            // Arrange
            _fixture.CreateConfigurationRepository();
            var queueItemRepository = new InMemoryQueueItemRepository();
            var actionJournalRepository = new InMemoryActionJournalRepository();
            var journalRepository = new InMemoryReceiptJournalRepository();
            var journalFRRepository = new InMemoryJournalFRRepository();
            var archiveProcessor = new Mock<IArchiveProcessor>();
            var middlewareconfig = new MiddlewareConfiguration
            {
                Configuration = new System.Collections.Generic.Dictionary<string, object>()
            };
            middlewareconfig.Configuration.Add("frarchiveexport", false);
            var logger = new Mock<ILogger<ArchiveCommand>>();

            var command = new ArchiveCommand(_fixture.signatureFactoryFR, middlewareconfig, archiveProcessor.Object, actionJournalRepository, queueItemRepository, journalRepository, journalFRRepository, logger.Object);

            var (request, queueItem) = _fixture.CreateReceiptRequest(0x4652000000000001, 0x4652000000000001);

            // Act
            var result = command.Validate(_fixture.queue, _fixture.queueFR, request, queueItem);

            // Assert
            result.Count().Should().Be(2);
            result.ToList().Should().Contain(x => x.Message == "The Archive receipt must not have charge items.");
            result.ToList().Should().Contain(x => x.Message == "The Archive receipt must not have pay items.");
        }
    }
}
