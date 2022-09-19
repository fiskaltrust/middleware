using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Storage.Base.Extensions;

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

        public IAsyncEnumerable<ftQueueItem> GetPreviousReceiptReferencesAsync(ftQueueItem ftQueueItem)
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(ftQueueItem.request);
            if (!receiptRequest.IsPosReceipt() || (string.IsNullOrWhiteSpace(receiptRequest.cbPreviousReceiptReference) && string.IsNullOrWhiteSpace(ftQueueItem.cbReceiptReference)))
            {
                return new List<ftQueueItem>().ToAsyncEnumerable();
            }

            return DbContext.QueueItemList.Where(x => x.ftQueueRow < ftQueueItem.ftQueueRow && 
                (x.cbReceiptReference == receiptRequest.cbPreviousReceiptReference || x.cbReceiptReference == ftQueueItem.cbReceiptReference)).ToAsyncEnumerable();
        }
    }
}
