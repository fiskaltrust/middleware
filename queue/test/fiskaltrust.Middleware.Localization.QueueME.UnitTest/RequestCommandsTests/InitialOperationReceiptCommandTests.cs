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
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE.MasterData;

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
            var initialOperationReceiptCommand = new InitialOperationReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), new SignatureFactoryME(), inMemoryConfigurationRepository, Mock.Of<IJournalMERepository>());

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
            signaturCreationUnitME.IssuerTin.Should().Equals(tcr.IssuerTIN);
            signaturCreationUnitME.BusinessUnitCode.Should().Equals(tcr.BusinUnitCode);
            signaturCreationUnitME.TcrIntId.Should().Equals(tcr.TCRIntID);
            signaturCreationUnitME.TcrCode.Should().Equals(testTcr);
            signaturCreationUnitME.Should().NotBeNull();

        }


        [Fact]
        public async Task ExecuteAsync_RegisterENU_ENUAlreadyRegisteredException()
        {
            var tcr = CreateTCR();
            var receiptRequest = CreateReceiptRequest(tcr);
            var inMemoryConfigurationRepository = new InMemoryConfigurationRepository();
            var InitialOperationReceiptCommand = new InitialOperationReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), new SignatureFactoryME(), inMemoryConfigurationRepository, Mock.Of<IJournalMERepository>());
            var sutMethod = CallInitialOperationReceiptCommand(InitialOperationReceiptCommand,receiptRequest);
            await sutMethod.Should().ThrowAsync<ENUAlreadyRegisteredException>().ConfigureAwait(false);
        }


        private Func<Task> CallInitialOperationReceiptCommand(InitialOperationReceiptCommand initialOperationReceiptCommand, ReceiptRequest receiptRequest)
        {
            return async () => { var receiptResponse = await initialOperationReceiptCommand.ExecuteAsync(new InMemoryMESSCD("testTcr"), new ftQueue(), receiptRequest, new ftQueueItem()); };
        }

        private TCR CreateTCR()
        {
            return new TCR()
            {
                BusinUnitCode = "aT007FT888",
                IssuerTIN = "02657597",
                TCRIntID = Guid.NewGuid().ToString()
            };
        }

        private ReceiptRequest CreateReceiptRequest(TCR tcr)
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
