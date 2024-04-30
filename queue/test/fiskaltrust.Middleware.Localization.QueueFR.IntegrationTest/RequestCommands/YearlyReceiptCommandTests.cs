using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Internal;
using fiskaltrust.Middleware.Localization.QueueFR.IntegrationTest.Fixtures;
using fiskaltrust.Middleware.Localization.QueueFR.RequestCommands;
using FluentAssertions;
using Xunit;


namespace fiskaltrust.Middleware.Localization.QueueFR.IntegrationTest.RequestCommands
{
    public class YearlyReceiptCommandTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly SignProcessorDependenciesFixture _fixture;
        public YearlyReceiptCommandTests(SignProcessorDependenciesFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task ExecuteAsync_ShouldCreateTrainingReceiptResponse_WhenRequestHasTrainingReceiptFlag()
        {
            // Arrange
            _fixture.CreateConfigurationRepository();
            var command = new YearlyReceiptCommand(_fixture.signatureFactoryFR);
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
            var command = new YearlyReceiptCommand(_fixture.signatureFactoryFR);
            var (request, queueItem) = _fixture.CreateReceiptRequest(0x4652000000000001);

            // Act
            var (receiptResponse, journalFR, actionJournals) = await command.ExecuteAsync(_fixture.queue, _fixture.queueFR, _fixture.signaturCreationUnitFR, request, queueItem);

            // Assert
            _fixture.CheckResponse(request, queueItem, receiptResponse, journalFR);
            "ftA#G1".Should().Be(receiptResponse.ftReceiptIdentification);
            receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Totals");
            receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Month Totals");
            receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Day Totals");
            receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Year Totals");

            journalFR.ReceiptType.Should().Be("G");
            journalFR.Number.Should().Be(11);
            actionJournals.Should().Contain(x => x.Message == "Yearly closure");

        }

        [Fact]
        public void Validate_ShouldReturnValidationError_WhenRequestDoesNotHaveValidPayItems()
        {
            // Arrange
            _fixture.CreateConfigurationRepository();
            var command = new YearlyReceiptCommand(_fixture.signatureFactoryFR);
            var (request, queueItem) = _fixture.CreateReceiptRequest(0x4652000000000001, 0x4652000000000001);

            // Act
            var result = command.Validate(_fixture.queue, _fixture.queueFR, request, queueItem);

            // Assert
            result.Count().Should().Be(2);
            result.ToList().Should().Contain(x => x.Message == "The Yearly receipt must not have charge items.");
            result.ToList().Should().Contain(x => x.Message == "The Yearly receipt must not have pay items.");
        }
    }
}
