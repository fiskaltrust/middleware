using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using fiskaltrust.Middleware.Queue.Helpers;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class InitialOperationTests
    {
        private readonly SignProcessorDependenciesFixture _fixture;
        private readonly ReceiptTests _receiptTests;

        public InitialOperationTests()
        {
            _fixture = new SignProcessorDependenciesFixture();
            _receiptTests = new ReceiptTests(_fixture);
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
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-5), DateTime.Now.AddMinutes(-5));
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);
            var actionJournal = await _fixture.actionJournalRepository.GetAsync().ConfigureAwait(false);
            var lastActionEntry = actionJournal.OrderByDescending(x => x.TimeStamp).First();
            lastActionEntry.Message.Should().Be($"Queue {_fixture.QUEUEID} is de-activated, initial-operations-receipt can not be executed.");
        }

        [Fact]
        public async Task InititalOperation_QueueIsActive_ActionJournalEntry()
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "InitialOperation"));
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-5));
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);
            var actionJournal = await _fixture.actionJournalRepository.GetAsync().ConfigureAwait(false);
            var lastActionEntry = actionJournal.OrderByDescending(x => x.TimeStamp).First();
            lastActionEntry.Message.Should().Be($"Queue {_fixture.QUEUEID} is activated, initial-operations-receipt can not be executed.");
        }

        [Fact]
        public async Task InititalOperation_IsNoImplicitFlow_ExpectArgumentException()
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "InitialOperation"));
            receiptRequest.ftReceiptCase = 4919338167972134915;
            await _receiptTests.ExpectArgumentExceptionReceiptcase(receiptRequest, $"ReceiptCase {receiptRequest.ftReceiptCase:X} (initial-operation receipt) must use implicit-flow flag.");
        }
    }
}
