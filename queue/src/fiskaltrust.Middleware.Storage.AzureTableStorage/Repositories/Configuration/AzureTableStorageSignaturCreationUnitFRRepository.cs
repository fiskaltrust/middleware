using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageSignaturCreationUnitFRRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtSignaturCreationUnitFR, ftSignaturCreationUnitFR>
    {
        public AzureTableStorageSignaturCreationUnitFRRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "SignaturCreationUnitFR";

        protected override void EntityUpdated(ftSignaturCreationUnitFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitFR entity) => entity.ftSignaturCreationUnitFRId;

        protected override AzureTableStorageFtSignaturCreationUnitFR MapToAzureEntity(ftSignaturCreationUnitFR src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtSignaturCreationUnitFR
            {
                PartitionKey = src.ftSignaturCreationUnitFRId.ToString(),
                RowKey = src.ftSignaturCreationUnitFRId.ToString(),
                ftSignaturCreationUnitFRId = src.ftSignaturCreationUnitFRId,
                Siret = src.Siret,
                PrivateKey = src.PrivateKey,
                CertificateBase64 = src.CertificateBase64,
                CertificateSerialNumber = src.CertificateSerialNumber,
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftSignaturCreationUnitFR MapToStorageEntity(AzureTableStorageFtSignaturCreationUnitFR src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftSignaturCreationUnitFR
            {
                ftSignaturCreationUnitFRId = src.ftSignaturCreationUnitFRId,
                Siret = src.Siret,
                PrivateKey = src.PrivateKey,
                CertificateBase64 = src.CertificateBase64,
                CertificateSerialNumber = src.CertificateSerialNumber,
                TimeStamp = src.TimeStamp
            };
        }
    }
}
