using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueueATRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueueAT, ftQueueAT>
    {
        public AzureTableStorageQueueATRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "QueueAT";

        protected override void EntityUpdated(ftQueueAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueAT entity) => entity.ftQueueATId;

        public async Task InsertOrUpdateAsync(ftQueueAT storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        protected override AzureTableStorageFtQueueAT MapToAzureEntity(ftQueueAT src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtQueueAT
            {
                PartitionKey = src.ftQueueATId.ToString(),
                RowKey = src.ftQueueATId.ToString(),
                ftQueueATId = src.ftQueueATId,
                LastSignatureCertificateSerialNumber = src.LastSignatureCertificateSerialNumber,
                LastSignatureZDA = src.LastSignatureZDA,
                LastSignatureHash = src.LastSignatureHash,
                MessageMoment = src.MessageMoment?.ToUniversalTime(),
                MessageCount = src.MessageCount,
                UsedMobileQueueItemId = src.UsedMobileQueueItemId,
                UsedMobileMoment = src.UsedMobileMoment?.ToUniversalTime(),
                UsedMobileCount = src.UsedMobileCount,
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                UsedFailedMomentMax = src.UsedFailedMomentMax?.ToUniversalTime(),
                UsedFailedMomentMin = src.UsedFailedMomentMin?.ToUniversalTime(),
                UsedFailedCount = src.UsedFailedCount,
                ftCashNumerator = src.ftCashNumerator,
                SSCDFailMessageSent = src.SSCDFailMessageSent?.ToUniversalTime(),
                SSCDFailMoment = src.SSCDFailMoment?.ToUniversalTime(),
                SSCDFailCount = src.SSCDFailCount,
                LastSettlementQueueItemId = src.LastSettlementQueueItemId,
                LastSettlementMoment = src.LastSettlementMoment?.ToUniversalTime(),
                LastSettlementMonth = src.LastSettlementMonth,
                ClosedSystemNote = src.ClosedSystemNote,
                ClosedSystemValue = src.ClosedSystemValue,
                ClosedSystemKind = src.ClosedSystemKind,
                SignAll = src.SignAll,
                EncryptionKeyBase64 = src.EncryptionKeyBase64,
                CashBoxIdentification = src.CashBoxIdentification,
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                ftCashTotalizer = Convert.ToDouble(src.ftCashTotalizer),
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftQueueAT MapToStorageEntity(AzureTableStorageFtQueueAT src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftQueueAT
            {
                ftQueueATId = src.ftQueueATId,
                LastSignatureCertificateSerialNumber = src.LastSignatureCertificateSerialNumber,
                LastSignatureZDA = src.LastSignatureZDA,
                LastSignatureHash = src.LastSignatureHash,
                MessageMoment = src.MessageMoment,
                MessageCount = src.MessageCount,
                UsedMobileQueueItemId = src.UsedMobileQueueItemId,
                UsedMobileMoment = src.UsedMobileMoment,
                UsedMobileCount = src.UsedMobileCount,
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                UsedFailedMomentMax = src.UsedFailedMomentMax,
                UsedFailedMomentMin = src.UsedFailedMomentMin,
                UsedFailedCount = src.UsedFailedCount,
                ftCashNumerator = src.ftCashNumerator,
                SSCDFailMessageSent = src.SSCDFailMessageSent,
                SSCDFailMoment = src.SSCDFailMoment,
                SSCDFailCount = src.SSCDFailCount,
                LastSettlementQueueItemId = src.LastSettlementQueueItemId,
                LastSettlementMoment = src.LastSettlementMoment,
                LastSettlementMonth = src.LastSettlementMonth,
                ClosedSystemNote = src.ClosedSystemNote,
                ClosedSystemValue = src.ClosedSystemValue,
                ClosedSystemKind = src.ClosedSystemKind,
                SignAll = src.SignAll,
                EncryptionKeyBase64 = src.EncryptionKeyBase64,
                CashBoxIdentification = src.CashBoxIdentification,
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                ftCashTotalizer = Convert.ToDecimal(src.ftCashTotalizer),
                TimeStamp = src.TimeStamp
            };
        }
    }
}

