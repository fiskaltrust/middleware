using System.Linq;
using System.Threading.Tasks;
using Xunit;
using fiskaltrust.Middleware.Localization.QueueAT.RequestCommands;
using FluentAssertions;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueAT.UnitTest.RequestCommands
{
    public class ZeroReceiptCommandTest : IClassFixture<SignProcessorATFixture>
    {

        private readonly SignProcessorATFixture _fixture;
        public ZeroReceiptCommandTest(SignProcessorATFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnExpectedResult()
        {
            await _fixture.CreateConfigurationRepository();
            var _initialOperationReceiptCommand = new InitialOperationReceiptCommand(_fixture.GetIATSSCDProvider("34"), _fixture.middlewareConfiguration, _fixture.queueATConfiguration, _fixture.logger);

            var request = _fixture.CreateReceiptRequest("ZeroReceipt.json");
            var queueItem = _fixture.CreateQueueItem(request);
            var response = RequestCommand.CreateReceiptResponse(request, queueItem, _fixture.queueAT, _fixture.queue);

            var result = await _initialOperationReceiptCommand.ExecuteAsync(_fixture.queue, _fixture.queueAT, request, queueItem, response);

            _fixture.TestCommandResult(request, queueItem, result);
            SignProcessorATFixture.TestAllSignSignatures(result);
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