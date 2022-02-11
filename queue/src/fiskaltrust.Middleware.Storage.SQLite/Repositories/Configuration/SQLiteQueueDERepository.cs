using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteQueueDERepository : AbstractSQLiteRepository<Guid, ftQueueDE>, IConfigurationItemRepository<ftQueueDE>
    {
        public SQLiteQueueDERepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftQueueDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueueDE> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftQueueDE>("Select * from ftQueueDE where ftQueueDEId = @QueueDEId", new { QueueDEId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftQueueDE>> GetAsync() => await DbConnection.QueryAsync<ftQueueDE>("select * from ftQueueDE").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftQueueDE entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftQueueDE " +
                      "(ftQueueDEId, ftSignaturCreationUnitDEId, LastHash, CashBoxIdentification, SSCDFailCount, SSCDFailMoment, SSCDFailQueueItemId, UsedFailedCount, UsedFailedMomentMin, UsedFailedMomentMax, UsedFailedQueueItemId, TimeStamp, DailyClosingNumber) " +
                      "Values (@ftQueueDEId, @ftSignaturCreationUnitDEId, @LastHash, @CashBoxIdentification, @SSCDFailCount, @SSCDFailMoment, @SSCDFailQueueItemId, @UsedFailedCount, @UsedFailedMomentMin, @UsedFailedMomentMax, @UsedFailedQueueItemId, @TimeStamp, @DailyClosingNumber);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftQueueDE entity) => entity.ftQueueDEId;
    }
}
