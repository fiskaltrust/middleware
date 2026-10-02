using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteQueuePLRepository : AbstractSQLiteRepository<Guid, ftQueuePL>, IConfigurationItemRepository<ftQueuePL>
    {
        public SQLiteQueuePLRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftQueuePL entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueuePL> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftQueuePL>("Select * from ftQueuePL where ftQueuePLId = @QueuePLId", new { QueuePLId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftQueuePL>> GetAsync() => await DbConnection.QueryAsync<ftQueuePL>("select * from ftQueuePL").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftQueuePL entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftQueuePL " +
                      "(ftQueuePLId, ftSignaturCreationUnitPLId, CashBoxIdentification, SSCDFailCount, SSCDFailMoment, SSCDFailQueueItemId, UsedFailedCount, UsedFailedMomentMin, UsedFailedMomentMax, UsedFailedQueueItemId, TimeStamp ) " +
                      "Values (@ftQueuePLId, @ftSignaturCreationUnitPLId, @CashBoxIdentification, @SSCDFailCount, @SSCDFailMoment, @SSCDFailQueueItemId, @UsedFailedCount, @UsedFailedMomentMin, @UsedFailedMomentMax, @UsedFailedQueueItemId, @TimeStamp ); ";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftQueuePL entity) => entity.ftQueuePLId;
    }
}
