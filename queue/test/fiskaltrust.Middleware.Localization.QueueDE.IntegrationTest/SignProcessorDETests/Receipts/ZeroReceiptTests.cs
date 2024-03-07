using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Localization.QueueDE.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class ZeroReceiptTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly SignProcessorDependenciesFixture _fixture;
        private readonly ReceiptTests _receiptTests;
        private readonly ReceiptProcessorHelper _receiptProcessorHelper;

        public ZeroReceiptTests(SignProcessorDependenciesFixture fixture)
        {
            _fixture = fixture;
            _receiptTests = new ReceiptTests(fixture);
            _receiptProcessorHelper = new ReceiptProcessorHelper(_fixture.SignProcessor);
        }

        [Fact]
        public async Task SignProcessor_ZeroReceipt_WithTseInfoFlag_ShouldReturnValidResponse()
        {
            var (receiptRequest, expectedResponse, queueItem) = GetReceipt(Path.Combine("Data", "ZeroReceipt", "TseInfo"), "ZeroReceiptWithTseInfoFlag");
            var queue = new ftQueue { ftQueueId = Guid.Parse(receiptRequest.ftQueueID), StartMoment = DateTime.UtcNow };
            var sut = GetSUT();

            var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, queue, queueItem);

            receiptResponse.Should().BeEquivalentTo(expectedResponse, x => x
                .Excluding(x => x.ftReceiptMoment)
                .Excluding(x => x.ftReceiptIdentification)
                .Excluding(x => x.ftSignatures));
            receiptResponse.ftSignatures.Length.Should().Be(expectedResponse.ftSignatures.Length);
        }

        [Fact]
        public async Task SignProcessor_ZeroReceipt_WithSelfTestFlag_ShouldExecuteSelfTest()
        {
            var (receiptRequest, _, queueItem) = GetReceipt(Path.Combine("Data", "ZeroReceipt", "SelfTest"), "ZeroReceiptWithSelfTestFlag");
            var queue = new ftQueue { ftQueueId = Guid.Parse(receiptRequest.ftQueueID), StartMoment = DateTime.UtcNow };
            var sut = GetSUT();

            var timestampBeforeProcessing = DateTime.UtcNow;
            await sut.ProcessAsync(receiptRequest, queue, queueItem);

            _fixture.InMemorySCU.LastSelfTest.Should().BeOnOrAfter(timestampBeforeProcessing);
        }

        [Fact]
        public async Task SignProcessor_ZeroReceipt_WithExportFlag_ShouldPerformTarExportAndEraseTSE()
        {
            var (receiptRequest, _, queueItem) = GetReceipt(Path.Combine("Data", "ZeroReceipt", "TarExport"), "ZeroReceiptWithExportFlag");
            var queue = new ftQueue { ftQueueId = Guid.Parse(receiptRequest.ftQueueID), StartMoment = DateTime.UtcNow };

            var actionJournalRepositoryMock = new Mock<IActionJournalRepository>(MockBehavior.Strict);
            var journalRepository = new InMemoryJournalDERepository();
            var config = new MiddlewareConfiguration { Configuration = new Dictionary<string, object>(), ServiceFolder = Path.GetTempPath() };

            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(Mock.Of<ILogger<SignProcessorDE>>(), _fixture.CreateConfigurationRepository(), journalRepository,
                actionJournalRepositoryMock.Object, _fixture.DeSSCDProvider, new DSFinVKTransactionPayloadFactory(Mock.Of<ILogger<DSFinVKTransactionPayloadFactory>>()), new InMemoryFailedFinishTransactionRepository(),
                new InMemoryFailedStartTransactionRepository(), new InMemoryOpenTransactionRepository(), Mock.Of<IMasterDataService>(), config,
                new InMemoryQueueItemRepository(), new SignatureFactoryDE(QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config)));

            var timestampBeforeProcessing = DateTime.UtcNow;
            await sut.ProcessAsync(receiptRequest, queue, queueItem);

            _fixture.InMemorySCU.LastErase.Should().BeOnOrAfter(timestampBeforeProcessing);
            var journals = await journalRepository.GetAsync();
            journals.Should().HaveCount(1);
            journals.First().Number.Should().Be(1);
            journals.First().FileContentBase64.Should().NotBeNull();
            journals.First().FileName.Should().NotBeNull();
            journals.First().FileExtension.Should().Be(".zip");
            journals.First().ftQueueItemId.Should().Be(queueItem.ftQueueItemId);
            journals.First().ftQueueId.Should().Be(queueItem.ftQueueId);

        }
        [Fact]
        public async Task SignProcessorZeroReceipt_NoImplicitFlow_ExpectErrorState()
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "ZeroReceipt", "SelfTest"));
            receiptRequest.cbReceiptReference = "ZeroReceiptNoImplicitFlow";
            receiptRequest.ftReceiptCase = 0x4445000000000002;

            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            Assert.Equal(0xEEEE_EEEE, response.ftState);
        }

        [Fact]
        public async Task SignProcessorZeroReceipt_IsTseInfoRequest_RespondeStates()
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "ZeroReceipt", "SelfTest"));
            receiptRequest.cbReceiptReference = "IsTseInfoRequest";
            receiptRequest.ftReceiptCase = 0x4445000100800002;
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(true, DateTime.Now.AddHours(-1));
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest).ConfigureAwait(false);
            receiptResponse.ftStateData.Should().Contain("TseInfo");
        }

        [Fact]
        public async Task SignProcessorZeroReceipt_QueueFailedModeEraseOpenTransNotOnTse_ValidOutput()
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "ZeroReceipt", "SelfTest"));
            receiptRequest.cbReceiptReference = "R101NotonTse";
            receiptRequest.ftReceiptCase = 0x4445000121000002;
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(true, DateTime.Now.AddHours(-1), null, null, false, true, true);
            var queueItemId = Guid.NewGuid();
            await _fixture.failedFinishTransactionRepository.InsertOrUpdateTransactionAsync(new FailedFinishTransaction
            {
                cbReceiptReference = receiptRequest.cbReceiptReference,
                CashBoxIdentification = _fixture.CASHBOXIDENTIFICATION.ToString(),
                ftQueueItemId = queueItemId,
                TransactionNumber = 77,
                FinishMoment = DateTime.UtcNow,
                Request = JsonConvert.SerializeObject(receiptRequest)
            }).ConfigureAwait(false);
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest).ConfigureAwait(false);
            _fixture.failedFinishTransactionRepository.GetAsync().Result.Should().BeEmpty();
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, "Removed failed finish-transaction 77 from the database, which was not open on the TSE anymore.");
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, "TSE Kommunikation wiederhergestellt am");
            await CheckQueueLeftFailedModeAsync().ConfigureAwait(false);
        }

        /*
        [Fact]
        public async Task SignProcessorZeroReceipt_QueueFailedMode_ValidOutput()
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "ZeroReceipt", "SelfTest"));
            receiptRequest.cbReceiptReference = "ZeroReceiptQueueFailedMode";
            receiptRequest.ftReceiptCase = 0x4445000101010002;
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(true, DateTime.Now.AddHours(-1), null);
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest).ConfigureAwait(false);
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, string.Empty, 0x4445000000000008, null, false );
            ;
            
            receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "ZeroReceipt", "SelfTest"));
            await _fixture.failedFinishTransactionRepository.InsertOrUpdateTransactionAsync(new FailedFinishTransaction
            {
                cbReceiptReference = receiptRequest.cbReceiptReference,
                CashBoxIdentification = _fixture.CASHBOXIDENTIFICATION.ToString(),
                ftQueueItemId = Guid.NewGuid(),
                TransactionNumber = 78,
                FinishMoment = DateTime.UtcNow,
                Request = JsonConvert.SerializeObject(receiptRequest)
            }).ConfigureAwait(false);
            await _fixture.failedStartTransactionRepository.InsertOrUpdateTransactionAsync(new FailedStartTransaction
            {
                cbReceiptReference = receiptRequest.cbReceiptReference,
                CashBoxIdentification = _fixture.CASHBOXIDENTIFICATION.ToString(),
                ftQueueItemId = Guid.NewGuid(),
                StartMoment = DateTime.UtcNow.AddMinutes(-1),
                Request = JsonConvert.SerializeObject(receiptRequest)                
            }).ConfigureAwait(false);
            receiptResponse = await signProcessor.ProcessAsync(receiptRequest).ConfigureAwait(false);
            _fixture.failedFinishTransactionRepository.GetAsync().Result.Should().BeEmpty();
            _fixture.failedStartTransactionRepository.GetAsync().Result.Should().BeEmpty();
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, "TSE Kommunikation wiederhergestellt am");
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, "Ausfallsnacherfassung abgeschlossen am");
            await CheckQueueLeftFailedModeAsync().ConfigureAwait(false);
      }*/

        private (ReceiptRequest receiptRequest, ReceiptResponse expectedResponse, ftQueueItem queueItem) GetReceipt(string jsonDirectory, string receiptReference)
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(File.ReadAllText(Path.Combine(jsonDirectory, "Request.json")));
            receiptRequest.cbReceiptReference = receiptReference;
            var responsePath = Path.Combine(jsonDirectory, "Response.json");
            var expectedResponse = File.Exists(responsePath) ? JsonConvert.DeserializeObject<ReceiptResponse>(File.ReadAllText(responsePath)) : null;
            if(expectedResponse != null)
            {
                expectedResponse.cbReceiptReference = receiptReference;
            }
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = receiptRequest.cbReceiptMoment,
                cbReceiptReference = receiptRequest.cbReceiptReference,
                cbTerminalID = receiptRequest.cbTerminalID,
                country = "DE",
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                ftQueueItemId = expectedResponse != null ? Guid.Parse(expectedResponse.ftQueueItemID) : Guid.Empty,
                ftQueueRow = expectedResponse?.ftQueueRow ?? 0,
                request = JsonConvert.SerializeObject(receiptRequest),
                requestHash = "test request hash"
            };
            return (receiptRequest, expectedResponse, queueItem);
        }

        private async Task CheckQueueLeftFailedModeAsync()
        {
            var queueDE = await _fixture.configurationRepository.GetQueueDEAsync(_fixture.QUEUEID).ConfigureAwait(false);
            queueDE.UsedFailedCount.Should().Be(0);
            queueDE.UsedFailedMomentMin.Should().BeNull();
            queueDE.UsedFailedMomentMax.Should().BeNull();
            queueDE.UsedFailedQueueItemId.Should().BeNull();
        }

        private SignProcessorDE GetSUT()
        {
            var journalRepositoryMock = new Mock<IMiddlewareJournalDERepository>(MockBehavior.Strict);
            _fixture.InMemorySCU.OpenTans = false;
            var actionJournalRepositoryMock = new Mock<IActionJournalRepository>(MockBehavior.Strict);
            var config = new MiddlewareConfiguration { Configuration = new Dictionary<string, object>() };

            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(Mock.Of<ILogger<SignProcessorDE>>(), _fixture.CreateConfigurationRepository(), journalRepositoryMock.Object,
                actionJournalRepositoryMock.Object, _fixture.DeSSCDProvider, new DSFinVKTransactionPayloadFactory(Mock.Of<ILogger<DSFinVKTransactionPayloadFactory>>()), new InMemoryFailedFinishTransactionRepository(),
                new InMemoryFailedStartTransactionRepository(), new InMemoryOpenTransactionRepository(), Mock.Of<IMasterDataService>(), config,
                new InMemoryQueueItemRepository(), new SignatureFactoryDE(QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config)));

            return sut;
        }
    }
}
