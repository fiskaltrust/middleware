using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.ME;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using fiskaltrust.Middleware.Localization.QueueME.Extensions;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands.Factories;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

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
                EnuType = "REGULAR",     
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
            var cdReceipRequest = TestHelper.CreateReceiptRequest(0x44D5_0000_0000_0007);
            queueItem.request = JsonConvert.SerializeObject(cdReceipRequest);
            await queueItemRepository.InsertAsync(queueItem).ConfigureAwait(false);
            var cashDepositReceiptCommand = new CashDepositReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), inMemoryConfigurationRepository, journalMERepository, queueItemRepository, actionJournalRepository, new QueueMEConfiguration { Sandbox = true });
            await cashDepositReceiptCommand.ExecuteAsync(new InMemoryMESSCD("TestZeroTCRCode", "iic", "iicSignature"), queue, cdReceipRequest, queueItem, queueME).ConfigureAwait(false);



            var zeroReceiptCommand = new ZeroReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), inMemoryConfigurationRepository, journalMERepository, queueItemRepository, actionJournalRepository, CreateRequestCommandFactory(), new QueueMEConfiguration { Sandbox = true });
            var respond = await zeroReceiptCommand.ExecuteAsync(client, queue, cdReceipRequest, queueItem, queueME);
        }

        private IRequestCommandFactory CreateRequestCommandFactory()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder =>
            {
                builder.AddSerilog(dispose: true);
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            var sut = new QueueMEBootstrapper();
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
