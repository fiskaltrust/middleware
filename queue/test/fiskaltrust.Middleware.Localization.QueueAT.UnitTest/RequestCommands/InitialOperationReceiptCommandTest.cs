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

            result.ReceiptResponse.ftCashBoxID.Should().Be(_fixture.cashBoxId.ToString());
            result.ReceiptResponse.ftQueueID.Should().Be(_fixture.queueId.ToString());
            result.ReceiptResponse.ftQueueItemID.Should().Be(queueItem.ftQueueItemId.ToString());
            result.ReceiptResponse.cbTerminalID.Should().Be(request.cbTerminalID);
            result.ReceiptResponse.cbReceiptReference.Should().Be(request.cbReceiptReference);
            result.ReceiptResponse.ftCashBoxIdentification.Should().Be(request.ftCashBoxID);
            TestAllSignSignatures(result);
            var signStopp = result.ReceiptResponse.ftSignatures.ToList().Where(x => x.Caption == "Startbeleg").FirstOrDefault();
            signStopp.Should().NotBeNull();
            signStopp.Data.Should().Be("Startbeleg");
            signStopp.ftSignatureType.Should().Be(4707387510509010946);
            var signDA = result.ReceiptResponse.ftSignatures.ToList().Where(x => x.Caption.Contains("Aktivierung (Inbetriebnahme)")).FirstOrDefault();
            signDA.Should().NotBeNull();
            signDA.Data.Should().Contain("ActionJournalId");
            signDA.ftSignatureType.Should().Be(4707387510509010947);
            //"Caption": "De-Aktivierung (Ausserbetriebnahme) einer Sicherheitseinrichtung ef9764af-1102-41e8-b901-eb89d45cde1b nach RKSV. (Queue)",
            //"Data": "{\"ActionJournalId\":\"5d181535-4334-4dd3-a0e6-0719dea76e78\",\"Type\":\"0x4154000000000003-FonDeactivateQueue\",\"Data\":{\"CashBoxId\":\"6caa852c-4230-4496-83c0-1597eee7084e\",\"QueueId\":\"ef9764af-1102-41e8-b901-eb89d45cde1b\",\"Moment\":\"2024-04-11T10:55:54.2934742Z\",\"CashBoxIdentification\":\"O3yXy0sjA9DLse4AEuhM85M8v92u1+WwuTwJi3Ltl64=\",\"DEPValue\":\"\",\"IsStopReceipt\":true}}"

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

            static void TestAllSignSignatures(Models.RequestCommandResponse result)
            {
                var signAllSig = result.ReceiptResponse.ftSignatures.ToList().Where(x => x.Caption == "Sign-All-Mode").FirstOrDefault();
                signAllSig.Should().NotBeNull();
                signAllSig.Data.Should().Be("Sign: True Counter:True");
                signAllSig.ftSignatureType.Should().Be(4707387510509010944);
                var signZero = result.ReceiptResponse.ftSignatures.ToList().Where(x => x.Caption == "Zero-Receipt").FirstOrDefault();
                signZero.Should().NotBeNull();
                signZero.Data.Should().Be("Sign: True Counter:True");
                signZero.ftSignatureType.Should().Be(4707387510509010944);
                var signCounter = result.ReceiptResponse.ftSignatures.ToList().Where(x => x.Caption == "Counter Add").FirstOrDefault();
                signCounter.Should().NotBeNull();
                signCounter.Data.Should().Be("0");
                signCounter.ftSignatureType.Should().Be(4707387510509010944);
                var signFT = result.ReceiptResponse.ftSignatures.ToList().Where(x => x.Caption == "www.fiskaltrust.at").FirstOrDefault();
                signFT.Should().NotBeNull();
                signFT.ftSignatureType.Should().Be(4707387510509010945);
            }
        }
    }
}