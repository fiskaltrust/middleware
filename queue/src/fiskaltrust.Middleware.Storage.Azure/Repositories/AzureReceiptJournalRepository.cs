using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories
{
    public class AzureReceiptJournalRepository : BaseAzureTableRepository<Guid, AzureFtReceiptJournal, ftReceiptJournal>, IReceiptJournalRepository, IMiddlewareRepository<ftReceiptJournal>
    {
        public AzureReceiptJournalRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftReceiptJournal)) { }

        protected override void EntityUpdated(ftReceiptJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftReceiptJournal entity) => entity.ftReceiptJournalId;

        protected override AzureFtReceiptJournal MapToAzureEntity(ftReceiptJournal entity) => Mapper.Map(entity);

        protected override ftReceiptJournal MapToStorageEntity(AzureFtReceiptJournal entity) => Mapper.Map(entity);

        public IAsyncEnumerable<ftReceiptJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = base.GetEntriesOnOrAfterTimeStampAsync(fromInclusive).OrderBy(x => x.TimeStamp);
            return take.HasValue ? result.Take(take.Value) : result;
        }
    }
}
