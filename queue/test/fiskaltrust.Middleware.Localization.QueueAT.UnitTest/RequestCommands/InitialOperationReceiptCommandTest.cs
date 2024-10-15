using System.Linq;
using System.Threading.Tasks;
using Xunit;
using fiskaltrust.Middleware.Localization.QueueAT.RequestCommands;
using FluentAssertions;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueAT.UnitTest.RequestCommands
{
    public class InitialOperationReceiptCommandTest : IClassFixture<SignProcessorATFixture>
    {

        private readonly SignProcessorATFixture _fixture;
        public InitialOperationReceiptCommandTest(SignProcessorATFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnExpectedResult()
        {
            await _fixture.CreateConfigurationRepository();
            var _initialOperationReceiptCommand = new InitialOperationReceiptCommand(_fixture.GetIATSSCDProvider("34"), _fixture.middlewareConfiguration, _fixture.queueATConfiguration, _fixture.logger);

            var request = _fixture.CreateReceiptRequest("InitialOperation.json");
            var queueItem = _fixture.CreateQueueItem(request);
            var response = RequestCommand.CreateReceiptResponse(request, queueItem, _fixture.queueAT, _fixture.queue);

            var result = await _initialOperationReceiptCommand.ExecuteAsync(_fixture.queue, _fixture.queueAT, request, queueItem, response);

            _fixture.TestCommandResult(request, queueItem, result);
            SignProcessorATFixture.TestAllSignSignatures(result);
            var signStopp = result.ReceiptResponse.ftSignatures.ToList().Where(x => x.Caption == "Startbeleg").FirstOrDefault();
            signStopp.Should().NotBeNull();
            signStopp.Data.Should().Be("Startbeleg");
            signStopp.ftSignatureType.Should().Be(4707387510509010946);
            var signDA = result.ReceiptResponse.ftSignatures.ToList().Where(x => x.Caption.Contains("Aktivierung (Inbetriebnahme)")).FirstOrDefault();
            signDA.Should().NotBeNull();
            signDA.Data.Should().Contain("ActionJournalId");
            signDA.ftSignatureType.Should().Be(4707387510509010947);
            var ftStateData = JsonConvert.DeserializeAnonymousType(result.ReceiptResponse.ftStateData,
                new
                {
                    Exception = string.Empty,
                    Signing = false,
                    Counting = false,
                    ZeroReceipt = false
                });
            ftStateData.Exception.Should().BeEmpty();
            ftStateData.Signing.Should().BeTrue();
            ftStateData.Counting.Should().BeTrue();
            ftStateData.ZeroReceipt.Should().BeTrue();

        }
    }
}