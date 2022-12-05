using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.AT
{
    public class AzureJournalATRepository : BaseAzureTableRepository<Guid, AzureFtJournalAT, ftJournalAT>, IJournalATRepository, IMiddlewareRepository<ftJournalAT>
    {
        public AzureJournalATRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftJournalAT)) { }

        protected override void EntityUpdated(ftJournalAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalAT entity) => entity.ftJournalATId;

        protected override AzureFtJournalAT MapToAzureEntity(ftJournalAT entity) => Mapper.Map(entity);

        protected override ftJournalAT MapToStorageEntity(AzureFtJournalAT entity) => Mapper.Map(entity);

        public IAsyncEnumerable<ftJournalAT> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = base.GetEntriesOnOrAfterTimeStampAsync(fromInclusive).OrderBy(x => x.TimeStamp);
            return take.HasValue ? result.Take(take.Value) : result;
        }
    }
}