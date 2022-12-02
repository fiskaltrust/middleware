using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Azure.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.Azure.Repositories.FR;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Xunit;

namespace fiskaltrust.Middleware.Storage.Azure.AcceptanceTest
{
    public class AzureJournalFRRepositoryTests : AbstractJournalFRRepositoryTests, IClassFixture<AzureStorageFixture>
    {
        private readonly AzureStorageFixture _fixture;

        public AzureJournalFRRepositoryTests(AzureStorageFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyJournalFRRepository> CreateReadOnlyRepository(IEnumerable<ftJournalFR> entries) => await CreateRepository(entries);

        public override async Task<IJournalFRRepository> CreateRepository(IEnumerable<ftJournalFR> entries)
        {
            var azureJournalFRRepository = new AzureJournalFRRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureJournalFRRepository.InsertAsync(entry);
            }

            return azureJournalFRRepository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(nameof(ftJournalFR));
    }
}
