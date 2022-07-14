using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v1;
using Xunit;
using FluentAssertions;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using Moq;
using fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.ME;

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.RequestCommandsTests
{
    public class OutOfOperationReceiptCommandTest
    {

        [Fact]
        public async Task ExecuteAsync_SetValidTo_ValidResultAsync()
        {
            var receiptRequest = CreateReceiptRequest();

            var tcrCode = "TestTCRCode";
            var iic = "iic";
            var iicSignature = "iicSignature";
            var businessUnitCode = "aT007FT888";
            var issuerTin = "02657597";
            var validFrom = DateTime.UtcNow;

            var queue = new ftQueue()
            {
                ftQueueId = Guid.NewGuid()
            };
            var queueME = new ftQueueME()
            {
                ftQueueMEId = queue.ftQueueId,
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
            };
            var inMemoryConfigurationRepository = new InMemoryConfigurationRepository();
            await InsertQueueMESCU(inMemoryConfigurationRepository, queueME, tcrCode, queue.ftQueueId.ToString(), businessUnitCode, issuerTin, validFrom);
 
            var inMemoryJournalMERepository = new InMemoryJournalMERepository();
            var inMemoryQueueItemRepository = new InMemoryQueueItemRepository();
            var queueMeConfig = new QueueMEConfiguration { Sandbox = true };
            
            var inMemoryMESSCD = new InMemoryMESSCD(tcrCode, iic, iicSignature);

            var sut = new OutOfOperationReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), inMemoryConfigurationRepository, 
                inMemoryJournalMERepository, inMemoryQueueItemRepository, new InMemoryActionJournalRepository(), queueMeConfig, new Factories.SignatureItemFactory(queueMeConfig));
            await sut.ExecuteAsync(inMemoryMESSCD, queue, receiptRequest, new ftQueueItem(), queueME);

            queue.StopMoment.Should().NotBeNull();

            var scuMeAfterTest = await inMemoryConfigurationRepository.GetSignaturCreationUnitMEAsync(queueME.ftSignaturCreationUnitMEId.Value);
            scuMeAfterTest.ValidTo.Should().NotBeNull();

        }
        private ReceiptRequest CreateReceiptRequest()
        {
            return new ReceiptRequest
            {
                ftReceiptCase = 0x4D45_0000_0000_0004,
                cbReceiptReference = "OutOfOperation",
                ftCashBoxID = Guid.NewGuid().ToString(),
                cbReceiptMoment = DateTime.Now,
                cbUser = "Admin",
                cbTerminalID = "TCRIntID_1"
            };
        }

        private async Task InsertQueueMESCU(IConfigurationRepository configurationRepository, ftQueueME queueME, string tcrCode, string tcrIntId, string businessUnitCode, string issuerTin, DateTime validFrom)
        {
            var signaturCreationUnitME = new ftSignaturCreationUnitME()
            {
                ftSignaturCreationUnitMEId = queueME.ftSignaturCreationUnitMEId.Value,
                TimeStamp = DateTime.Now.AddDays(-10).Ticks,
                TcrIntId = tcrIntId,
                BusinessUnitCode = businessUnitCode,
                IssuerTin = issuerTin,
                TcrCode = tcrCode,
                ValidFrom = validFrom,
            };
            await configurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(signaturCreationUnitME);
            await configurationRepository.InsertOrUpdateQueueMEAsync(queueME);
        }
    }
}
