using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.AT
{
    public class AzureJournalATRepository : BaseAzureTableRepository<Guid, AzureFtJournalAT, ftJournalAT>, IJournalATRepository, IMiddlewareRepository<ftJournalAT>
    {
        public AzureJournalATRepository(Guid queueId, string connectionString)
            : base(queueId, connectionString, nameof(ftJournalAT)) { }

        protected override void EntityUpdated(ftJournalAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalAT entity) => entity.ftJournalATId;

        protected override AzureFtJournalAT MapToAzureEntity(ftJournalAT entity) => Mapper.Map(entity);

        protected override ftJournalAT MapToStorageEntity(AzureFtJournalAT entity) => Mapper.Map(entity);

        public IAsyncEnumerable<ftJournalAT> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
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