using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageSignaturCreationUnitDERepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtSignaturCreationUnitDE, ftSignaturCreationUnitDE>
    {
        public AzureTableStorageSignaturCreationUnitDERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "SignaturCreationUnitDE";

        protected override void EntityUpdated(ftSignaturCreationUnitDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitDE entity) => entity.ftSignaturCreationUnitDEId;

        protected override AzureTableStorageFtSignaturCreationUnitDE MapToAzureEntity(ftSignaturCreationUnitDE src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtSignaturCreationUnitDE
            {
                PartitionKey = src.ftSignaturCreationUnitDEId.ToString(),
                RowKey = src.ftSignaturCreationUnitDEId.ToString(),
                ftSignaturCreationUnitDEId = src.ftSignaturCreationUnitDEId,
                Url = src.Url,
                TseInfoJson = src.TseInfoJson,
                TimeStamp = src.TimeStamp,
                Mode = src.Mode,
                ModeConfigurationJson = src.ModeConfigurationJson
            };
        }

        protected override ftSignaturCreationUnitDE MapToStorageEntity(AzureTableStorageFtSignaturCreationUnitDE src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftSignaturCreationUnitDE
            {
                ftSignaturCreationUnitDEId = src.ftSignaturCreationUnitDEId,
                TseInfoJson = src.TseInfoJson,
                TimeStamp = src.TimeStamp,
                Mode = src.Mode,
                ModeConfigurationJson = src.ModeConfigurationJson,
                Url = src.Url
            };
        }
    }
}
