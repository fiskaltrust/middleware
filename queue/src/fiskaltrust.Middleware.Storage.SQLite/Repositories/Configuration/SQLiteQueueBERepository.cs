using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteQueueBERepository : AbstractSQLiteRepository<Guid, ftQueueBE>, IConfigurationItemRepository<ftQueueBE>
    {
        public SQLiteQueueBERepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftQueueBE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueueBE> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftQueueBE>("Select * from ftQueueBE where ftQueueBEId = @QueueBEId", new { QueueBEId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftQueueBE>> GetAsync() => await DbConnection.QueryAsync<ftQueueBE>("select * from ftQueueBE").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftQueueBE entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftQueueBE " +
                      "(ftQueueBEId, ftSignaturCreationUnitBEId, LastHash, LastSignature, CashBoxIdentification, SSCDFailCount, SSCDFailMoment, SSCDFailQueueItemId, UsedFailedCount, UsedFailedMomentMin, UsedFailedMomentMax, UsedFailedQueueItemId, TimeStamp ) " +
                      "Values (@ftQueueBEId, @ftSignaturCreationUnitBEId, @LastHash, @LastSignature, @CashBoxIdentification, @SSCDFailCount, @SSCDFailMoment, @SSCDFailQueueItemId, @UsedFailedCount, @UsedFailedMomentMin, @UsedFailedMomentMax, @UsedFailedQueueItemId, @TimeStamp ); ";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftQueueBE entity) => entity.ftQueueBEId;
    }
}
