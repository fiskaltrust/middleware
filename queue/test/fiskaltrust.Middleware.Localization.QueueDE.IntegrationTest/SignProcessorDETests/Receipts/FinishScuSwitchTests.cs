using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using fiskaltrust.Middleware.Localization.QueueDE.Models;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class FinishScuSwitchTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly SignProcessorDependenciesFixture _fixture;
        private readonly ReceiptTests _receiptTests;
        private readonly ReceiptProcessorHelper _receiptProcessorHelper;

        public FinishScuSwitchTests(SignProcessorDependenciesFixture fixture)
        {
            _receiptTests = new ReceiptTests(fixture);
            _fixture = fixture;
            _receiptProcessorHelper = new ReceiptProcessorHelper(_fixture.SignProcessor);
        }

        [Fact]
        public async Task FinishScuSwitchReceipt_IsNoImplicitFlow_ExpectErrorState()
        {
            var receiptRequest = _receiptTests.GetReceipt("InitiateScuSwitchReceipt", "InitiateScuSwitchNoImplFlow", 0x4445000000000018);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);
        }

        [Fact]
        public async Task FinishSwitchReceipt_IsTraining_ExpectErrorState()
        {
            var receiptRequest = _receiptTests.GetReceipt("InitiateScuSwitchReceipt", "InitiateScuSwitchNoImplFlow", 0x4445000100020018);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);
        }
        
        [Fact]
        public async Task InitScuSwitchReceipt_QueueDESignaturCreationUnitDEIdIsNotNull_ExpectErrorState()
        {
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = DateTime.Now,
                cbReceiptReference = "IsNoScuSwitchDC",
                cbTerminalID = "369a013a-37e2-4c23-8614-6a8f282e6330",
                country = "DE",
                ftQueueId = _fixture.QUEUEID,
                ftQueueItemId = Guid.NewGuid(),
                ftQueueRow = 10,
                request = "",
                requestHash = "test request hash"
            };
            await _fixture.queueItemRepository.InsertOrUpdateAsync(queueItem);

            var receiptRequest = _receiptTests.GetReceipt("InitiateScuSwitchReceipt", "InitiateScuSwitchNoImplFlow", 0x4445000100000018);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);
            response.ftState.Should().Be(0xEEEE_EEEE);
        }

        [Fact]
        public async Task InitScuSwitchReceipt_LastActionJournalDataJsonIsNull_ExpectException()
        {
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = DateTime.Now,
                cbReceiptReference = "IniScuDataJsonIsNull",
                cbTerminalID = "369a013a-37e2-4c23-8614-6a8f282e6330",
                country = "DE",
                ftQueueId = _fixture.QUEUEID,
                ftQueueItemId = Guid.NewGuid(),
                ftQueueRow = 10,
                request = "",
                requestHash = "test request hash"
            };
            await _fixture.queueItemRepository.InsertOrUpdateAsync(queueItem);
            await _fixture.actionJournalRepository.InsertAsync(CreateftActionJournal(
                $"{0x4445000000000003:X}-{nameof(InitiateSCUSwitch)}", _fixture.QUEUEID, queueItem.ftQueueItemId, string.Empty));
            var receiptRequest = _receiptTests.GetReceipt("InitiateScuSwitchReceipt", "InitiateScuSwitchNoImplFlow", 0x4445000100000018);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);
            response.ftState.Should().Be(0xEEEE_EEEE);
        }

        [Fact]
        public async Task InitScuSwitchReceipt_ValidRewuest_ValidResult()
        {
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = DateTime.Now,
                cbReceiptReference = "IniScuDataJsonIsNull",
                cbTerminalID = "369a013a-37e2-4c23-8614-6a8f282e6330",
                country = "DE",
                ftQueueId = _fixture.QUEUEID,
                ftQueueItemId = Guid.NewGuid(),
                ftQueueRow = 10,
                request = "",
                requestHash = "test request hash"
            };
            await _fixture.queueItemRepository.InsertOrUpdateAsync(queueItem);
            var initiateSCUSwitchJson = JsonConvert.SerializeObject(_fixture.GetInitiateSCUSwitch());
            var actionJournal = CreateftActionJournal($"{0x4445000000000003:X}-{nameof(InitiateSCUSwitch)}", _fixture.QUEUEID, queueItem.ftQueueItemId, initiateSCUSwitchJson);
            await _fixture.actionJournalRepository.InsertAsync(actionJournal);

            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1), null, null, false, false, true, true, true);
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
