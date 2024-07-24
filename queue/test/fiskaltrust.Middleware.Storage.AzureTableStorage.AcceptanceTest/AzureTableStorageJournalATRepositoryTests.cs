using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.AT;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    [Collection("AzureTableStorageCollection")]
    public class AzureTableStorageJournalATRepositoryTests : AbstractJournalATRepositoryTests
    {
        private readonly AzureTableStorageFixture _fixture;

        public AzureTableStorageJournalATRepositoryTests(AzureTableStorageFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyJournalATRepository> CreateReadOnlyRepository(IEnumerable<ftJournalAT> entries) => await CreateRepository(entries);

        public override async Task<IJournalATRepository> CreateRepository(IEnumerable<ftJournalAT> entries)
        {
            var azureJournalATRepository = new AzureTableStorageJournalATRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureJournalATRepository.InsertAsync(entry);
            }

            return azureJournalATRepository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(AzureTableStorageJournalATRepository.TABLE_NAME);

        public override async Task InsertAsync_ShouldUpdateEntry_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftJournalAT>(10).ToList();

            var sut = await CreateRepository(entries);
            await sut.InsertAsync(entries[0]);

            var insertedEntry = await sut.GetAsync(entries[0].ftJournalATId);
            insertedEntry.Should().BeEquivalentTo(entries[0]);
        }
    }
}
