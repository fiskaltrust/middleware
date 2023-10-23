using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.FR;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    [Collection("AzureTableStorageCollection")]
    public class AzureTableStorageJournalFRRepositoryTests : AbstractJournalFRRepositoryTests
    {
        private readonly AzureTableStorageFixture _fixture;

        public AzureTableStorageJournalFRRepositoryTests(AzureTableStorageFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyJournalFRRepository> CreateReadOnlyRepository(IEnumerable<ftJournalFR> entries) => await CreateRepository(entries);

        public override async Task<IJournalFRRepository> CreateRepository(IEnumerable<ftJournalFR> entries)
        {
            var azureJournalFRRepository = new AzureTableStorageJournalFRRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureJournalFRRepository.InsertAsync(entry);
            }

            return azureJournalFRRepository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(nameof(ftJournalFR));

        public override async Task InsertAsync_ShouldUpdateEntry_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftJournalFR>(10).ToList();

            var sut = await CreateRepository(entries);
            await sut.InsertAsync(entries[0]);

            var insertedEntry = await sut.GetAsync(entries[0].ftJournalFRId);
            insertedEntry.Should().BeEquivalentTo(entries[0]);
        }
    }
}
