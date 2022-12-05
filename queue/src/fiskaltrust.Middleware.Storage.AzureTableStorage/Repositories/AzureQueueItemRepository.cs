using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.Base.Extensions;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories
{
    public class AzureQueueItemRepository : BaseAzureTableRepository<Guid, TableEntity, ftQueueItem>, IMiddlewareQueueItemRepository, IMiddlewareRepository<ftQueueItem>
    {
        public AzureQueueItemRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftQueueItem)) { }

        protected override void EntityUpdated(ftQueueItem entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueItem entity) => entity.ftQueueItemId;

        protected override ftQueueItem MapToStorageEntity(TableEntity entity) => Mapper.Map(entity);

        protected override TableEntity MapToAzureEntity(ftQueueItem entity) => Mapper.Map(entity);

        public IAsyncEnumerable<ftQueueItem> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = base.GetEntriesOnOrAfterTimeStampAsync(fromInclusive).OrderBy(x => x.TimeStamp);
            return take.HasValue ? result.Take(take.Value) : result;
        }

        public IAsyncEnumerable<ftQueueItem> GetByReceiptReferenceAsync(string cbReceiptReference, string cbTerminalId)
        {
            var filter = string.IsNullOrWhiteSpace(cbTerminalId)
                ? TableClient.CreateQueryFilter($"cbReceiptReference eq {cbReceiptReference}")
                : TableClient.CreateQueryFilter($"cbReceiptReference eq {cbReceiptReference} and cbTerminalID eq {cbTerminalId}");

            var result = _tableClient.QueryAsync<TableEntity>(filter: filter);
            return result.Select(MapToStorageEntity);
        }

        public IAsyncEnumerable<ftQueueItem> GetPreviousReceiptReferencesAsync(ftQueueItem ftQueueItem)
        {
            // TODO: Add separate tables for this call
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(ftQueueItem.request);
            if (!receiptRequest.IncludeInReferences() || (string.IsNullOrWhiteSpace(receiptRequest.cbPreviousReceiptReference) && string.IsNullOrWhiteSpace(ftQueueItem.cbReceiptReference)))
            {
                return AsyncEnumerable.Empty<ftQueueItem>();
            }

            var result = _tableClient.QueryAsync<TableEntity>(filter: TableClient.CreateQueryFilter($"ftQueueRow lt {ftQueueItem.ftQueueRow} and (cbReceiptReference eq {receiptRequest.cbPreviousReceiptReference} or cbReceiptReference eq {ftQueueItem.cbReceiptReference})"));
            return result.Select(MapToStorageEntity).Where(x => JsonConvert.DeserializeObject<ReceiptRequest>(x.request).IncludeInReferences());
        }

        public IAsyncEnumerable<ftQueueItem> GetQueueItemsAfterQueueItem(ftQueueItem ftQueueItem)
        {
            // TODO: Add a separate table for this call
            var result = _tableClient.QueryAsync<TableEntity>(filter: TableClient.CreateQueryFilter($"ftQueueRow ge {ftQueueItem.ftQueueRow}"));
            return result.Select(MapToStorageEntity);
        }
    }
}
