﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Storage.Base.Extensions;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;

namespace fiskaltrust.Middleware.Storage.EF.Repositories
{
    public class EfQueueItemRepository : AbstractEFRepostiory<Guid, ftQueueItem>, IMiddlewareQueueItemRepository
    {
        private long _lastInsertedTimeStamp;

        public EfQueueItemRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftQueueItem entity)
        {
            if (_lastInsertedTimeStamp == DateTime.UtcNow.Ticks)
            {
                Task.Run(() => Task.Delay(1)).Wait();
            }
            entity.TimeStamp = DateTime.UtcNow.Ticks;
            _lastInsertedTimeStamp = entity.TimeStamp;
        }

        protected override Guid GetIdForEntity(ftQueueItem entity) => entity.ftQueueItemId;

        public async Task<ftQueueItem> GetByQueueRowAsync(long queueRow) => await Task.FromResult(DbContext.Set<ftQueueItem>().FirstOrDefault(x => x.ftQueueRow == queueRow)).ConfigureAwait(false);

        public override IAsyncEnumerable<ftQueueItem> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => DbContext.QueueItemList.Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();

        public override IAsyncEnumerable<ftQueueItem> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = DbContext.QueueItemList.Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }

        public IAsyncEnumerable<ftQueueItem> GetByReceiptReferenceAsync(string receiptReference, string cbTerminalID)
        {
            if (!string.IsNullOrWhiteSpace(cbTerminalID))
            {
                return DbContext.QueueItemList.Where(x => x.cbReceiptReference == receiptReference && x.cbTerminalID == cbTerminalID).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();
            }
            else
            {
                return DbContext.QueueItemList.Where(x => x.cbReceiptReference == receiptReference).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();
            }
        }

        public IAsyncEnumerable<ftQueueItem> GetQueueItemsAfterQueueItem(ftQueueItem ftQueueItem)
        {
            return DbContext.QueueItemList.Where(x => x.ftQueueRow >= ftQueueItem.ftQueueRow).ToAsyncEnumerable();
        }

        public async IAsyncEnumerable<string> GetGroupedReceiptReferenceAsync(long? fromIncl, long? toIncl)
        {
            var groupByLastNamesQuery =
                    from queueItem in DbContext.QueueItemList
                    where
                    (fromIncl.HasValue ? queueItem.TimeStamp >= fromIncl.Value : true) &&
                    (toIncl.HasValue ? queueItem.TimeStamp <= toIncl.Value : true) &&
                    !string.IsNullOrEmpty(queueItem.response)
                    group queueItem by queueItem.cbReceiptReference into newGroup
                    orderby newGroup.Key
                    select newGroup;
            await foreach (var entry in groupByLastNamesQuery.ToAsyncEnumerable())
            {
                yield return entry.Key;
            }
        }
        public async IAsyncEnumerable<ftQueueItem> GetQueueItemsForReceiptReferenceAsync(string receiptReference)
        {
            var queueItemsForReceiptReference =
                from queueItem in DbContext.QueueItemList.AsQueryable()
                where queueItem.cbReceiptReference == receiptReference &&
                !string.IsNullOrEmpty(queueItem.response)
                orderby queueItem.TimeStamp
                select queueItem;
            await foreach (var entry in queueItemsForReceiptReference.ToAsyncEnumerable())
            {
                if (JsonConvert.DeserializeObject<ReceiptRequest>(entry.request).IncludeInReferences())
                {
                    yield return entry;
                }
            }
        }
        public async Task<ftQueueItem> GetFirstPreviousReceiptReferencesAsync(ftQueueItem ftQueueItem)
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(ftQueueItem.request);

            if (string.IsNullOrWhiteSpace(receiptRequest.cbPreviousReceiptReference) || string.IsNullOrWhiteSpace(ftQueueItem.cbReceiptReference) || receiptRequest.cbPreviousReceiptReference == ftQueueItem.cbReceiptReference)
            {
                return null;
            }
            var queueItemsForReceiptReference =
                            (from queueItem in DbContext.QueueItemList.AsQueryable()
                             where queueItem.ftQueueRow < ftQueueItem.ftQueueRow &&
                             queueItem.cbReceiptReference == receiptRequest.cbPreviousReceiptReference &&
                             !string.IsNullOrEmpty(queueItem.response)
                             orderby queueItem.TimeStamp
                             select queueItem).ToAsyncEnumerable();

            await foreach (var entry in queueItemsForReceiptReference)
            {
                if (JsonConvert.DeserializeObject<ReceiptRequest>(entry.request).IncludeInReferences())
                {
                    return entry;
                }
            }
            return null;
        }
    }
}
