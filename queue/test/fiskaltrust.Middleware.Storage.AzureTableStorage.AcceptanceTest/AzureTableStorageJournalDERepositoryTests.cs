using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.DE;
using fiskaltrust.storage.V0;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    public class AzureTableStorageJournalDERepositoryTests : AbstractJournalDERepositoryTests, IClassFixture<AzureTableStorageFixture>
    {
        private readonly AzureTableStorageFixture _fixture;

        public AzureTableStorageJournalDERepositoryTests(AzureTableStorageFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyJournalDERepository> CreateReadOnlyRepository(IEnumerable<ftJournalDE> entries) => await CreateRepository(entries);

        public override async Task<IJournalDERepository> CreateRepository(IEnumerable<ftJournalDE> entries)
        {
            var azureJournalDERepository = new AzureTableStorageJournalDERepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString), new BlobServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureJournalDERepository.InsertAsync(entry);
            }

            return azureJournalDERepository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(nameof(ftJournalDE));
    }
}
