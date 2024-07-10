using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageSignaturCreationUnitMERepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtSignaturCreationUnitME, ftSignaturCreationUnitME>
    {
        public AzureTableStorageSignaturCreationUnitMERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "SignaturCreationUnitME";

        protected override void EntityUpdated(ftSignaturCreationUnitME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitME entity) => entity.ftSignaturCreationUnitMEId;

        protected override AzureTableStorageFtSignaturCreationUnitME MapToAzureEntity(ftSignaturCreationUnitME src)
        {
            if (src == null)
            {
                return null;
            }
            return new AzureTableStorageFtSignaturCreationUnitME
            {
                PartitionKey = src.ftSignaturCreationUnitMEId.ToString(),
                RowKey = src.ftSignaturCreationUnitMEId.ToString(),
                ftSignaturCreationUnitMEId = src.ftSignaturCreationUnitMEId,
                TimeStamp = src.TimeStamp,
                IssuerTin = src.IssuerTin,
                BusinessUnitCode = src.BusinessUnitCode,
                TcrIntId = src.TcrIntId,
                SoftwareCode = src.SoftwareCode,
                MaintainerCode = src.MaintainerCode,
                ValidFrom = src.ValidFrom?.ToUniversalTime(),
                ValidTo = src.ValidTo?.ToUniversalTime(),
                TcrCode = src.TcrCode
            };
        }

        protected override ftSignaturCreationUnitME MapToStorageEntity(AzureTableStorageFtSignaturCreationUnitME src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftSignaturCreationUnitME
            {
                ftSignaturCreationUnitMEId = src.ftSignaturCreationUnitMEId,
                TimeStamp = src.TimeStamp,
                IssuerTin = src.IssuerTin,
                BusinessUnitCode = src.BusinessUnitCode,
                TcrIntId = src.TcrIntId,
                SoftwareCode = src.SoftwareCode,
                MaintainerCode = src.MaintainerCode,
                ValidFrom = src.ValidFrom,
                ValidTo = src.ValidTo,
                TcrCode = src.TcrCode
            };
        }
    }
}

