using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.RequestCommandsTests
{
    public class YearlyClosingReceiptCommandTest
    {
        [Fact]
        public async Task ExecuteAsync_MonthlyClosing_ValidResultAsync()
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
            var queueME = new ftQueueME()
            {
                ftQueueMEId = queue.ftQueueId,
                ftSignaturCreationUnitMEId = Guid.NewGuid()
            };
            var actionJournalRep = new InMemoryActionJournalRepository();
            var cashDepositReceiptCommand = await TestHelper.InitializeRequestCommand<YearlyClosingReceiptCommand>(queueME, "TestTCRCodePos", actionJournalRep).ConfigureAwait(false);
            var requestResponse = await cashDepositReceiptCommand.ExecuteAsync(new InMemoryMESSCD("TestTCRCodePos"), queue, TestHelper.CreateReceiptRequest(0x44D5_0000_0000_0006), queueItem, queueME);
            await TestHelper.CheckResultActionJournal(queue, queueItem, actionJournalRep, requestResponse, 0x44D5_0000_0000_0006).ConfigureAwait(false);
        }
    }
}
