using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteQueuePTRepository : AbstractSQLiteRepository<Guid, ftQueuePT>, IConfigurationItemRepository<ftQueuePT>
    {
        public SQLiteQueuePTRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftQueuePT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueuePT> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftQueuePT>("Select * from ftQueuePT where ftQueuePTId = @QueuePTId", new { QueuePTId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftQueuePT>> GetAsync() => await DbConnection.QueryAsync<ftQueuePT>("select * from ftQueuePT").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftQueuePT entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftQueuePT " +
                      "(ftQueuePTId, ftSignaturCreationUnitPTId, LastHash, LastSignature, CashBoxIdentification, SSCDFailCount, SSCDFailMoment, SSCDFailQueueItemId, UsedFailedCount, UsedFailedMomentMin, UsedFailedMomentMax, UsedFailedQueueItemId, IssuerTIN, NumeratorStorage, TimeStamp) " +
                      "Values (@ftQueuePTId, @ftSignaturCreationUnitPTId, @LastHash, @LastSignature, @CashBoxIdentification, @SSCDFailCount, @SSCDFailMoment, @SSCDFailQueueItemId, @UsedFailedCount, @UsedFailedMomentMin, @UsedFailedMomentMax, @UsedFailedQueueItemId, @IssuerTIN, @NumeratorStorage, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftQueuePT entity) => entity.ftQueuePTId;
    }
}
