using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.IT
{
    public class AzureTableStorageJournalITRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtJournalIT, ftJournalIT>, IJournalITRepository
    {
        public AzureTableStorageJournalITRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftJournalIT)) { }

        protected override void EntityUpdated(ftJournalIT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalIT entity) => entity.ftJournalITId;

        protected override AzureTableStorageFtJournalIT MapToAzureEntity(ftJournalIT entity) => Mapper.Map(entity);

        protected override ftJournalIT MapToStorageEntity(AzureTableStorageFtJournalIT entity) => Mapper.Map(entity);
    }
}
