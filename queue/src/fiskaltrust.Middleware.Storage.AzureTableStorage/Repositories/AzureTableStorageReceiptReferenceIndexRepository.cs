using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Helpers;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories
{
    public class ReceiptReferenceIndexTable : BaseTableEntity
    {
        public string cbReceiptReference { get; set; }
        public Guid ftQueueItemId { get; set; }
    }

    public class ReceiptReferenceIndex
    {
        public string cbReceiptReference { get; set; }
        public Guid ftQueueItemId { get; set; }
    }

    public class AzureTableStorageReceiptReferenceIndexRepository : BaseAzureTableStorageRepository<string, ReceiptReferenceIndexTable, ReceiptReferenceIndex>
    {
        public AzureTableStorageReceiptReferenceIndexRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = nameof(ReceiptReferenceIndex);

        protected override void EntityUpdated(ReceiptReferenceIndex entity) { }

        protected override string GetIdForEntity(ReceiptReferenceIndex entity) => ConversionHelper.ToBase64UrlString(Encoding.UTF8.GetBytes(entity.cbReceiptReference));

        protected override ReceiptReferenceIndex MapToStorageEntity(ReceiptReferenceIndexTable entity)
            => new ReceiptReferenceIndex
            {
                cbReceiptReference = entity.cbReceiptReference,
                ftQueueItemId = entity.ftQueueItemId
            };


        protected override ReceiptReferenceIndexTable MapToAzureEntity(ReceiptReferenceIndex entity)
            => new ReceiptReferenceIndexTable
            {
                PartitionKey = GetIdForEntity(entity),
                RowKey = entity.ftQueueItemId.ToString(),
                cbReceiptReference = entity.cbReceiptReference,
                ftQueueItemId = entity.ftQueueItemId
            };

        public IAsyncEnumerable<Guid> GetByReceiptReferenceAsync(string cbReceiptReference)
        {
            var partitionKey = GetIdForEntity(new ReceiptReferenceIndex { cbReceiptReference = cbReceiptReference });
            var result = _tableClient.QueryAsync<ReceiptReferenceIndexTable>(x => x.PartitionKey == partitionKey, select: new[] { nameof(ftQueueItem.ftQueueItemId) });
            return result.Select(x => x.ftQueueItemId);
        }

        public async Task InsertOrUpdateAsync(ReceiptReferenceIndex storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }
    }
}

