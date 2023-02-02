using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteQueueITRepository : AbstractSQLiteRepository<Guid, ftQueueIT>, IConfigurationItemRepository<ftQueueIT>
    {
        public SQLiteQueueITRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftQueueIT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueueIT> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftQueueIT>("Select * from ftQueueIT where ftQueueITId = @QueueITId", new { QueueITId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftQueueIT>> GetAsync() => await DbConnection.QueryAsync<ftQueueIT>("select * from ftQueueIT").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftQueueIT entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftQueueIT " +
                      "(ftQueueITId, ftSignaturCreationUnitITId, LastHash, SSCDFailCount, SSCDFailMoment, SSCDFailQueueItemId, UsedFailedCount, UsedFailedMomentMin, UsedFailedMomentMax, UsedFailedQueueItemId, TimeStamp) " +
                      "Values (@ftQueueITId, @ftSignaturCreationUnitITId, @LastHash, @SSCDFailCount, @SSCDFailMoment, @SSCDFailQueueItemId, @UsedFailedCount, @UsedFailedMomentMin, @UsedFailedMomentMax, @UsedFailedQueueItemId, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftQueueIT entity) => entity.ftQueueITId;
    }
}
