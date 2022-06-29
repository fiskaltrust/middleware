using System;
using System.Linq;
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

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper
{
    public class TestHelper
    {
        public static async Task<T> InitializeRequestCommand<T>(ftQueueME queueME, string tcrCode, IJournalMERepository journalMERepository) 
            where T : RequestCommand
        {
            var inMemoryConfigurationRepository = new InMemoryConfigurationRepository();
            var scu = new ftSignaturCreationUnitME()
            {
                ftSignaturCreationUnitMEId = queueME.ftSignaturCreationUnitMEId.Value,
                TcrIntId = "TcrIntId",
                IssuerTin = "4524689",
                TcrCode = tcrCode,
                EnuType = "Regular"
            };
            await inMemoryConfigurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(scu).ConfigureAwait(false);
            await inMemoryConfigurationRepository.InsertOrUpdateQueueMEAsync(queueME);
            var requ = typeof(T);
            return (T) Activator.CreateInstance(requ, new object[] {
                    Mock.Of<ILogger<RequestCommand>>(), inMemoryConfigurationRepository, journalMERepository, new InMemoryQueueItemRepository(), new InMemoryActionJournalRepository(), new QueueMEConfiguration { Sandbox = true }});
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
