using System;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper;
using fiskaltrust.storage.V0;
using Xunit;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.ME;
using FluentAssertions;

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.RequestCommandsTests
{
    public class CashDepositReceiptCommandTest
    {
        [Fact]
        public async Task ExecuteAsync_CashDeposit_ValidResultAsync()
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
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
            };
            var journalMERepository = new InMemoryJournalMERepository();
            var cashDepositReceiptCommand = await TestHelper.InitializeRequestCommand<CashDepositReceiptCommand>(queueME, "TestTCRCodePos", journalMERepository).ConfigureAwait(false);
            var requestResponse = await cashDepositReceiptCommand.ExecuteAsync(new InMemoryMESSCD("TestTCRCodePos", "iic", "iicSignature"), queue, TestHelper.CreateReceiptRequest(0x44D5_0000_0000_0007), queueItem, queueME);
            var journalME = await journalMERepository.GetByQueueItemId(queueItem.ftQueueItemId).FirstOrDefaultAsync().ConfigureAwait(false);
            journalME.Should().NotBeNull();
            journalME.JournalType.Should().Be(0x44D5_0000_0000_0007);
            journalME.FCDC.Should().Be("1111");
        }
    }
}
