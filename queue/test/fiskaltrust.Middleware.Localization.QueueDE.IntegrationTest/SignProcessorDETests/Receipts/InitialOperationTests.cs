using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers;
using fiskaltrust.Middleware.Queue.Helpers;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class InitialOperationTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly SignProcessorDependenciesFixture _fixture;
        private readonly ReceiptTests _receiptTests;
        private readonly ReceiptProcessorHelper _receiptProcessorHelper;

        public InitialOperationTests(SignProcessorDependenciesFixture fixture)
        {
            _fixture = fixture;
            _receiptTests = new ReceiptTests(fixture);
            _receiptProcessorHelper = new ReceiptProcessorHelper(_fixture.SignProcessor);
        }

        [Fact]
        public async Task InititalOperation_QueueIsNew_CheckEntryInActionJournalResponseQueueQueueItem()
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "InitialOperation"));
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false);
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest,
                $"In-Betriebnahme-Beleg. Kassenseriennummer: {null}, TSE-Seriennummer: {null}, Queue-ID: {_fixture.QUEUEID}");
        }

        [Fact]
        public async Task InititalOperation_QueueIsDeactivated_ActionJournalEntry()
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "InitialOperation"));
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);

            var actionJournalEntry = _fixture.ActionJournalRepositoryMock.Invocations.FirstOrDefault();
            Assert.NotNull(actionJournalEntry);
        }

        [Fact]
        public async Task InititalOperation_QueueIsActive_ActionJournalEntry()
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "InitialOperation"));
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);

            var actionJournalEntry = _fixture.ActionJournalRepositoryMock.Invocations.FirstOrDefault();
            Assert.NotNull(actionJournalEntry);
        }

        [Fact]
        public async Task InititalOperation_IsNoImplicitFlow_ExpectErrorState()
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "InitialOperation"));
            receiptRequest.ftReceiptCase = 4919338167972134915;

            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);
        }
    }
}