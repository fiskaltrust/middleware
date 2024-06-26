using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageSignaturCreationUnitATRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtSignaturCreationUnitAT, ftSignaturCreationUnitAT>
    {
        public AzureTableStorageSignaturCreationUnitATRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "SignaturCreationUnitAT";

        protected override void EntityUpdated(ftSignaturCreationUnitAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitAT entity) => entity.ftSignaturCreationUnitATId;

        protected override AzureTableStorageFtSignaturCreationUnitAT MapToAzureEntity(ftSignaturCreationUnitAT src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtSignaturCreationUnitAT
            {
                PartitionKey = src.ftSignaturCreationUnitATId.ToString(),
                RowKey = src.ftSignaturCreationUnitATId.ToString(),
                ftSignaturCreationUnitATId = src.ftSignaturCreationUnitATId,
                Url = src.Url,
                ZDA = src.ZDA,
                SN = src.SN,
                CertificateBase64 = src.CertificateBase64,
                Mode = src.Mode,
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftSignaturCreationUnitAT MapToStorageEntity(AzureTableStorageFtSignaturCreationUnitAT src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftSignaturCreationUnitAT
            {
                ftSignaturCreationUnitATId = src.ftSignaturCreationUnitATId,
                Url = src.Url,
                ZDA = src.ZDA,
                SN = src.SN,
                CertificateBase64 = src.CertificateBase64,
                Mode = src.Mode,
                TimeStamp = src.TimeStamp
            };
        }
    }
}

