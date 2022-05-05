using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using FluentAssertions;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.ME;

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.RequestCommandsTests
{
    public class InitialOperationReceiptCommandTests
    {

        [Fact]
        public async Task ExecuteAsync_RegisterENU_ValidResultAsync()
        {
            var tcr = CreateTCR();
            var receiptRequest = CreateReceiptRequest(tcr);

            var inMemoryConfigurationRepository = new InMemoryConfigurationRepository();
            var inMemoryJournalMERepository = new InMemoryJournalMERepository();
            var inMemoryQueueItemRepository = new InMemoryQueueItemRepository();
            var initialOperationReceiptCommand = new InitialOperationReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), new SignatureFactoryME(), inMemoryConfigurationRepository, inMemoryJournalMERepository, inMemoryQueueItemRepository);

            var testTcr = "TestTCRCode";
            var inMemoryMESSCD = new InMemoryMESSCD(testTcr);
            var queue = new ftQueue()
            {
                ftQueueId = Guid.NewGuid()
            };
            await initialOperationReceiptCommand.ExecuteAsync(inMemoryMESSCD, queue, receiptRequest, new ftQueueItem()).ConfigureAwait(false);

            var queuMe = await inMemoryConfigurationRepository.GetQueueMEAsync(queue.ftQueueId).ConfigureAwait(false);
            queuMe.Should().NotBeNull();
            queuMe.ftSignaturCreationUnitMEId.HasValue.Should().BeTrue();

            var signaturCreationUnitME = await inMemoryConfigurationRepository.GetSignaturCreationUnitMEAsync(queuMe.ftSignaturCreationUnitMEId.Value).ConfigureAwait(false);
            signaturCreationUnitME.IssuerTin.Should().Equals(tcr.IssuerTin);
            signaturCreationUnitME.BusinessUnitCode.Should().Equals(tcr.BusinessUnitCode);
            signaturCreationUnitME.TcrIntId.Should().Equals(tcr.TcrIntId);
            signaturCreationUnitME.TcrCode.Should().Equals(testTcr);
            signaturCreationUnitME.Should().NotBeNull();

        }

        [Fact]
        public async Task ExecuteAsync_RegisterENU_ENUAlreadyRegisteredException()
        {
            var tcr = CreateTCR();
            var receiptRequest = CreateReceiptRequest(tcr);
            var inMemoryConfigurationRepository = new InMemoryConfigurationRepository();
            var scu = new ftSignaturCreationUnitME()
            {
                ftSignaturCreationUnitMEId = Guid.NewGuid(),        
                IssuerTin = tcr.IssuerTin,
                BusinessUnitCode = tcr.BusinessUnitCode,
                TcrIntId = tcr.TcrIntId,
                TcrCode = "TestTcr"
            };
            await inMemoryConfigurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(scu).ConfigureAwait(false);
            var queue = new ftQueue()
            {
                ftQueueId = Guid.NewGuid(),
            };
            await inMemoryConfigurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
            var queueME = new ftQueueME()
            {
                ftQueueMEId = queue.ftQueueId,
                ftSignaturCreationUnitMEId = scu.ftSignaturCreationUnitMEId
            };
            await inMemoryConfigurationRepository.InsertOrUpdateQueueMEAsync(queueME).ConfigureAwait(false);
            var inMemoryJournalMERepository = new InMemoryJournalMERepository();
            var inMemoryQueueItemRepository = new InMemoryQueueItemRepository();
            var InitialOperationReceiptCommand = new InitialOperationReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), new SignatureFactoryME(), inMemoryConfigurationRepository, inMemoryJournalMERepository, inMemoryQueueItemRepository);
            var sutMethod = CallInitialOperationReceiptCommand(InitialOperationReceiptCommand, queue, receiptRequest);
            await sutMethod.Should().ThrowAsync<ENUAlreadyRegisteredException>().ConfigureAwait(false);
        }

        private Func<Task> CallInitialOperationReceiptCommand(InitialOperationReceiptCommand initialOperationReceiptCommand, ftQueue queue, ReceiptRequest receiptRequest)
        {
            return async () => { var receiptResponse = await initialOperationReceiptCommand.ExecuteAsync(new InMemoryMESSCD("testTcr"), queue, receiptRequest, new ftQueueItem()); };
        }

        private Tcr CreateTCR()
        {
            return new Tcr()
            {
                BusinessUnitCode = "aT007FT888",
                IssuerTin = "02657597",
                TcrIntId = Guid.NewGuid().ToString()
            };
        }

        private ReceiptRequest CreateReceiptRequest(Tcr tcr)
        {
            var tcrJson = JsonConvert.SerializeObject(tcr);
            return new ReceiptRequest
            {
                ftReceiptCase = 0x44D5_0000_0000_0003,
                cbReceiptReference = "INIT",
                ftCashBoxID = Guid.NewGuid().ToString(),
                cbReceiptMoment = DateTime.Now,
                cbUser = "Admin",
                ftReceiptCaseData = tcrJson,
                cbTerminalID = "TCRIntID_1"
            };
        }
    }
}
