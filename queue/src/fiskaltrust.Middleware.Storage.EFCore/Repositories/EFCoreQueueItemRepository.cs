using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Base.Extensions;
using fiskaltrust.storage.V0;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories
{
    public class EFCoreQueueItemRepository : AbstractEFCoreRepostiory<Guid, ftQueueItem>, IMiddlewareQueueItemRepository
    {
        private long _lastInsertedTimeStamp;

        public EFCoreQueueItemRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

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

        public async Task<ftQueueItem> GetByQueueRowAsync(long queueRow) => await Task.FromResult(DbContext.Set<ftQueueItem>().FirstOrDefault(x => x.ftQueueRow == queueRow));

        public override IAsyncEnumerable<ftQueueItem> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => DbContext.QueueItemList.AsQueryable().Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).AsAsyncEnumerable();

        public override IAsyncEnumerable<ftQueueItem> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = DbContext.QueueItemList.AsQueryable().Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).AsAsyncEnumerable();
            }
            return result.AsAsyncEnumerable();
        }

        public IAsyncEnumerable<ftQueueItem> GetByReceiptReferenceAsync(string receiptReference, string cbTerminalID)
        {
            if (!string.IsNullOrWhiteSpace(cbTerminalID))
            {
                return DbContext.QueueItemList.AsQueryable().Where(x => x.cbReceiptReference == receiptReference && x.cbTerminalID == cbTerminalID).OrderBy(x => x.TimeStamp).AsAsyncEnumerable();
            }
            else
            {
                return DbContext.QueueItemList.AsQueryable().Where(x => x.cbReceiptReference == receiptReference).OrderBy(x => x.TimeStamp).AsAsyncEnumerable();
            }
        }

        public IAsyncEnumerable<ftQueueItem> GetPreviousReceiptReferencesAsync(ftQueueItem ftQueueItem)
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(ftQueueItem.request);
            if (!receiptRequest.IncludeInReferences() || (string.IsNullOrWhiteSpace(receiptRequest.cbPreviousReceiptReference) && string.IsNullOrWhiteSpace(ftQueueItem.cbReceiptReference)))
            {
                return new List<ftQueueItem>().ToAsyncEnumerable();
            }

            return Queryable.Where(DbContext.QueueItemList, x =>
                    x.ftQueueRow < ftQueueItem.ftQueueRow &&
                    (x.cbReceiptReference == receiptRequest.cbPreviousReceiptReference || x.cbReceiptReference == ftQueueItem.cbReceiptReference)
                )
                .ToAsyncEnumerable()
                .Where(x => JsonConvert.DeserializeObject<ReceiptRequest>(x.request).IncludeInReferences());
        }
    }
}
