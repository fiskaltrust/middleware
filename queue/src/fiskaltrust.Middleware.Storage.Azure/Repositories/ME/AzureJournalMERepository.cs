using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.ME
{
    public class AzureJournalMERepository : BaseAzureTableRepository<Guid, AzureFtJournalME, ftJournalME>, IMiddlewareRepository<ftJournalME>, IMiddlewareJournalMERepository
    {
        public AzureJournalMERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftJournalME)) { }

        protected override void EntityUpdated(ftJournalME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalME entity) => entity.ftJournalMEId;

        protected override AzureFtJournalME MapToAzureEntity(ftJournalME entity) => Mapper.Map(entity);

        protected override ftJournalME MapToStorageEntity(AzureFtJournalME entity) => Mapper.Map(entity);

        public IAsyncEnumerable<ftJournalME> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = base.GetEntriesOnOrAfterTimeStampAsync(fromInclusive).OrderBy(x => x.TimeStamp);
            return take.HasValue ? result.Take(take.Value) : result;
        }
        public async Task<ftJournalME> GetLastEntryAsync()
        {
            var result = _tableClient.QueryAsync<AzureFtJournalME>(filter: TableClient.CreateQueryFilter($"JournalType eq {(long) JournalTypes.JournalME}"));
            return await result.OrderByDescending(x => x.Number).Take(1).Select(MapToStorageEntity).FirstOrDefaultAsync();
        }

        public IAsyncEnumerable<ftJournalME> GetByQueueItemId(Guid queueItemId)
        {
            var result = _tableClient.QueryAsync<AzureFtJournalME>(filter: TableClient.CreateQueryFilter($"ftQueueItemId eq {queueItemId}"));
            return result.Select(MapToStorageEntity);
        }

        public IAsyncEnumerable<ftJournalME> GetByReceiptReference(string cbReceiptReference)
        {
            var result = _tableClient.QueryAsync<AzureFtJournalME>(filter: TableClient.CreateQueryFilter($"cbReference eq {cbReceiptReference}"));
            return result.Select(MapToStorageEntity);
        }
    }
}
