using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    [Collection("AzureTableStorageCollection")]
    public class AzureTableStorageReceiptJournalRepositoryTests : AbstractReceiptJournalRepositoryTests
    {
        private readonly AzureTableStorageFixture _fixture;

        public AzureTableStorageReceiptJournalRepositoryTests(AzureTableStorageFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyReceiptJournalRepository> CreateReadOnlyRepository(IEnumerable<ftReceiptJournal> entries) => await CreateRepository(entries);

        public override async Task<IReceiptJournalRepository> CreateRepository(IEnumerable<ftReceiptJournal> entries)
        {
            var azureReceiptJournalRepository = new AzureTableStorageReceiptJournalRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureReceiptJournalRepository.InsertAsync(entry);
            }

            return azureReceiptJournalRepository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(nameof(ftReceiptJournal));

        public override async Task InsertAsync_ShouldThrowException_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftReceiptJournal>(10).ToList();

            var sut = await CreateRepository(entries);
            await sut.InsertAsync(entries[0]);

            var insertedEntry = await sut.GetAsync(entries[0].ftReceiptJournalId);
            insertedEntry.Should().BeEquivalentTo(entries[0]);
        }
    }
}
