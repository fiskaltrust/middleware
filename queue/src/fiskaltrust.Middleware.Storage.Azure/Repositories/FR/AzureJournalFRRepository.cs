using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.FR
{
    public class AzureJournalFRRepository : BaseAzureTableRepository<Guid, AzureFtJournalFR, ftJournalFR>, IJournalFRRepository, IMiddlewareRepository<ftJournalFR>, IMiddlewareJournalFRRepository
    {
        public AzureJournalFRRepository(Guid queueId, string connectionString)
            : base(queueId, connectionString, nameof(ftJournalFR)) { }

        protected override void EntityUpdated(ftJournalFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalFR entity) => entity.ftJournalFRId;

        protected override AzureFtJournalFR MapToAzureEntity(ftJournalFR entity) => Mapper.Map(entity);

        protected override ftJournalFR MapToStorageEntity(AzureFtJournalFR entity) => Mapper.Map(entity);

        public IAsyncEnumerable<ftJournalFR> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = GetEntriesOnOrAfterTimeStampAsync(fromInclusive).ToListAsync().Result.OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }
    }
}
