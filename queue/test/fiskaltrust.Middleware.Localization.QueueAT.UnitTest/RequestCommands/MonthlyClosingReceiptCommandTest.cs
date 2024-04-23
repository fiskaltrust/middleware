using System.Linq;
using System.Threading.Tasks;
using Xunit;
using fiskaltrust.Middleware.Localization.QueueAT.RequestCommands;
using FluentAssertions;
using Newtonsoft.Json;
using Moq;
using fiskaltrust.Middleware.Localization.QueueAT.Services;

namespace fiskaltrust.Middleware.Localization.QueueAT.UnitTest.RequestCommands
{
    public class MonthlyClosingReceiptCommandTest : IClassFixture<SignProcessorATFixture>
    {
        private readonly SignProcessorATFixture _fixture;
        public MonthlyClosingReceiptCommandTest(SignProcessorATFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnExpectedResult()
        {
            await _fixture.CreateConfigurationRepository();
            var exportService = new Mock<IExportService>().Object;
            var _initialOperationReceiptCommand = new MonthlyClosingReceiptCommand(_fixture.GetIATSSCDProvider("34"), _fixture.middlewareConfiguration, _fixture.queueATConfiguration, exportService, _fixture.logger);

            var request = _fixture.CreateReceiptRequest("MonthlyReceipt.json");
            var queueItem = _fixture.CreateQueueItem(request);
            var response = RequestCommand.CreateReceiptResponse(request, queueItem, _fixture.queueAT, _fixture.queue);

            var result = await _initialOperationReceiptCommand.ExecuteAsync(_fixture.queue, _fixture.queueAT, request, queueItem, response);

            _fixture.TestCommandResult(request, queueItem, result);
            SignProcessorATFixture.TestAllSignSignatures(result, true, "", false);
            var sign = result.ReceiptResponse.ftSignatures.ToList().Where(x => x.Caption == "Monatsbeleg #0").FirstOrDefault();
            sign.Should().NotBeNull();
            sign.ftSignatureType.Should().Be(4707387510509010946);

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