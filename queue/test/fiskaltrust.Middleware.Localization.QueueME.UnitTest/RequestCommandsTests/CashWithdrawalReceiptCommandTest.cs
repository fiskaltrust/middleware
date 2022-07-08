using System;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper;
using fiskaltrust.storage.V0;
using Xunit;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.ME;
using FluentAssertions;

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.RequestCommandsTests
{
    public class CashWithdrawalReceiptCommandTest
    {
        [Fact]
        public async Task ExecuteAsync_CashWithdrawal_ValidResultAsync()
        {
            var queue = new ftQueue
            {
                ftQueueId = Guid.NewGuid()
            };
            var queueItem = new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftWorkMoment = DateTime.Now
            };
            var queueMe = new ftQueueME
            {
                ftQueueMEId = queue.ftQueueId,
                ftSignaturCreationUnitMEId = Guid.NewGuid()
            };
            var journalMeRepository = new InMemoryJournalMERepository();
            var cashDepositReceiptCommand = await TestHelper.InitializeRequestCommand<CashWithdrawalReceiptCommand>(queueMe, "TestTCRCodePos", journalMeRepository).ConfigureAwait(false);
            var requestResponse = await cashDepositReceiptCommand.ExecuteAsync(new InMemoryMESSCD("TestTCRCodePos", "iic", "iicSignature"), queue, TestHelper.CreateReceiptRequest(0x44D5_0000_0000_0008), queueItem, queueMe);
            requestResponse.ActionJournals.Should().HaveCount(1);
            requestResponse.ActionJournals.FirstOrDefault()?.ftQueueItemId.Should().Be(queueItem.ftQueueItemId);
            requestResponse.ActionJournals.FirstOrDefault()?.Type.Should().Be(0x44D5_0000_0000_0008.ToString());
        }
    }
}
