using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Azure.Repositories.FR;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.AcceptanceTest
{
    public class AzureJournalFRRepositoryTests : AbstractJournalFRRepositoryTests
    {
        public override async Task<IReadOnlyJournalFRRepository> CreateReadOnlyRepository(IEnumerable<ftJournalFR> entries) => await CreateRepository(entries);

        public override async Task<IJournalFRRepository> CreateRepository(IEnumerable<ftJournalFR> entries)
        {
            var azureJournalFRRepository = new AzureJournalFRRepository(new QueueConfiguration { QueueId = Guid.NewGuid() }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureJournalFRRepository.InsertAsync(entry);
            }

            return azureJournalFRRepository;
        }
    }
}
