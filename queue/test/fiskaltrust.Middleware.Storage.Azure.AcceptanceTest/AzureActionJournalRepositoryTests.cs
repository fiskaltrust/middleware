using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Azure.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.Azure.Repositories;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Xunit;

namespace fiskaltrust.Middleware.Storage.Azure.AcceptanceTest
{
    public class AzureActionJournalRepositoryTests : AbstractActionJournalRepositoryTests, IClassFixture<AzureStorageFixture>
    {
        private readonly AzureStorageFixture _fixture;

        public AzureActionJournalRepositoryTests(AzureStorageFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyActionJournalRepository> CreateReadOnlyRepository(IEnumerable<ftActionJournal> entries) => await CreateRepository(entries);

        public override async Task<IActionJournalRepository> CreateRepository(IEnumerable<ftActionJournal> entries)
        {
            var azureActionJournal = new AzureActionJournalRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureActionJournal.InsertAsync(entry).ConfigureAwait(false);
            }

            return azureActionJournal;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(nameof(ftActionJournal));
    }
}
