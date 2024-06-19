using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.IT
{
    public class AzureTableStorageJournalITRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtJournalIT, ftJournalIT>, IMiddlewareJournalITRepository
    {
        public AzureTableStorageJournalITRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "JournalIT";

        protected override void EntityUpdated(ftJournalIT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalIT entity) => entity.ftJournalITId;

        protected override AzureTableStorageFtJournalIT MapToAzureEntity(ftJournalIT entity) => Mapper.Map(entity);

        protected override ftJournalIT MapToStorageEntity(AzureTableStorageFtJournalIT entity) => Mapper.Map(entity);

        async Task<ftJournalIT> IMiddlewareJournalITRepository.GetByQueueItemId(Guid queueItemId)
        {
            var items = await GetAsync().ConfigureAwait(false);
            return items.Where(x => x.ftQueueItemId == queueItemId).FirstOrDefault();
        }
    }
}
