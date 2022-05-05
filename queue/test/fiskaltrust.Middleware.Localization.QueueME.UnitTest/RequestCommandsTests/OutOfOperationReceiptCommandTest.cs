using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.ifPOS.v1;
using Xunit;
using FluentAssertions;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using Moq;
using fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE.MasterData;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.ME;

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.RequestCommandsTests
{
    public class OutOfOperationReceiptCommandTest
    {

        [Fact]
        public async Task ExecuteAsync_SetValidTo_ValidResultAsync()
        {
            var tcr = CreateTCR();
            var inMemoryConfigurationRepository = new InMemoryConfigurationRepository();
            var testTcr = "TestTCRCodeOoO";
            var queue = new ftQueue()
            {
                ftQueueId = Guid.NewGuid()
            };
            await InsertQueueSCU(tcr, inMemoryConfigurationRepository, testTcr, queue.ftQueueId);
 
            tcr.ValidTo = DateTime.Now.Date;
            var receiptRequest = CreateReceiptRequest(tcr);
            var inMemoryJournalMERepository = new InMemoryJournalMERepository();
            var inMemoryQueueItemRepository = new InMemoryQueueItemRepository();
            var outOfOperationReceiptCommand = new OutOfOperationReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), new SignatureFactoryME(), inMemoryConfigurationRepository, inMemoryJournalMERepository, inMemoryQueueItemRepository);
            var inMemoryMESSCD = new InMemoryMESSCD(testTcr);
            await outOfOperationReceiptCommand.ExecuteAsync(inMemoryMESSCD, queue, receiptRequest, new ftQueueItem()).ConfigureAwait(false);

            var queuMe = await inMemoryConfigurationRepository.GetQueueMEAsync(queue.ftQueueId).ConfigureAwait(false);
            queuMe.Should().NotBeNull();
            queuMe.ftSignaturCreationUnitMEId.HasValue.Should().BeTrue();

            var signaturCreationUnitME = await inMemoryConfigurationRepository.GetSignaturCreationUnitMEAsync(queuMe.ftSignaturCreationUnitMEId.Value).ConfigureAwait(false);
            signaturCreationUnitME.Should().NotBeNull();
            signaturCreationUnitME.IssuerTin.Should().Equals(tcr.IssuerTin);
            signaturCreationUnitME.BusinessUnitCode.Should().Equals(tcr.BusinessUnitCode);
            signaturCreationUnitME.TcrIntId.Should().Equals(tcr.TcrIntId);
            signaturCreationUnitME.TcrCode.Should().Equals(testTcr);

        }
        private Tcr CreateTCR()
        {
            return new Tcr()
            {
                BusinessUnitCode = "aT007FT885",
                IssuerTin = "02657594",
                TcrIntId = Guid.NewGuid().ToString(),
                ValidFrom = DateTime.Now.AddDays(-10)
            };
        }

        private ReceiptRequest CreateReceiptRequest(Tcr tcr)
        {
            var tcrJson = JsonConvert.SerializeObject(tcr);
            return new ReceiptRequest
            {
                ftReceiptCase = 0x44D5_0000_0000_0004,
                cbReceiptReference = "OutOfOperation",
                ftCashBoxID = Guid.NewGuid().ToString(),
                cbReceiptMoment = DateTime.Now,
                cbUser = "Admin",
                ftReceiptCaseData = tcrJson,
                cbTerminalID = "TCRIntID_1"
            };
        }

        private async Task InsertQueueSCU(Tcr tcr, IConfigurationRepository configurationRepository, string testTcr, Guid ftQueueId)
        {

            var signaturCreationUnitME = new ftSignaturCreationUnitME()
            {
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
                TimeStamp = DateTime.Now.AddDays(-10).Ticks,
                TcrIntId = tcr.TcrIntId,
                BusinessUnitCode = tcr.BusinessUnitCode,
                IssuerTin = tcr.IssuerTin,
                TcrCode = testTcr,
                ValidFrom = tcr.ValidFrom,
                ValidTo = tcr.ValidTo
            };
            await configurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(signaturCreationUnitME).ConfigureAwait(false);

            var queueME = new ftQueueME()
            {
                ftQueueMEId = ftQueueId,
                ftSignaturCreationUnitMEId = signaturCreationUnitME.ftSignaturCreationUnitMEId

            };
            await configurationRepository.InsertOrUpdateQueueMEAsync(queueME).ConfigureAwait(false);
        }
    }
}
