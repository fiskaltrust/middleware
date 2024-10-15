using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.DE;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    [Collection("AzureTableStorageCollection")]
    public class AzureTableStorageJournalDERepositoryTests : AbstractJournalDERepositoryTests
    {
        private readonly AzureTableStorageFixture _fixture;

        public AzureTableStorageJournalDERepositoryTests(AzureTableStorageFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyJournalDERepository> CreateReadOnlyRepository(IEnumerable<ftJournalDE> entries) => await CreateRepository(entries);

        public override async Task<IMiddlewareJournalDERepository> CreateRepository(IEnumerable<ftJournalDE> entries)
        {
            var azureJournalDERepository = new AzureTableStorageJournalDERepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString), new BlobServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureJournalDERepository.InsertAsync(entry);
            }

            return azureJournalDERepository;
        }

        public override void DisposeDatabase()
        {
            _fixture.CleanTable(AzureTableStorageJournalDERepository.TABLE_NAME);
            _fixture.CleanBlobStorage(AzureTableStorageJournalDERepository.BLOB_CONTAINER_NAME);
        }

        public override async Task InsertAsync_ShouldUpdateEntry_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftJournalDE>(10).ToList();

            var sut = await CreateRepository(entries);
            await sut.InsertAsync(entries[0]);

            var insertedEntry = await sut.GetAsync(entries[0].ftJournalDEId);
            insertedEntry.Should().BeEquivalentTo(entries[0]);
        }
    }
}
