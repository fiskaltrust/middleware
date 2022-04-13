using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.storage.serialization.DE.V0;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v2.me;
using System.Linq;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;

using Xunit;
using FluentAssertions;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using Moq;
using fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper;

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
            var outOfOperationReceiptCommand = new OutOfOperationReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), new SignatureFactoryME(), inMemoryConfigurationRepository);
            
            var inMemoryMESSCD = new InMemoryMESSCD(testTcr);
            await outOfOperationReceiptCommand.ExecuteAsync(inMemoryMESSCD, queue, receiptRequest, new ftQueueItem()).ConfigureAwait(false);

            var queuMe = await inMemoryConfigurationRepository.GetQueueMEAsync(queue.ftQueueId).ConfigureAwait(false);
            queuMe.Should().NotBeNull();
            queuMe.IssuerTIN.Should().Equals(tcr.IssuerTIN);
            queuMe.BusinUnitCode.Should().Equals(tcr.BusinUnitCode);
            queuMe.TCRIntID.Should().Equals(tcr.TCRIntID);
            queuMe.TCRCode.Should().Equals(testTcr);
            queuMe.ftSignaturCreationUnitMEId.HasValue.Should().BeTrue();

            var signaturCreationUnitME = await inMemoryConfigurationRepository.GetSignaturCreationUnitMEAsync(queuMe.ftSignaturCreationUnitMEId.Value).ConfigureAwait(false);
            signaturCreationUnitME.Should().NotBeNull();

        }
        private TCR CreateTCR()
        {
            return new TCR()
            {
                BusinUnitCode = "aT007FT885",
                IssuerTIN = "02657594",
                TCRIntID = Guid.NewGuid().ToString(),
                ValidFrom = DateTime.Now.AddDays(-10)
            };
        }

        private ReceiptRequest CreateReceiptRequest(TCR tcr)
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

        private async Task InsertQueueSCU(TCR  tcr, IConfigurationRepository configurationRepository, string testTcr, Guid ftQueueId)
        {

            var signaturCreationUnitME = new ftSignaturCreationUnitME()
            {
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
                TimeStamp = DateTime.Now.AddDays(-10).Ticks,
            };
            await configurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(signaturCreationUnitME).ConfigureAwait(false);

            var queueME = new ftQueueME()
            {
                ftQueueMEId = ftQueueId,
                TCRIntID = tcr.TCRIntID,
                BusinUnitCode = tcr.BusinUnitCode,
                IssuerTIN = tcr.IssuerTIN,
                TCRCode = testTcr,
                ftSignaturCreationUnitMEId = signaturCreationUnitME.ftSignaturCreationUnitMEId,
                ValidFrom = tcr.ValidFrom,
                ValidTo = tcr.ValidTo
            };
            await configurationRepository.InsertOrUpdateQueueMEAsync(queueME).ConfigureAwait(false);
        }
    }
}
