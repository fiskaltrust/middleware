using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using fiskaltrust.Middleware.Localization.QueueDE.Models;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class FinishScuSwitchTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly SignProcessorDependenciesFixture _fixture;
        private readonly ReceiptTests _receiptTests;

        public FinishScuSwitchTests(SignProcessorDependenciesFixture fixture)
        {
            _receiptTests = new ReceiptTests(fixture);
            _fixture = fixture;
        }

        [Fact]
        public async Task FinishScuSwitchReceipt_IsNoImplicitFlow_ExpectArgumentException() => await _receiptTests.ExpectArgumentExceptionReceiptcase(
            _receiptTests.GetReceipt("InitiateScuSwitchReceipt", "InitiateScuSwitchNoImplFlow", 0x4445000000000018), "ReceiptCase {0:X} (Finish-SCU-switch receipt) must use implicit-flow flag.").ConfigureAwait(false);
        [Fact]
        public async Task FinishSwitchReceipt_IsTraining_ExpectArgumentException() => await _receiptTests.ExpectArgumentExceptionReceiptcase(
            _receiptTests.GetReceipt("InitiateScuSwitchReceipt", "InitiateScuSwitchNoImplFlow", 0x4445000100020018), "ReceiptCase {0:X} can not use 'TrainingMode' flag.").ConfigureAwait(false);

        [Fact]
        public async Task InitScuSwitchReceipt_QueueDESignaturCreationUnitDEIdIsNotNull_ExpectException()
        {
            var request = new ReceiptRequest() { ftReceiptCase = 0x4445000100000018 };
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = DateTime.Now,
                cbReceiptReference = "IsNoScuSwitchDC",
                cbTerminalID = "369a013a-37e2-4c23-8614-6a8f282e6330",
                country = "DE",
                ftQueueId = _fixture.QUEUEID,
                ftQueueItemId = Guid.NewGuid(),
                ftQueueRow = 10,
                request = JsonConvert.SerializeObject(request),
                requestHash = "test request hash"
            };
            await _fixture.queueItemRepository.InsertOrUpdateAsync(queueItem);
            await _receiptTests.ExpectException(
                _receiptTests.GetReceipt("InitiateScuSwitchReceipt", "InitiateScuSwitchNoImplFlow", 0x4445000100000018), $"The SCU switch must be initiated with a initiate-scu-switch receipt. See https://link.fiskaltrust.cloud/market-de/scu-switch for more details.").ConfigureAwait(false);
        }

        [Fact]
        public async Task InitScuSwitchReceipt_LastActionJournalDataJsonIsNull_ExpectException()
        {
            var request = new ReceiptRequest() { ftReceiptCase = 0x4445000100000018 };
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = DateTime.Now,
                cbReceiptReference = "IniScuDataJsonIsNull",
                cbTerminalID = "369a013a-37e2-4c23-8614-6a8f282e6330",
                country = "DE",
                ftQueueId = _fixture.QUEUEID,
                ftQueueItemId = Guid.NewGuid(),
                ftQueueRow = 10,
                request = JsonConvert.SerializeObject(request),
                requestHash = "test request hash"
            };
            await _fixture.queueItemRepository.InsertOrUpdateAsync(queueItem);
            await _fixture.actionJournalRepository.InsertAsync(CreateftActionJournal($"{0x4445000000000003:X}-{nameof(InitiateSCUSwitch)}", _fixture.QUEUEID, queueItem.ftQueueItemId, string.Empty));
            await _receiptTests.ExpectException(
                _receiptTests.GetReceipt("InitiateScuSwitchReceipt", "InitiateScuSwitchNoImplFlow", 0x4445000100000018), $"The SCU switch must be initiated with a initiate-scu-switch receipt. See https://link.fiskaltrust.cloud/market-de/scu-switch for more details.", true, true).ConfigureAwait(false);
        }

        [Fact]
        public async Task InitScuSwitchReceipt_ValidRewuest_ValidResult()
        {
            var request = new ReceiptRequest() { ftReceiptCase = 0x4445000100000018 };
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = DateTime.Now,
                cbReceiptReference = "IniScuDataJsonIsNull",
                cbTerminalID = "369a013a-37e2-4c23-8614-6a8f282e6330",
                country = "DE",
                ftQueueId = _fixture.QUEUEID,
                ftQueueItemId = Guid.NewGuid(),
                ftQueueRow = 10,
                request = JsonConvert.SerializeObject(request),
                requestHash = "test request hash"
            };
            await _fixture.queueItemRepository.InsertOrUpdateAsync(queueItem);
            var initiateSCUSwitchJson = JsonConvert.SerializeObject(_fixture.GetInitiateSCUSwitch());
            var actionJournal = CreateftActionJournal($"{0x4445000000000003:X}-{nameof(InitiateSCUSwitch)}", _fixture.QUEUEID, queueItem.ftQueueItemId, initiateSCUSwitchJson);
            await _fixture.actionJournalRepository.InsertAsync(actionJournal);

            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1), sourceIsScuSwitch: true, targetIsScuSwitch: true, queueDECreationUnitIsNull: true);
            var receiptRequest = _receiptTests.GetReceipt("InitiateScuSwitchReceipt", "InitiateScuSwitchNoImplFlow", 0x4445000100000018);
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);
            var signaturNotification = receiptResponse.ftSignatures.Where(x => x.ftSignatureType.Equals(0x4445000000000003)).LastOrDefault();
            signaturNotification.Should().NotBeNull();
            var finishSCUSwitch = JsonConvert.DeserializeObject<FinishSCUSwitch>(signaturNotification.Data);
            finishSCUSwitch.Should().NotBeNull();
            _fixture.AreTargeAndSourceScusAsGiven(finishSCUSwitch.SourceSCUId, finishSCUSwitch.TargetSCUId).Should().BeTrue();
            var actionMessage = string.Format("Queue-ID: {0}, SCU-ID: {1}", _fixture.QUEUEID, finishSCUSwitch.TargetSCUId);
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, actionMessage).ConfigureAwait(false);
        }

        private ftActionJournal CreateftActionJournal(string type, Guid ftQueueId, Guid ftQueueItemId, string dataJson)
        {
            return new ftActionJournal
            {
                Type = type,
                Moment = DateTime.Now,
                DataJson = dataJson,
                DataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(dataJson)),
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = ftQueueId,
                ftQueueItemId = ftQueueItemId,
                Message = "",
                Priority = 0,
                TimeStamp = DateTime.Now.Ticks
            };
        }
    }
}
