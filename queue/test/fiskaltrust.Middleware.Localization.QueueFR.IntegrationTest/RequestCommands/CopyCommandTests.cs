using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueFR.IntegrationTest.Fixtures;
using fiskaltrust.Middleware.Localization.QueueFR.RequestCommands;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.FR;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.IntegrationTest.RequestCommands
{
    public class CopyCommandTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly SignProcessorDependenciesFixture _fixture;
        public CopyCommandTests(SignProcessorDependenciesFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task ExecuteAsync_ShouldCreateTrainingReceiptResponse_WhenRequestHasTrainingReceiptFlag()
        {
            // Arrange
            _fixture.CreateConfigurationRepository();
            var journalFRCopyPayloadRepository = new InMemoryJournalFRCopyPayloadRepository();
            var command = new CopyCommand(_fixture.signatureFactoryFR, journalFRCopyPayloadRepository);
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
            var journalFRCopyPayloadRepository = new InMemoryJournalFRCopyPayloadRepository();
            var command = new CopyCommand(_fixture.signatureFactoryFR, journalFRCopyPayloadRepository);
            var (request, queueItem) = _fixture.CreateReceiptRequest(0x4652000000000001);

            // Act
            var (receiptResponse, journalFR, actionJournals) = await command.ExecuteAsync(_fixture.queue, _fixture.queueFR, _fixture.signaturCreationUnitFR, request, queueItem);

            // Assert
            _fixture.CheckResponse(request, queueItem, receiptResponse, journalFR);
            "ftA#C1".Should().Be(receiptResponse.ftReceiptIdentification);
            receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Duplicata");
            journalFR.ReceiptType.Should().Be("C");
            journalFR.Number.Should().Be(11);
            var copies = await journalFRCopyPayloadRepository.GetAsync();
            copies.Should().HaveCount(1);
            copies.First().QueueItemId.Should().Be(queueItem.ftQueueItemId);
        }

        [Fact]
        public void Validate_ShouldReturnValidationError_WhenRequestDoesNotHaveValidPayItems()
        {
            // Arrange
            _fixture.CreateConfigurationRepository();
            var journalFRCopyPayloadRepository = new InMemoryJournalFRCopyPayloadRepository();
            var command = new CopyCommand(_fixture.signatureFactoryFR, journalFRCopyPayloadRepository);
            var (request, queueItem) = _fixture.CreateReceiptRequest(0x4652000000000001, 0x4652000000000001);

            // Act
            var result = command.Validate(_fixture.queue, _fixture.queueFR, request, queueItem);

            // Assert
            result.Count().Should().Be(3);
            result.ToList().Should().Contain(x => x.Message == "The Copy receipt must not have charge items.");
            result.ToList().Should().Contain(x => x.Message == "The Copy receipt must not have pay items.");
            result.ToList().Should().Contain(x => x.Message == "The Copy receipt must provide the POS System receipt reference of the receipt whose the copy has been asked.");
        }
    }
}
