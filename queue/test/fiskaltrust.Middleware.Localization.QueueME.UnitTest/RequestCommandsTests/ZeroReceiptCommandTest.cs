using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.ME;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands.Factories;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Org.BouncyCastle.Asn1.Ocsp;

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.RequestCommandsTests
{
    public class ZeroReceiptCommandTest
    {
        [Fact]
        public async Task ExecuteAsync_CashDepositReceiptCommandAndZeroReceipt_ValidResultAsync()
        {
            var queue = new ftQueue()
            {
                ftQueueId = Guid.NewGuid()
            };
            var scu = new ftSignaturCreationUnitME()
            {
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
                BusinessUnitCode = "cb567zu789",
                SoftwareCode = "ft54ft871",
                MaintainerCode = "ft88fz999",
                ValidFrom = DateTime.UtcNow.AddDays(-50),
                TcrCode = "Testtcr"
            };
            var queueME = new ftQueueME()
            {
                ftQueueMEId = queue.ftQueueId,
                ftSignaturCreationUnitMEId = scu.ftSignaturCreationUnitMEId
            };
            var inMemoryConfigurationRepository = new InMemoryConfigurationRepository();
            await inMemoryConfigurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(scu).ConfigureAwait(false);
            await inMemoryConfigurationRepository.InsertOrUpdateQueueMEAsync(queueME);
            var client = new InMemoryMESSCD("TestZeroTCRCode", "iic", "iicSignature", true);
            var actionJournalRepository = new InMemoryActionJournalRepository();
            var journalMERepository = new InMemoryJournalMERepository();
            var queueItemRepository = new InMemoryQueueItemRepository();
            var queueItem = CreateQueueItem(queue);
            var cdReceipRequest = TestHelper.CreateReceiptRequest(0x4D45_0000_0000_0007);
            cdReceipRequest.cbChargeItems = new ifPOS.v1.ChargeItem[]
            {
                new ifPOS.v1.ChargeItem
                {
                    ftChargeItemCase = 0x4D45000000000020,
                    Amount = 100.0M
                }
            };
            queueItem.request = JsonConvert.SerializeObject(cdReceipRequest);
            await queueItemRepository.InsertAsync(queueItem).ConfigureAwait(false);
            var cashDepositReceiptCommand = new CashDepositReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), inMemoryConfigurationRepository, journalMERepository, queueItemRepository, actionJournalRepository, new QueueMEConfiguration { Sandbox = true });
            await cashDepositReceiptCommand.ExecuteAsync(new InMemoryMESSCD("TestZeroTCRCode", "iic", "iicSignature"), queue, cdReceipRequest, queueItem, queueME).ConfigureAwait(false);



            var sut = new ZeroReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), inMemoryConfigurationRepository, journalMERepository, queueItemRepository, actionJournalRepository, CreateRequestCommandFactory(), new QueueMEConfiguration { Sandbox = true });
            var respond = await sut.ExecuteAsync(client, queue, cdReceipRequest, queueItem, queueME);
        }

        private IRequestCommandFactory CreateRequestCommandFactory()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder =>
            {
                builder.AddSerilog(dispose: true);
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            var sut = new QueueMeBootstrapper();
            sut.ConfigureServices(serviceCollection);
            return new RequestCommandFactory(serviceCollection.BuildServiceProvider());
        }

        private ftQueueItem CreateQueueItem(ftQueue queue)
        {
            return new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftWorkMoment = DateTime.Now
            };
        }
    }
}
