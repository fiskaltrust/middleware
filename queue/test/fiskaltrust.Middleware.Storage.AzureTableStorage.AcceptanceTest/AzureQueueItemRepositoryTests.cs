using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories;
using fiskaltrust.storage.V0;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    public class AzureQueueItemRepositoryTests : AbstractQueueItemRepositoryTests, IClassFixture<AzureStorageFixture>
    {
        private readonly AzureStorageFixture _fixture;

        public AzureQueueItemRepositoryTests(AzureStorageFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyQueueItemRepository> CreateReadOnlyRepository(IEnumerable<ftQueueItem> entries) => await CreateRepository(entries);

        public override async Task<IMiddlewareQueueItemRepository> CreateRepository(IEnumerable<ftQueueItem> entries)
        {
            var azureQueueItemRepository = new AzureQueueItemRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureQueueItemRepository.InsertAsync(entry);
            }

            return azureQueueItemRepository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(nameof(ftQueueItem));
    }
}
