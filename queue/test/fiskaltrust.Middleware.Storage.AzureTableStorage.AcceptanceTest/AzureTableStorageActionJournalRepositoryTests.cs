using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    [Collection("AzureTableStorageCollection")]
    public class AzureTableStorageActionJournalRepositoryTests : AbstractActionJournalRepositoryTests
    {
        private readonly AzureTableStorageFixture _fixture;

        public AzureTableStorageActionJournalRepositoryTests(AzureTableStorageFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyActionJournalRepository> CreateReadOnlyRepository(IEnumerable<ftActionJournal> entries) => await CreateRepository(entries);

        public override async Task<IMiddlewareActionJournalRepository> CreateRepository(IEnumerable<ftActionJournal> entries)
        {
            var azureActionJournal = new AzureTableStorageActionJournalRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureActionJournal.InsertAsync(entry).ConfigureAwait(false);
            }

            return azureActionJournal;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(nameof(ftActionJournal));

        public override async Task InsertAsync_ShouldThrowException_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftActionJournal>(10).ToList();

            var sut = await CreateRepository(entries);
            await sut.InsertAsync(entries[0]);

            var insertedEntry = await sut.GetAsync(entries[0].ftActionJournalId);
            insertedEntry.Should().BeEquivalentTo(entries[0]);
        }
    }
}
