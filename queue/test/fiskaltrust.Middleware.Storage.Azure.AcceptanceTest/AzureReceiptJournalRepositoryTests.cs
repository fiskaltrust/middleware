using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Azure.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.AcceptanceTest
{
    public class AzureReceiptJournalRepositoryTests : AbstractReceiptJournalRepositoryTests
    {
        public override async Task<IReadOnlyReceiptJournalRepository> CreateReadOnlyRepository(IEnumerable<ftReceiptJournal> entries) => await CreateRepository(entries);

        public override async Task<IReceiptJournalRepository> CreateRepository(IEnumerable<ftReceiptJournal> entries)
        {
            var azureReceiptJournalRepository = new AzureReceiptJournalRepository(new QueueConfiguration { QueueId = Guid.NewGuid() }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureReceiptJournalRepository.InsertAsync(entry);
            }

            return azureReceiptJournalRepository;
        }
    }
}
