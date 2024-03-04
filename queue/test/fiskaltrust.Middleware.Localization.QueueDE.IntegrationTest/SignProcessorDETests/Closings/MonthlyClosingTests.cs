using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Closings
{
    public class MonthlyClosingTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly ClosingTests _closingTests;
        private readonly ReceiptTests _receiptTests;

        public MonthlyClosingTests(SignProcessorDependenciesFixture fixture)
        {
            _closingTests = new ClosingTests(fixture);
            _receiptTests = new ReceiptTests(fixture);
        }
        [Fact]
        public async Task MonthlyClosing_QueueIsActiv_CheckEntryInActionJournalResponseQueueQueueItem() => await _closingTests.ClosingTests_CheckEntryInActionJournalResponseQueueQueueItem("MonthlyClosingReceipt", $"Monthly-Closing receipt was processed.").ConfigureAwait(false);
        [Fact]
        public async Task MonthlyClosing_MasterDataChange_CheckAccountOutlet() => await _closingTests.ClosingTests_MasterDataChange_CheckAccountOutlet("MonthlyClosingReceipt", $"Monthly-Closing receipt was processed, and a master data update was performed.", 0x4445000108000005).ConfigureAwait(false);
        [Fact]
        public async Task MonthlyClosing_IsNoImplicitFlow_ExpectArgumentException() => await MonthlClosing_ExpectArgumentException(0x4445000000000005, "ReceiptCase {0:x} (Monthly-closing receipt) must use implicit-flow flag.").ConfigureAwait(false);
        [Fact]
        public async Task MonthlyClosing_IsTraining_ExpectArgumentException() => await MonthlClosing_ExpectArgumentException(0x4445000100020005, "ReceiptCase {0:x} can not use 'TrainingMode' flag.").ConfigureAwait(false);
        private async Task MonthlClosing_ExpectArgumentException(long receiptCase, string errorMessage)
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "MonthlyClosingReceipt"));
            receiptRequest.ftReceiptCase = receiptCase;
            await _receiptTests.ExpectArgumentExceptionReceiptcase(receiptRequest, errorMessage);
        }
    }
}
