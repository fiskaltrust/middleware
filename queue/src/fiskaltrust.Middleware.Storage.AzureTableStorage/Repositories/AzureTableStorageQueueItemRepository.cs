using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.Base.Extensions;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories
{
    public class AzureTableStorageQueueItemRepository : BaseAzureTableStorageRepository<Guid, TableEntity, ftQueueItem>, IMiddlewareQueueItemRepository, IMiddlewareRepository<ftQueueItem>
    {
        private readonly AzureTableStorageReceiptReferenceIndexRepository _receiptReferenceIndexRepository;
        public AzureTableStorageQueueItemRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient, AzureTableStorageReceiptReferenceIndexRepository receiptReferenceIndexRepository)
            : base(queueConfig, tableServiceClient, nameof(ftQueueItem))
        {
            _receiptReferenceIndexRepository = receiptReferenceIndexRepository;
        }

        protected override void EntityUpdated(ftQueueItem entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueItem entity) => entity.ftQueueItemId;

        protected override ftQueueItem MapToStorageEntity(TableEntity entity) => Mapper.Map(entity);

        protected override TableEntity MapToAzureEntity(ftQueueItem entity) => Mapper.Map(entity);

        public override async Task InsertAsync(ftQueueItem storageEntity)
        {
            await base.InsertAsync(storageEntity);
            await _receiptReferenceIndexRepository.InsertAsync(new ReceiptReferenceIndex { cbReceiptReference = storageEntity.cbReceiptReference, ftQueueItemId = storageEntity.ftQueueItemId });
        }

        public override async Task InsertOrUpdateAsync(ftQueueItem storageEntity)
        {
            await base.InsertAsync(storageEntity);
            await _receiptReferenceIndexRepository.InsertOrUpdateAsync(new ReceiptReferenceIndex { cbReceiptReference = storageEntity.cbReceiptReference, ftQueueItemId = storageEntity.ftQueueItemId });
        }

        public IAsyncEnumerable<ftQueueItem> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = base.GetEntriesOnOrAfterTimeStampAsync(fromInclusive).OrderBy(x => x.TimeStamp);
            return take.HasValue ? result.Take(take.Value) : result;
        }

        private async IAsyncEnumerable<ftQueueItem> GetByReceiptReferenceAsync(string cbReceiptReference, Func<ftQueueItem, bool> predicate = null)
        {
            await foreach (var ftQueueItemId in _receiptReferenceIndexRepository.GetByReceiptReferenceAsync(cbReceiptReference))
            {
                var result = await GetAsync(ftQueueItemId);
                if (predicate is not null && !predicate(result))
                {
                    continue;
                }
                yield return result;
            }
        }

        public IAsyncEnumerable<ftQueueItem> GetByReceiptReferenceAsync(string cbReceiptReference, string cbTerminalId)
            => GetByReceiptReferenceAsync(cbReceiptReference, string.IsNullOrWhiteSpace(cbTerminalId) ? x => true : x => x.cbTerminalID == cbTerminalId);

        public IAsyncEnumerable<ftQueueItem> GetQueueItemsAfterQueueItem(ftQueueItem ftQueueItem)
        {
            // TODO: Add a separate table for this call
            var result = _tableClient.QueryAsync<TableEntity>(filter: TableClient.CreateQueryFilter($"ftQueueRow ge {ftQueueItem.ftQueueRow}"));
            return result.Select(MapToStorageEntity);
        }

        public async IAsyncEnumerable<string> GetGroupedReceiptReferenceAsync(long? fromIncl, long? toIncl)
        {
            var groupByLastNamesQuery =
                    from queueItem in await GetAsync()
                    where
                    (fromIncl.HasValue ? queueItem.TimeStamp >= fromIncl.Value : true) &&
                    (toIncl.HasValue ? queueItem.TimeStamp <= toIncl.Value : true) &&
                    JsonConvert.DeserializeObject<ReceiptRequest>(queueItem.request).IncludeInReferences() &&
                    !string.IsNullOrEmpty(queueItem.response)
                    group queueItem by queueItem.cbReceiptReference into newGroup
                    orderby newGroup.Key
                    select newGroup.Key;
            await foreach (var entry in groupByLastNamesQuery.ToAsyncEnumerable())
            {
                yield return entry;
            }
        }

        public async IAsyncEnumerable<ftQueueItem> GetQueueItemsForReceiptReferenceAsync(string receiptReference)
        {
            var queueItemsForReceiptReference =
                from queueItem in GetByReceiptReferenceAsync(receiptReference).ToEnumerable()
                where JsonConvert.DeserializeObject<ReceiptRequest>(queueItem.request).IncludeInReferences() &&
                !string.IsNullOrEmpty(queueItem.response)
                orderby queueItem.TimeStamp
                select queueItem;
            await foreach (var entry in queueItemsForReceiptReference.ToAsyncEnumerable())
            {
                yield return entry;
            }
        }

        public async Task<ftQueueItem> GetClosestPreviousReceiptReferencesAsync(ftQueueItem ftQueueItem)
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(ftQueueItem.request);
            if(receiptRequest.cbPreviousReceiptReference is null)
            {
                return null;
            }
            var queueItemsForReceiptReference =
                            (from queueItem in GetByReceiptReferenceAsync(receiptRequest.cbPreviousReceiptReference).ToEnumerable()
                             where receiptRequest.IncludeInReferences() &&
                             !string.IsNullOrEmpty(queueItem.response)
                             orderby queueItem.TimeStamp descending
                             select queueItem).ToAsyncEnumerable().Take(1);
            return await queueItemsForReceiptReference.FirstOrDefaultAsync();
        }

        public async Task<ftQueueItem> GetByQueueRowAsync(long queueRow)
        {
            var result = _tableClient.QueryAsync<TableEntity>(filter: TableClient.CreateQueryFilter($"ftQueueRow eq {queueRow}"));
            return MapToStorageEntity(await result.FirstOrDefaultAsync());
        }
    }
}

