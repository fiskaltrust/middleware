using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.FR;
using Xunit;
using ftJournalFRCopyPayload = fiskaltrust.Middleware.Contracts.Models.FR.ftJournalFRCopyPayload;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    [Collection("AzureTableStorageCollection")]
    public class AzureTableStorageJournalFRCopyPayloadRepositoryTests : AbstractJournalFRCopyPayloadRepositoryTests
    {
        private readonly AzureTableStorageFixture _fixture;

        public AzureTableStorageJournalFRCopyPayloadRepositoryTests(AzureTableStorageFixture fixture) => _fixture = fixture;

        public override async Task<IJournalFRCopyPayloadRepository> CreateRepository(IEnumerable<ftJournalFRCopyPayload> entries)
        {
            var repository = new AzureTableStorageJournalFRCopyPayloadRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await repository.InsertAsync(entry);
            }

            return repository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(AzureTableStorageJournalFRCopyPayloadRepository.TABLE_NAME);
    }
}
