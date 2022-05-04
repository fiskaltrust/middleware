﻿using System;
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
            var outOfOperationReceiptCommand = new OutOfOperationReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), new SignatureFactoryME(), inMemoryConfigurationRepository, Mock.Of<IJournalMERepository>());
            var inMemoryMESSCD = new InMemoryMESSCD(testTcr);
            await outOfOperationReceiptCommand.ExecuteAsync(inMemoryMESSCD, queue, receiptRequest, new ftQueueItem()).ConfigureAwait(false);

            var queuMe = await inMemoryConfigurationRepository.GetQueueMEAsync(queue.ftQueueId).ConfigureAwait(false);
            queuMe.Should().NotBeNull();
            queuMe.ftSignaturCreationUnitMEId.HasValue.Should().BeTrue();

            var signaturCreationUnitME = await inMemoryConfigurationRepository.GetSignaturCreationUnitMEAsync(queuMe.ftSignaturCreationUnitMEId.Value).ConfigureAwait(false);
            signaturCreationUnitME.Should().NotBeNull();
            signaturCreationUnitME.IssuerTin.Should().Equals(tcr.IssuerTIN);
            signaturCreationUnitME.BusinessUnitCode.Should().Equals(tcr.BusinUnitCode);
            signaturCreationUnitME.TcrIntId.Should().Equals(tcr.TCRIntID);
            signaturCreationUnitME.TcrCode.Should().Equals(testTcr);

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
                TcrIntId = tcr.TCRIntID,
                BusinessUnitCode = tcr.BusinUnitCode,
                IssuerTin = tcr.IssuerTIN,
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
