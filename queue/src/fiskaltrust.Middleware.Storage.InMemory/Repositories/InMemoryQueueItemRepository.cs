using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Base.Extensions;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories
{
    public class InMemoryQueueItemRepository : AbstractInMemoryRepository<Guid, ftQueueItem>, IMiddlewareQueueItemRepository
    {
        public InMemoryQueueItemRepository() : base(new List<ftQueueItem>()) { }

        public InMemoryQueueItemRepository(IEnumerable<ftQueueItem> data) : base(data) { }

        protected override void EntityUpdated(ftQueueItem entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueItem entity) => entity.ftQueueItemId;

        public async Task<ftQueueItem> GetByQueueRowAsync(long queueRow) => await Task.FromResult(Data.Values.FirstOrDefault(x => x.ftQueueRow == queueRow)).ConfigureAwait(false);

        public override IAsyncEnumerable<ftQueueItem> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => Data.Select(x => x.Value).Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();

        public override IAsyncEnumerable<ftQueueItem> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {

            var result = Data.Select(x => x.Value).Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }

        public IAsyncEnumerable<ftQueueItem> GetByReceiptReferenceAsync(string receiptReference, string cbTerminalID)
        {
            var result = Data.Select(x => x.Value).Where(x => x.cbReceiptReference == receiptReference);
            if (!string.IsNullOrWhiteSpace(cbTerminalID))
            {
                return result.Where(x => x.cbTerminalID == cbTerminalID).ToAsyncEnumerable();
            }
            else
            {
                return result.ToAsyncEnumerable();
            }
        }

        public IAsyncEnumerable<ftQueueItem> GetPreviousReceiptReferencesAsync(ftQueueItem ftQueueItem)
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(ftQueueItem.request);
            if (!receiptRequest.IsPosReceipt() || (string.IsNullOrWhiteSpace(receiptRequest.cbPreviousReceiptReference) && string.IsNullOrWhiteSpace(ftQueueItem.cbReceiptReference)))
            {
                return new List<ftQueueItem>().ToAsyncEnumerable();
            }

            return Data.Values.Where(x => x.ftQueueRow < ftQueueItem.ftQueueRow && 
                (x.cbReceiptReference == receiptRequest.cbPreviousReceiptReference || x.cbReceiptReference == receiptRequest.cbReceiptReference)).ToAsyncEnumerable();
        }
    }
}