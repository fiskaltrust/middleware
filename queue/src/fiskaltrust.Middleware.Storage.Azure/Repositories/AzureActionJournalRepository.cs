using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities;
using fiskaltrust.storage.V0;
using Microsoft.Azure.Cosmos.Table;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories
{
    public class AzureActionJournalRepository : BaseAzureTableRepository<Guid, AzureFtActionJournal, ftActionJournal>, IActionJournalRepository, IMiddlewareRepository<ftActionJournal>, IMiddlewareActionJournalRepository
    {
        public AzureActionJournalRepository(Guid queueId, string connectionString)
            : base(queueId, connectionString, nameof(ftActionJournal)) { }

        protected override void EntityUpdated(ftActionJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftActionJournal entity) => entity.ftActionJournalId;

        protected override AzureFtActionJournal MapToAzureEntity(ftActionJournal entity) => Mapper.Map(entity);

        protected override ftActionJournal MapToStorageEntity(AzureFtActionJournal entity) => Mapper.Map(entity);

        public IAsyncEnumerable<ftActionJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
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