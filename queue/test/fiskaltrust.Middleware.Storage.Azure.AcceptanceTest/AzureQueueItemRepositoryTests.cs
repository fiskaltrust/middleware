using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Azure.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.AcceptanceTest
{
    public class AzureQueueItemRepositoryTests : AbstractQueueItemRepositoryTests
    {
        public override async Task<IReadOnlyQueueItemRepository> CreateReadOnlyRepository(IEnumerable<ftQueueItem> entries) => await CreateRepository(entries);

        public override async Task<IMiddlewareQueueItemRepository> CreateRepository(IEnumerable<ftQueueItem> entries)
        {
            var azureQueueItemRepository = new AzureQueueItemRepository(new QueueConfiguration { QueueId = Guid.NewGuid() }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureQueueItemRepository.InsertAsync(entry);
            }

            return azureQueueItemRepository;
        }
    }
}
