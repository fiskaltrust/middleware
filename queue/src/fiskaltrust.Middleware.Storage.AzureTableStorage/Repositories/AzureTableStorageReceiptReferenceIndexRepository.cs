using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Azure.Data.Tables;
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

    internal static class ConversionHelper
    {
        public static string ToBase64UrlString(byte[] bytes)
        {
            var base64 = Convert.ToBase64String(bytes);
            return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static byte[] FromBase64UrlString(string base64urlString)
        {
            var base64 = base64urlString.Replace('_', '/').Replace('-', '+');
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }
            return Convert.FromBase64String(base64);
        }
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
    }
}

