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
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    [Collection("AzureTableStorageCollection")]
    public class AzureTableStorageQueueItemRepositoryTests : AbstractQueueItemRepositoryTests
    {
        private readonly AzureTableStorageFixture _fixture;

        public AzureTableStorageQueueItemRepositoryTests(AzureTableStorageFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyQueueItemRepository> CreateReadOnlyRepository(IEnumerable<ftQueueItem> entries) => await CreateRepository(entries);

        public override async Task<IMiddlewareQueueItemRepository> CreateRepository(IEnumerable<ftQueueItem> entries)
        {
            var receiptReferenceIndexRepository = new AzureTableStorageReceiptReferenceIndexRepository(
                new QueueConfiguration { QueueId = _fixture.QueueId },
                new TableServiceClient(Constants.AzureStorageConnectionString));
            
            var azureQueueItemRepository = new AzureTableStorageQueueItemRepository(
                new QueueConfiguration { QueueId = _fixture.QueueId },
                new TableServiceClient(Constants.AzureStorageConnectionString),
                receiptReferenceIndexRepository);

            await SetQueueRowAndTimeStamp(entries.ToList());
            foreach (var entry in entries)
            {
                await azureQueueItemRepository.InsertAsync(entry);
            }

            return azureQueueItemRepository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(AzureTableStorageQueueItemRepository.TABLE_NAME);

        public override async Task InsertOrUpdateAsync_ShouldUpdateEntry_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).ToList();
            
            foreach (var entry in entries)
            {
                entry.ProcessingVersion ??= "0.0.0"; 
            }

            var sut = await CreateRepository(entries);
            var count = (await sut.GetAsync()).Count();
            var entryToUpdate = await sut.GetAsync(entries[0].ftQueueItemId);
            entryToUpdate.cbReceiptReference = entryToUpdate.cbReceiptReference + StorageTestFixtureProvider.GetFixture().Create<string>();

            await sut.InsertOrUpdateAsync(entryToUpdate);

            var updatedEntry = await sut.GetAsync(entries[0].ftQueueItemId);

            updatedEntry.cbReceiptReference.Should().Be(entryToUpdate.cbReceiptReference);
            (await sut.GetAsync()).Count().Should().Be(count);
        }

        public override async Task GetQueueItemsForReceiptReferenceAsync_PosAndNonePosReceipts_ValidQueueItems()
        {
            _fixture.CleanTable(AzureTableStorageReceiptReferenceIndexRepository.TABLE_NAME);
            
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).ToList();
            
            foreach (var entry in entries)
            {
                entry.ProcessingVersion ??= "0.0.0";
            }

            await base.GetQueueItemsForReceiptReferenceAsync_PosAndNonePosReceipts_ValidQueueItems();
        }
    }
}