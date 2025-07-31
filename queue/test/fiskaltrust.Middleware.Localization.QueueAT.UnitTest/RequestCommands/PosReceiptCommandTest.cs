using System.Threading.Tasks;
using Xunit;
using fiskaltrust.Middleware.Localization.QueueAT.RequestCommands;
using FluentAssertions;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueAT.UnitTest.RequestCommands
{
    public class PosReceiptCommandTest : IClassFixture<SignProcessorATFixture>
    {

        private readonly SignProcessorATFixture _fixture;
        public PosReceiptCommandTest(SignProcessorATFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnExpectedResult()
        {
            await _fixture.CreateConfigurationRepository();
            var _initialOperationReceiptCommand = new PosReceiptCommand(_fixture.GetIATSSCDProvider("35"), _fixture.middlewareConfiguration, _fixture.queueATConfiguration, _fixture.logger);

            var request = _fixture.CreateReceiptRequest("CashSale.json");
            var queueItem = _fixture.CreateQueueItem(request);
            var response = RequestCommand.CreateReceiptResponse(request, queueItem, _fixture.queueAT, _fixture.queue);

            var result = await _initialOperationReceiptCommand.ExecuteAsync(_fixture.queue, _fixture.queueAT, request, queueItem, response);

            _fixture.TestCommandResult(request, queueItem, result);
            SignProcessorATFixture.TestAllSignSignatures(result, false, "38.75", false);

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
            ftStateData.ZeroReceipt.Should().BeFalse();

        }

        [Fact]
        public async Task ExecuteAsync_ProtocolReceiptWithoutPayItem_ShouldNotBeSigned()
        {
            await _fixture.CreateConfigurationRepository();
            var _posReceiptCommand = new PosReceiptCommand(_fixture.GetIATSSCDProvider("35"), _fixture.middlewareConfiguration, _fixture.queueATConfiguration, _fixture.logger);

            var request = _fixture.CreateReceiptRequest("ProtocolReceipt.json");
            var queueItem = _fixture.CreateQueueItem(request);
            var response = RequestCommand.CreateReceiptResponse(request, queueItem, _fixture.queueATNotSignAll, _fixture.queue);
            var result = await _posReceiptCommand.ExecuteAsync(_fixture.queue, _fixture.queueATNotSignAll, request, queueItem, response);

            _fixture.TestCommandResult(request, queueItem, result);

            var ftStateData = JsonConvert.DeserializeAnonymousType(result.ReceiptResponse.ftStateData,
                new
                {
                    Exception = string.Empty,
                    Signing = false,
                    Counting = false,
                    ZeroReceipt = false
                });
            ftStateData.Exception.Should().Be("A1");
            ftStateData.Counting.Should().BeFalse();
            ftStateData.Signing.Should().BeFalse();
            ftStateData.ZeroReceipt.Should().BeFalse();
        }


        [Fact]
        public async Task ExecuteAsync_ReceiptNormalSCUFailingWithBackupSCU_ShouldUseBackup()
        {
            await _fixture.CreateConfigurationRepository(addBackupSCU: true);
            var _posReceiptCommand = new PosReceiptCommand(_fixture.GetIATSSCDProvider("35", addBackupSCU: true), _fixture.middlewareConfiguration, _fixture.queueATConfiguration, _fixture.logger);

            var request = _fixture.CreateReceiptRequest("CashSale.json");
            var queueItem = _fixture.CreateQueueItem(request);
            var response = RequestCommand.CreateReceiptResponse(request, queueItem, _fixture.queueAT, _fixture.queue);

            var result = await _posReceiptCommand.ExecuteAsync(_fixture.queue, _fixture.queueAT, request, queueItem, response);

            _fixture.TestCommandResult(request, queueItem, result);
            SignProcessorATFixture.TestAllSignSignatures(result, false, "38.75", false);

            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.Data.Contains("_R1-35backup_"));
            result.ReceiptResponse.ftSignatures.Should().NotContain(x => x.Data.Contains("_R1-35_"));
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
            ftStateData.ZeroReceipt.Should().BeFalse();
        }
    }
}