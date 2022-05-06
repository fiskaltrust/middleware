using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.ME;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.Middleware.Contracts.Constants;

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper
{
    public class TestHelper
    {
        public static async Task<T> InitializeRequestCommand<T>(Guid queueId, string tcrCode, IActionJournalRepository actionJournalRepository) 
            where T : RequestCommand
        {
            var inMemoryConfigurationRepository = new InMemoryConfigurationRepository();
            var scu = new ftSignaturCreationUnitME()
            {
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
                TcrIntId = "TcrIntId",
                IssuerTin = "4524689",
                TcrCode = tcrCode,
                EnuType = "Regular"
            };
            await inMemoryConfigurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(scu).ConfigureAwait(false);
            var queueME = new ftQueueME()
            {
                ftQueueMEId = queueId,
                ftSignaturCreationUnitMEId = scu.ftSignaturCreationUnitMEId
            };
            await inMemoryConfigurationRepository.InsertOrUpdateQueueMEAsync(queueME);
            var requ = typeof(T);
            return (T) Activator.CreateInstance(requ, new object[] {
                    Mock.Of<ILogger<RequestCommand>>(), new SignatureFactoryME(),
                    inMemoryConfigurationRepository, new InMemoryJournalMERepository(), new InMemoryQueueItemRepository(), actionJournalRepository
            });
        }

        public static async Task CheckResultActionJournal(ftQueue queue, ftQueueItem queueItem, InMemoryActionJournalRepository actionJournalRep, RequestCommandResponse requestResponse, long journalTypes)
        {
            requestResponse.ActionJournals.Count().Should().Be(1);
            var actionJournals = await actionJournalRep.GetAsync().ConfigureAwait(false);
            actionJournals.Count().Should().Be(1);
            requestResponse.ActionJournals.FirstOrDefault().ftQueueId.Should().Be(queue.ftQueueId);
            requestResponse.ActionJournals.FirstOrDefault().ftQueueItemId.Should().Be(queueItem.ftQueueItemId);
            requestResponse.ActionJournals.FirstOrDefault().Type.Should().Be(journalTypes.ToString());
            actionJournals.FirstOrDefault().ftQueueId.Should().Be(queue.ftQueueId);
            actionJournals.FirstOrDefault().ftQueueItemId.Should().Be(queueItem.ftQueueItemId);
            actionJournals.FirstOrDefault().Type.Should().Be(journalTypes.ToString());
        }
        public static ReceiptRequest CreateReceiptRequest(long receiptCase)
        {
            return new ReceiptRequest()
            {
                ftReceiptCase = receiptCase,
                cbReceiptMoment = DateTime.Now,
                cbReceiptReference = "107",
                cbReceiptAmount = 500
            };
        }
    }
}
