using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Extensions;
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
            : base(queueConfig, tableServiceClient, TABLE_NAME)
        {
            _receiptReferenceIndexRepository = receiptReferenceIndexRepository;
        }

        public const string TABLE_NAME = "QueueItem";

        protected override void EntityUpdated(ftQueueItem entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueItem entity) => entity.ftQueueItemId;

        protected override TableEntity MapToAzureEntity(ftQueueItem src)
        {
            if (src == null)
            {
                return null;
            }

            var entity = new TableEntity(Mapper.GetHashString(src.ftQueueRow), src.ftQueueItemId.ToString())
            {
                { nameof(ftQueueItem.ftQueueItemId), src.ftQueueItemId },
                { nameof(ftQueueItem.requestHash), src.requestHash },
                { nameof(ftQueueItem.responseHash), src.responseHash },
                { nameof(ftQueueItem.version), src.version },
                { nameof(ftQueueItem.country), src.country },
                { nameof(ftQueueItem.cbReceiptReference), src.cbReceiptReference },
                { nameof(ftQueueItem.cbTerminalID), src.cbTerminalID },
                { nameof(ftQueueItem.cbReceiptMoment), src.cbReceiptMoment.ToUniversalTime() },
                { nameof(ftQueueItem.ftDoneMoment), src.ftDoneMoment?.ToUniversalTime() },
                { nameof(ftQueueItem.ftWorkMoment), src.ftWorkMoment?.ToUniversalTime() },
                { nameof(ftQueueItem.ftQueueTimeout), src.ftQueueTimeout },
                { nameof(ftQueueItem.ftQueueMoment), src.ftQueueMoment.ToUniversalTime() },
                { nameof(ftQueueItem.ftQueueRow), src.ftQueueRow },
                { nameof(ftQueueItem.ftQueueId), src.ftQueueId },
                { nameof(ftQueueItem.TimeStamp), src.TimeStamp },
                { nameof(ftQueueItem.ProcessingVersion), src.ProcessingVersion } 
            };

            entity.SetOversized(nameof(ftQueueItem.request), src.request);
            entity.SetOversized(nameof(ftQueueItem.response), src.response);

            return entity;
        }

        protected override ftQueueItem MapToStorageEntity(TableEntity src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftQueueItem
            {
                ftQueueItemId = src.GetGuid(nameof(ftQueueItem.ftQueueItemId)).GetValueOrDefault(),
                requestHash = src.GetString(nameof(ftQueueItem.requestHash)),
                responseHash = src.GetString(nameof(ftQueueItem.responseHash)),
                version = src.GetString(nameof(ftQueueItem.version)),
                country = src.GetString(nameof(ftQueueItem.country)),
                cbReceiptReference = src.GetString(nameof(ftQueueItem.cbReceiptReference)),
                cbTerminalID = src.GetString(nameof(ftQueueItem.cbTerminalID)),
                cbReceiptMoment = src.GetDateTime(nameof(ftQueueItem.cbReceiptMoment)).GetValueOrDefault(),
                ftDoneMoment = src.GetDateTime(nameof(ftQueueItem.ftDoneMoment)),
                ftWorkMoment = src.GetDateTime(nameof(ftQueueItem.ftWorkMoment)),
                ftQueueTimeout = src.GetInt32(nameof(ftQueueItem.ftQueueTimeout)).GetValueOrDefault(),
                ftQueueMoment = src.GetDateTime(nameof(ftQueueItem.ftQueueMoment)).GetValueOrDefault(),
                ftQueueRow = src.GetInt64(nameof(ftQueueItem.ftQueueRow)).GetValueOrDefault(),
                ftQueueId = src.GetGuid(nameof(ftQueueItem.ftQueueId)).GetValueOrDefault(),
                TimeStamp = src.GetInt64(nameof(ftQueueItem.TimeStamp)).GetValueOrDefault(),
                request = src.GetOversized(nameof(ftQueueItem.request)),
                response = src.GetOversized(nameof(ftQueueItem.response)),
                ProcessingVersion = src.GetString(nameof(ftQueueItem.ProcessingVersion)) 
            };
        }

        public override async Task InsertAsync(ftQueueItem storageEntity)
        {
            await base.InsertAsync(storageEntity);
            await _receiptReferenceIndexRepository.InsertAsync(new ReceiptReferenceIndex { cbReceiptReference = storageEntity.cbReceiptReference, ftQueueItemId = storageEntity.ftQueueItemId });
        }


        // Because we need to update the QueueItems and we update the TimeStamp when doing so the TimeStamp can not be the PrimaryKey for the QueueItem.
        public async Task InsertOrUpdateAsync(ftQueueItem storageEntity)
        {
            EntityUpdated(storageEntity);
            storageEntity.ProcessingVersion = "1.0.0";
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
            await _receiptReferenceIndexRepository.InsertOrUpdateAsync(new ReceiptReferenceIndex { cbReceiptReference = storageEntity.cbReceiptReference, ftQueueItemId = storageEntity.ftQueueItemId });
        }

        public IAsyncEnumerable<ftQueueItem> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive)
        {
            var result = _tableClient.QueryAsync<TableEntity>(filter: TableClient.CreateQueryFilter<ftQueueItem>(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive));
            return result.Select(MapToStorageEntity).OrderBy(x => x.TimeStamp);
        }

        public IAsyncEnumerable<ftQueueItem> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive)
        {
            var result = _tableClient.QueryAsync<TableEntity>(filter: TableClient.CreateQueryFilter<ftQueueItem>(x => x.TimeStamp >= fromInclusive));
            return result.Select(MapToStorageEntity).OrderBy(x => x.TimeStamp);
        }

        public IAsyncEnumerable<ftQueueItem> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = GetEntriesOnOrAfterTimeStampAsync(fromInclusive);
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
            var result = _tableClient.QueryAsync<TableEntity>(filter: TableClient.CreateQueryFilter<ftQueueItem>(x => x.ftQueueRow >= ftQueueItem.ftQueueRow));
            return result.Select(MapToStorageEntity);
        }

        public async IAsyncEnumerable<string> GetGroupedReceiptReferenceAsync(long? fromIncl, long? toIncl)
        {
            var groupByLastNamesQuery =
                    from queueItem in await GetAsync()
                    where
                    (fromIncl.HasValue ? queueItem.TimeStamp >= fromIncl.Value : true) &&
                    (toIncl.HasValue ? queueItem.TimeStamp <= toIncl.Value : true) &&
                    !string.IsNullOrEmpty(queueItem.response) && JsonConvert.DeserializeObject<ReceiptRequest>(queueItem.request).IncludeInReferences()
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
                where !string.IsNullOrEmpty(queueItem.response) && JsonConvert.DeserializeObject<ReceiptRequest>(queueItem.request).IncludeInReferences()
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
            if (receiptRequest.cbPreviousReceiptReference is null)
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
            var result = _tableClient.QueryAsync<TableEntity>(filter: TableClient.CreateQueryFilter<ftQueueItem>(x => x.ftQueueRow == queueRow));
            return MapToStorageEntity(await result.FirstOrDefaultAsync());
        }

        public async Task<int> CountAsync()
        {
            var results = _tableClient.QueryAsync<TableEntity>(select: new string[] { });
            return await results.CountAsync().ConfigureAwait(false);
        }

        public async Task<ftQueueItem> GetLastQueueItemAsync() 
        {
            var result = _tableClient.QueryAsync<TableEntity>(select: new string[] { "PartitionKey", "RowKey" });
            var lastEntity = await result.FirstOrDefaultAsync();

            if (lastEntity == null)
            {
                return null;
            }

            return MapToStorageEntity(await _tableClient.GetEntityAsync<TableEntity>(lastEntity.PartitionKey, lastEntity.RowKey));
        }
    }
}

