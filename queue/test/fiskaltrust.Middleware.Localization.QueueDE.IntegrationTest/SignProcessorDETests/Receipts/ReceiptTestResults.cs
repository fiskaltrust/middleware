using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Queue.Helpers;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class ReceiptTestResults
    {
        public static async Task IsResponseValidAsync(SignProcessorDependenciesFixture signProcessorDependenciesFixture, ReceiptResponse receiptResponse, ReceiptRequest receiptRequest, string actionJournalMessage, long responseState = 0x4445000000000000, ulong? transactionNumber = 0, bool checkReceiptId = true)
        {
            if (actionJournalMessage != string.Empty)
            {
                var actionJournals = await signProcessorDependenciesFixture.actionJournalRepository.GetAsync().ConfigureAwait(false);
                var actionJournalEntry = actionJournals.Where(x => x.Message.Contains(actionJournalMessage));
                actionJournalEntry.Should().NotBeNull();
            }
            var queueItems = await signProcessorDependenciesFixture.queueItemRepository.GetAsync().ConfigureAwait(false);
            var lastQueueItem = queueItems.Where(x => x.ftQueueItemId.ToString() == receiptResponse.ftQueueItemID).First();
            var queue = await signProcessorDependenciesFixture.configurationRepository.GetQueueAsync(signProcessorDependenciesFixture.QUEUEID).ConfigureAwait(false);
            var queueDE = await signProcessorDependenciesFixture.configurationRepository.GetQueueDEAsync(signProcessorDependenciesFixture.QUEUEID).ConfigureAwait(false);
            receiptResponse.ftCashBoxID.Should().Be(signProcessorDependenciesFixture.CASHBOXID.ToString());
            receiptResponse.ftCashBoxID.Should().Be(queue.ftCashBoxId.ToString());
            receiptResponse.ftCashBoxIdentification.Should().Be(signProcessorDependenciesFixture.CASHBOXIDENTIFICATION.ToString());
            receiptResponse.ftCashBoxIdentification.Should().Be(queueDE.CashBoxIdentification.ToString());
            receiptResponse.cbTerminalID.Should().Be(SignProcessorDependenciesFixture.terminalID);
            receiptResponse.cbReceiptReference.Should().Be(receiptRequest.cbReceiptReference);
            var cryptoHelper = new CryptoHelper();
            cryptoHelper.GenerateBase64Hash(JsonConvert.SerializeObject(receiptResponse)).Should().Be(lastQueueItem.responseHash);
            cryptoHelper.GenerateBase64Hash(JsonConvert.SerializeObject(receiptRequest)).Should().Be(lastQueueItem.requestHash);
            receiptResponse.ftState.Should().Be(responseState);
            if (checkReceiptId)
            {
                var receiptIdentification = receiptRequest.GetReceiptIdentification(queue.ftReceiptNumerator - 1, transactionNumber);
                receiptResponse.ftReceiptIdentification.Should().Be(receiptIdentification);
            }
        }

        public static async Task IsMasterDataValid(SignProcessorDependenciesFixture testMiddlewareContainer, MasterDataConfiguration masterdata)
        {
            var account = await testMiddlewareContainer.accountMasterDataRepository.GetAsync().ConfigureAwait(false);
            account.ToList().Count.Should().Be(1);
            account.ToList()[0].Should().BeEquivalentTo(masterdata.Account);
            var outlet = await testMiddlewareContainer.outletMasterDataRepository.GetAsync().ConfigureAwait(false);
            outlet.ToList().Count.Should().Be(1);
            outlet.Should().BeEquivalentTo(masterdata.Outlet);
            var agency = await testMiddlewareContainer.agencyMasterDataRepository.GetAsync().ConfigureAwait(false);
            agency.Should().BeEquivalentTo(masterdata.Agencies);
            var possys = await testMiddlewareContainer.posSystemMasterDataRepository.GetAsync().ConfigureAwait(false);
            possys.Should().BeEquivalentTo(masterdata.PosSystems);
        }
    }
}
