using System.Linq;
using System.Threading.Tasks;
using Xunit;
using fiskaltrust.Middleware.Localization.QueueAT.RequestCommands;
using FluentAssertions;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueAT.UnitTest.RequestCommands
{
    public class OutOfOperationReceiptCommandTest : IClassFixture<SignProcessorATFixture>
    {

        private readonly SignProcessorATFixture _fixture;
        public OutOfOperationReceiptCommandTest(SignProcessorATFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnExpectedResult()
        {
            await _fixture.CreateConfigurationRepository();
            var _outOfOperationReceiptCommand = new OutOfOperationReceiptCommand(_fixture.GetIATSSCDProvider("33"), _fixture.middlewareConfiguration, _fixture.queueATConfiguration, _fixture.logger);

            var request = _fixture.CreateReceiptRequest("OutOfOperation.json");
            var queueItem = _fixture.CreateQueueItem(request);
            var response = RequestCommand.CreateReceiptResponse(request, queueItem, _fixture.queueAT, _fixture.queue);

            var result = await _outOfOperationReceiptCommand.ExecuteAsync(_fixture.queue, _fixture.queueAT, request, queueItem, response);

            result.ReceiptResponse.ftCashBoxID.Should().Be(_fixture.cashBoxId.ToString());
            result.ReceiptResponse.ftQueueID.Should().Be(_fixture.queueId.ToString());
            result.ReceiptResponse.ftQueueItemID.Should().Be(queueItem.ftQueueItemId.ToString());
            result.ReceiptResponse.cbTerminalID.Should().Be(request.cbTerminalID);
            result.ReceiptResponse.cbReceiptReference.Should().Be(request.cbReceiptReference);
            result.ReceiptResponse.ftCashBoxIdentification.Should().Be(request.ftCashBoxID);
            SignProcessorATFixture.TestAllSignSignatures(result);
            var signStopp = result.ReceiptResponse.ftSignatures.ToList().Where(x => x.Caption == "Stopbeleg").FirstOrDefault();
            signStopp.Should().NotBeNull();
            signStopp.Data.Should().Be("Stopbeleg");
            signStopp.ftSignatureType.Should().Be(4707387510509010946);
            var signDA = result.ReceiptResponse.ftSignatures.ToList().Where(x => x.Caption.Contains("De-Aktivierung")).FirstOrDefault();
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