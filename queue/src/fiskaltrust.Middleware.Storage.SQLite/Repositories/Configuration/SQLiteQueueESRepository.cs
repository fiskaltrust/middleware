using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteQueueESRepository : AbstractSQLiteRepository<Guid, ftQueueES>, IConfigurationItemRepository<ftQueueES>
    {
        public SQLiteQueueESRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftQueueES entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueueES> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftQueueES>("Select * from ftQueueES where ftQueueESId = @QueueESId", new { QueueESId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftQueueES>> GetAsync() => await DbConnection.QueryAsync<ftQueueES>("select * from ftQueueES").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftQueueES entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftQueueES " +
                      "(ftQueueESId, ftSignaturCreationUnitESId, LastHash, SSCDFailCount, SSCDFailMoment, SSCDFailQueueItemId, UsedFailedCount, UsedFailedMomentMin, UsedFailedMomentMax, UsedFailedQueueItemId, TimeStamp) " +
                      "Values (@ftQueueESId, @ftSignaturCreationUnitESId, @LastHash, @SSCDFailCount, @SSCDFailMoment, @SSCDFailQueueItemId, @UsedFailedCount, @UsedFailedMomentMin, @UsedFailedMomentMax, @UsedFailedQueueItemId, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftQueueES entity) => entity.ftQueueESId;
    }
}
