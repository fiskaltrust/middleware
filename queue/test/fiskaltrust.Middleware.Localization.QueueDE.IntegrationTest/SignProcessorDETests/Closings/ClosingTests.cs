using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Closings
{
    public class ClosingTests
    {
        private readonly SignProcessorDependenciesFixture _fixture;
        public ClosingTests(SignProcessorDependenciesFixture fixture) => _fixture = fixture;
        public async Task ClosingTests_CheckEntryInActionJournalResponseQueueQueueItem(string receiptFolder, string actionJournalMessage)
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", receiptFolder));
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1));
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest).ConfigureAwait(false);
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, actionJournalMessage);
        }
        public async Task ClosingTests_MasterDataChange_CheckAccountOutlet(string receiptFolder, string actionJournalMessage, long receiptCase)
        {
            var masterdata = TestObjectFactory.CreateMasterdata();
            var condfiguration = new Dictionary<string, object>() { { "init_masterData", JsonConvert.SerializeObject(masterdata) } };
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", receiptFolder));
            receiptRequest.ftReceiptCase = receiptCase;
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1), null, condfiguration, true, Array.Empty<ulong>());
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest).ConfigureAwait(false);
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, actionJournalMessage);
            await ReceiptTestResults.IsMasterDataValid(_fixture, masterdata);
        }
    }
}
