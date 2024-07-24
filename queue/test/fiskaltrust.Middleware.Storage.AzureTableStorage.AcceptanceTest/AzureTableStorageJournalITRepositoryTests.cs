using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.IT;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    [Collection("AzureTableStorageCollection")]
    public class AzureTableStorageJournalITRepositoryTests : AbstractJournalITRepositoryTests
    {
        private readonly AzureTableStorageFixture _fixture;

        public AzureTableStorageJournalITRepositoryTests(AzureTableStorageFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyJournalITRepository> CreateReadOnlyRepository(IEnumerable<ftJournalIT> entries) => await CreateRepository(entries);

        public override async Task<IMiddlewareJournalITRepository> CreateRepository(IEnumerable<ftJournalIT> entries)
        {
            var azureJournalITRepository = new AzureTableStorageJournalITRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureJournalITRepository.InsertAsync(entry);
            }

            return azureJournalITRepository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(AzureTableStorageJournalITRepository.TABLE_NAME);

        public override async Task InsertAsync_ShouldUpdateEntry_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftJournalIT>(10).ToList();

            var sut = await CreateRepository(entries);
            await sut.InsertAsync(entries[0]);

            var insertedEntry = await sut.GetAsync(entries[0].ftJournalITId);
            insertedEntry.Should().BeEquivalentTo(entries[0]);
        }
    }
}
