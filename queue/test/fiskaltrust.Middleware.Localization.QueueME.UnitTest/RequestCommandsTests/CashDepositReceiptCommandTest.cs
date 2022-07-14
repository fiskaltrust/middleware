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
    public class CashDepositReceiptCommandTest
    {
        [Fact]
        public async Task ExecuteAsync_CashDeposit_ValidResultAsync()
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
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
            };
            var journalMeRepository = new InMemoryJournalMERepository();
            var request = TestHelper.CreateReceiptRequest(0x4D45_0000_0000_0007);
            request.cbChargeItems = new ifPOS.v1.ChargeItem[]
            {
                new ifPOS.v1.ChargeItem
                {
                    ftChargeItemCase = 0x4D45000000000020,
                    Amount = 100.0M
                }
            };

            var sut = await TestHelper.InitializeRequestCommand<CashDepositReceiptCommand>(queueMe, "TestTCRCodePos", journalMeRepository).ConfigureAwait(false);
            await sut.ExecuteAsync(new InMemoryMESSCD("TestTCRCodePos", "iic", "iicSignature"), queue, request, queueItem, queueMe);
            
            var journalME = await journalMeRepository.GetByQueueItemId(queueItem.ftQueueItemId).FirstOrDefaultAsync().ConfigureAwait(false);
            journalME.Should().NotBeNull();
            journalME.JournalType.Should().Be(0x4D45_0000_0000_0007);
            journalME.FCDC.Should().Be("1111");
        }

        [Fact]
        public async Task ExecuteAsync_CashDeposit_ShouldThrowWhenChargeItemsAreMissing()
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
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
            };
            var journalMeRepository = new InMemoryJournalMERepository();
            var request = TestHelper.CreateReceiptRequest(0x4D45_0000_0000_0007);
            request.cbChargeItems = new ifPOS.v1.ChargeItem[]
            {
                new ifPOS.v1.ChargeItem
                {
                    // Chargeitems that are not specific to deposits are ignored
                    ftChargeItemCase = 0x4D45000000000001,
                    Amount = 100.0M
                }
            };

            var sut = await TestHelper.InitializeRequestCommand<CashDepositReceiptCommand>(queueMe, "TestTCRCodePos", journalMeRepository).ConfigureAwait(false);
            sut.Invoking(async x => await x.ExecuteAsync(new InMemoryMESSCD("TestTCRCodePos", "iic", "iicSignature"), queue, request, queueItem, queueMe))
                .Should().Throw<Exception>().WithMessage("An opening-balance receipt was sent that did not include any cash deposit charge items.");
        }
    }
}
