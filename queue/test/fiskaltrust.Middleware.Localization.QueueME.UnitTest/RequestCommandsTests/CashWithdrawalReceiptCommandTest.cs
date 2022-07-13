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
            var request = TestHelper.CreateReceiptRequest(0x44D5_0000_0000_0008);
            request.cbChargeItems = new ifPOS.v1.ChargeItem[]
            {
                new ifPOS.v1.ChargeItem
                {
                    ftChargeItemCase = 0x4D45000000000021,
                    Amount = 100.0M
                }
            };

            var sut = await TestHelper.InitializeRequestCommand<CashWithdrawalReceiptCommand>(queueMe, "TestTCRCodePos", journalMeRepository).ConfigureAwait(false);
            var requestResponse = await sut.ExecuteAsync(new InMemoryMESSCD("TestTCRCodePos", "iic", "iicSignature"), queue, request, queueItem, queueMe);
            
            requestResponse.ActionJournals.Should().HaveCount(1);
            requestResponse.ActionJournals.FirstOrDefault()?.ftQueueItemId.Should().Be(queueItem.ftQueueItemId);
            requestResponse.ActionJournals.FirstOrDefault()?.Type.Should().Be(0x44D5_0000_0000_0008.ToString("x").ToUpper());
        }

        [Fact]
        public async Task ExecuteAsync_CashWithdrawal_ShouldThrowWhenChargeItemsAreMissing()
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
            var request = TestHelper.CreateReceiptRequest(0x44D5_0000_0000_0008);
            request.cbChargeItems = new ifPOS.v1.ChargeItem[]
            {
                new ifPOS.v1.ChargeItem
                {
                    // Chargeitems that are not specific to withdrawals are ignored
                    ftChargeItemCase = 0x4D45000000000001,
                    Amount = 100.0M
                }
            };

            var sut = await TestHelper.InitializeRequestCommand<CashWithdrawalReceiptCommand>(queueMe, "TestTCRCodePos", journalMeRepository).ConfigureAwait(false);
            sut.Invoking(async x => await x.ExecuteAsync(new InMemoryMESSCD("TestTCRCodePos", "iic", "iicSignature"), queue, request, queueItem, queueMe))
.Should().Throw<Exception>().WithMessage("An cash-withdrawal receipt was sent that did not include any cash withdrawal charge items.");
        }
    }
}
