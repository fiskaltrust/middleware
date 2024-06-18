using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.ME
{
    public class AzureTableStorageJournalMERepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtJournalME, ftJournalME>, IMiddlewareRepository<ftJournalME>, IMiddlewareJournalMERepository
    {
        public AzureTableStorageJournalMERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "JournalME";

        protected override void EntityUpdated(ftJournalME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalME entity) => entity.ftJournalMEId;

        protected override AzureTableStorageFtJournalME MapToAzureEntity(ftJournalME entity) => Mapper.Map(entity);

        protected override ftJournalME MapToStorageEntity(AzureTableStorageFtJournalME entity) => Mapper.Map(entity);

        public IAsyncEnumerable<ftJournalME> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = base.GetEntriesOnOrAfterTimeStampAsync(fromInclusive).OrderBy(x => x.TimeStamp);
            return take.HasValue ? result.Take(take.Value) : result;
        }
        public async Task<ftJournalME> GetLastEntryAsync()
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtJournalME>(x => x.JournalType == (long) JournalTypes.JournalME);
            return await result.OrderByDescending(x => x.Number).Take(1).Select(MapToStorageEntity).FirstOrDefaultAsync();
        }

        public IAsyncEnumerable<ftJournalME> GetByQueueItemId(Guid queueItemId)
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtJournalME>(x => x.ftQueueItemId == queueItemId);
            return result.Select(MapToStorageEntity);
        }

        public IAsyncEnumerable<ftJournalME> GetByReceiptReference(string cbReceiptReference)
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtJournalME>(x => x.cbReference == cbReceiptReference);
            return result.Select(MapToStorageEntity);
        }
    }
}
