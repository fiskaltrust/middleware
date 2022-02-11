using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Azure.Repositories.AT;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.AcceptanceTest
{
    public class AzureJournalATRepositoryTests : AbstractJournalATRepositoryTests
    {
        public override async Task<IReadOnlyJournalATRepository> CreateReadOnlyRepository(IEnumerable<ftJournalAT> entries) => await CreateRepository(entries);

        public override async Task<IJournalATRepository> CreateRepository(IEnumerable<ftJournalAT> entries)
        {
            var azureJournalATRepository = new AzureJournalATRepository(Guid.NewGuid(), Constants.AzureStorageConnectionString);
            foreach (var entry in entries)
            {
                await azureJournalATRepository.InsertAsync(entry);
            }

            return azureJournalATRepository;
        }
    }
}
