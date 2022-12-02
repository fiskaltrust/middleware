using System;

using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Azure.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.AcceptanceTest
{
    public class AzureActionJournalRepositoryTests : AbstractActionJournalRepositoryTests
    {
        public override async Task<IReadOnlyActionJournalRepository> CreateReadOnlyRepository(IEnumerable<ftActionJournal> entries) => await CreateRepository(entries);

        public override async Task<IActionJournalRepository> CreateRepository(IEnumerable<ftActionJournal> entries)
        {
            var azureActionJournal = new AzureActionJournalRepository(new QueueConfiguration { QueueId = Guid.NewGuid() }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureActionJournal.InsertAsync(entry).ConfigureAwait(false);
            }

            return azureActionJournal;
        }
    }
}
