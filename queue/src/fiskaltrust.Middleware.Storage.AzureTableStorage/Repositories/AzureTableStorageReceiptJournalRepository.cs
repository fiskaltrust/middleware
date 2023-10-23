using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories
{
    public class AzureTableStorageReceiptJournalRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtReceiptJournal, ftReceiptJournal>, IReceiptJournalRepository, IMiddlewareRepository<ftReceiptJournal>
    {
        public AzureTableStorageReceiptJournalRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftReceiptJournal)) { }

        protected override void EntityUpdated(ftReceiptJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftReceiptJournal entity) => entity.ftReceiptJournalId;

        protected override AzureTableStorageFtReceiptJournal MapToAzureEntity(ftReceiptJournal entity) => Mapper.Map(entity);

        protected override ftReceiptJournal MapToStorageEntity(AzureTableStorageFtReceiptJournal entity) => Mapper.Map(entity);

        public IAsyncEnumerable<ftReceiptJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = base.GetEntriesOnOrAfterTimeStampAsync(fromInclusive).OrderBy(x => x.TimeStamp);
            return take.HasValue ? result.Take(take.Value) : result;
        }
    }
}

