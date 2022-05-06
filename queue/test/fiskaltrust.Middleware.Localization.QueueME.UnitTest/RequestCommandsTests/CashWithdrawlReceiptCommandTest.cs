using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using Xunit;
using fiskaltrust.Middleware.Contracts.Constants;

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.RequestCommandsTests
{
    public class CashWithdrawlReceiptCommandTest
    {
        [Fact]
        public async Task ExecuteAsync_Cashwithdrawl_ValidResultAsync()
        {
            var queue = new ftQueue()
            {
                ftQueueId = Guid.NewGuid()
            };
            var queueItem = new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftWorkMoment = DateTime.Now
            };
            var actionJournalRep = new InMemoryActionJournalRepository();
            var cashDepositReceiptCommand = await TestHelper.InitializeRequestCommand<CashWithdrawlReceiptCommand>(queue.ftQueueId, "TestTCRCodePos", actionJournalRep).ConfigureAwait(false);
            var requestResponse = await cashDepositReceiptCommand.ExecuteAsync(new InMemoryMESSCD("TestTCRCodePos"), queue, TestHelper.CreateReceiptRequest(0x44D5_0000_0000_0008), queueItem);
            await TestHelper.CheckResultActionJournal(queue, queueItem, actionJournalRep, requestResponse, (long)JournalTypes.CashDepositME).ConfigureAwait(false);
        }
    }
}
