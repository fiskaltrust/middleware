using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Closings
{
    public class YearlyClosingTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly ClosingTests _closingTests;
        private readonly ReceiptTests _receiptTests;
        public YearlyClosingTests(SignProcessorDependenciesFixture fixture)
        {
            _closingTests = new ClosingTests(fixture);
            _receiptTests = new ReceiptTests(fixture);
        }
        [Fact]
        public async Task YearlyClosing_QueueIsActiv_CheckEntryInActionJournalResponseQueueQueueItem() => await _closingTests.ClosingTests_CheckEntryInActionJournalResponseQueueQueueItem("YearlyClosingReceipt", $"Yearly-Closing receipt was processed.").ConfigureAwait(false);
        [Fact]
        public async Task YearlyClosing_MasterDataChange_CheckAccountOutlet() => await _closingTests.ClosingTests_MasterDataChange_CheckAccountOutlet("YearlyClosingReceipt", $"Yearly-Closing receipt was processed, and a master data update was performed.", 4919338172401319942).ConfigureAwait(false);
        [Fact]
        public async Task YearlyClosing_IsNoImplicitFlow_ExpectArgumentException() => await YearlyClosing_ExpectArgumentException(0x4445000000000006, "ReceiptCase {0:X} (Yearly-closing receipt) must use implicit-flow flag.").ConfigureAwait(false);
        [Fact]
        public async Task YearlyClosing_IsTraining_ExpectArgumentException() => await YearlyClosing_ExpectArgumentException(0x4445000100020006, "ReceiptCase {0:X} can not use 'TrainingMode' flag.").ConfigureAwait(false);

        private async Task YearlyClosing_ExpectArgumentException(long receiptCase, string errorMessage)
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", "YearlyClosingReceipt"));
            receiptRequest.ftReceiptCase = receiptCase;
            await _receiptTests.ExpectArgumentExceptionReceiptcase(receiptRequest, errorMessage).ConfigureAwait(false);
        }
    }
}
