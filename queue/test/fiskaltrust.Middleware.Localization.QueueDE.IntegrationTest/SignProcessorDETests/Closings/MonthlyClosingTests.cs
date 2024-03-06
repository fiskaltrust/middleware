using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Closings
{
    public class MonthlyClosingTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly ClosingTests _closingTests;
        private readonly ReceiptTests _receiptTests;
        private readonly ReceiptProcessorHelper _receiptProcessorHelper;

        public MonthlyClosingTests(SignProcessorDependenciesFixture fixture)
        {
            _closingTests = new ClosingTests(fixture);
            _receiptTests = new ReceiptTests(fixture);
            _receiptProcessorHelper = new ReceiptProcessorHelper(fixture.SignProcessor); 
        }
        
        [Fact]
        public async Task MonthlyClosing_QueueIsActiv_CheckEntryInActionJournalResponseQueueQueueItem() => await _closingTests.ClosingTests_CheckEntryInActionJournalResponseQueueQueueItem("MonthlyClosingReceipt", $"Monthly-Closing receipt was processed.").ConfigureAwait(false);
        [Fact]
        public async Task MonthlyClosing_MasterDataChange_CheckAccountOutlet() => await _closingTests.ClosingTests_MasterDataChange_CheckAccountOutlet("MonthlyClosingReceipt", $"Monthly-Closing receipt was processed, and a master data update was performed.", 0x4445000108000005).ConfigureAwait(false);
        
        [Fact]
        public async Task MonthlyClosing_IsNoImplicitFlow_ExpectErrorState()
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "MonthlyClosingReceipt"));
            receiptRequest.ftReceiptCase = 0x4445000000000005;
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            Assert.Equal(0xEEEE_EEEE, response.ftState);
        }

        [Fact]
        public async Task MonthlyClosing_IsTraining_ExpectArgumentException() => await MonthlyClosing_ExpectArgumentException(0x4445000100020005, "ReceiptCase {0:x} can not use 'TrainingMode' flag.").ConfigureAwait(false);
        private async Task MonthlyClosing_ExpectArgumentException(long receiptCase, string errorMessage)
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "MonthlyClosingReceipt"));
            receiptRequest.ftReceiptCase = receiptCase;
            await _receiptTests.ExpectArgumentExceptionReceiptcase(receiptRequest, errorMessage);
        }
    }
}