﻿using System;
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
using fiskaltrust.Middleware.Storage.InMemory.Repositories.MasterData;
using fiskaltrust.storage.V0.MasterData;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.RequestCommandsTests
{
    public class InitialOperationReceiptCommandTests
    {

        [Fact]
        public async Task ExecuteAsync_RegisterENU_ValidResultAsync()
        {
            var receiptRequest = CreateReceiptRequest();

            var tcr = "TestTCRCode";
            var iic = "iic";
            var iicSignature = "iicSignature";
            var businessUnitCode = "aT007FT888";
            var issuerTin = "02657597";

            var inMemoryConfigurationRepository = new InMemoryConfigurationRepository();
            var inMemoryJournalMERepository = new InMemoryJournalMERepository();
            var inMemoryQueueItemRepository = new InMemoryQueueItemRepository();

            var outletRepository = new InMemoryOutletMasterDataRepository();
            await outletRepository.CreateAsync(new OutletMasterData { LocationId = businessUnitCode });
            var posSystemRepository = new InMemoryPosSystemMasterDataRepository();
            await posSystemRepository.CreateAsync(new PosSystemMasterData { });
            var accountRepository = new InMemoryAccountMasterDataRepository();
            await accountRepository.CreateAsync(new AccountMasterData { TaxId = issuerTin });

            var queueMeConfig = new QueueMEConfiguration { Sandbox = true };
            var initialOperationReceiptCommand = new InitialOperationReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), inMemoryConfigurationRepository,
                inMemoryJournalMERepository, inMemoryQueueItemRepository, new InMemoryActionJournalRepository(), outletRepository, posSystemRepository, accountRepository, queueMeConfig, new Factories.SignatureItemFactory(queueMeConfig));

            var inMemoryMESSCD = new InMemoryMESSCD(tcr, iic, iicSignature);
            var queue = new ftQueue()
            {
                ftQueueId = Guid.NewGuid()
            };
            var queueME = new ftQueueME()
            {
                ftQueueMEId = queue.ftQueueId,
            };
            await initialOperationReceiptCommand.ExecuteAsync(inMemoryMESSCD, queue, receiptRequest, new ftQueueItem(), queueME).ConfigureAwait(false);

            var queuMe = await inMemoryConfigurationRepository.GetQueueMEAsync(queue.ftQueueId).ConfigureAwait(false);
            queuMe.Should().NotBeNull();
            queuMe.ftSignaturCreationUnitMEId.HasValue.Should().BeTrue();

            var signaturCreationUnitME = await inMemoryConfigurationRepository.GetSignaturCreationUnitMEAsync(queuMe.ftSignaturCreationUnitMEId.Value).ConfigureAwait(false);
            signaturCreationUnitME.IssuerTin.Should().Equals(issuerTin);
            signaturCreationUnitME.BusinessUnitCode.Should().Equals(businessUnitCode);
            signaturCreationUnitME.TcrIntId.Should().Equals(queue.ftQueueId);
            signaturCreationUnitME.TcrCode.Should().Equals(tcr);
            signaturCreationUnitME.Should().NotBeNull();
        }

        [Fact]
        public async Task ExecuteAsync_WithStartedQueue_ShouldCreateExpectedActionJournal()
        {
            var receiptRequest = CreateReceiptRequest();

            var inMemoryConfigurationRepository = new InMemoryConfigurationRepository();
            var scu = new ftSignaturCreationUnitME();
            await inMemoryConfigurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(scu).ConfigureAwait(false);
            var queue = new ftQueue()
            {
                ftQueueId = Guid.NewGuid(),
                StartMoment = DateTime.UtcNow.AddDays(-1)
            };
            await inMemoryConfigurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
            var queueME = new ftQueueME()
            {
                ftQueueMEId = queue.ftQueueId,
                ftSignaturCreationUnitMEId = scu.ftSignaturCreationUnitMEId
            };

            var actionJournalRepo = new InMemoryActionJournalRepository();

            await inMemoryConfigurationRepository.InsertOrUpdateQueueMEAsync(queueME).ConfigureAwait(false);
            var inMemoryJournalMERepository = new InMemoryJournalMERepository();
            var inMemoryQueueItemRepository = new InMemoryQueueItemRepository();
            
            var sut = new InitialOperationReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), inMemoryConfigurationRepository,
                inMemoryJournalMERepository, inMemoryQueueItemRepository, actionJournalRepo, null, null, null, new QueueMEConfiguration { Sandbox = true }, null);
            await sut.ExecuteAsync(null, queue, receiptRequest, new ftQueueItem(), queueME);

            var ajs = await actionJournalRepo.GetAsync();
            ajs.Should().HaveCount(1);
            ajs.First().ftQueueId.Should().Be(queue.ftQueueId);
            ajs.First().Message.Should().Be($"Queue {queue.ftQueueId} is already activated, initial-operations-receipt can not be executed.");
        }

        private ReceiptRequest CreateReceiptRequest()
        {
            return new ReceiptRequest
            {
                ftReceiptCase = 0x44D5_0000_0000_0003,
                cbReceiptReference = "INIT",
                ftCashBoxID = Guid.NewGuid().ToString(),
                cbReceiptMoment = DateTime.Now,
                cbUser = "Admin",
                cbTerminalID = "TCRIntID_1"
            };
        }
    }
}
