using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteQueueRepository : AbstractSQLiteRepository<Guid, ftQueue>, IConfigurationItemRepository<ftQueue>
    {
        public SQLiteQueueRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftQueue entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueue> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftQueue>("Select * from ftQueue where ftQueueId = @QueueId", new { QueueId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftQueue>> GetAsync() => await DbConnection.QueryAsync<ftQueue>("select * from ftQueue").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftQueue entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftQueue " +
                                     "(ftQueueId, ftCashBoxId, ftCurrentRow, ftQueuedRow, ftReceiptNumerator, ftReceiptTotalizer, ftReceiptHash, StartMoment, StopMoment, CountryCode, Timeout, TimeStamp) " +
                                     "Values (@ftQueueId, @ftCashBoxId, @ftCurrentRow, @ftQueuedRow, @ftReceiptNumerator, @ftReceiptTotalizer, @ftReceiptHash, @StartMoment, @StopMoment, @CountryCode, @Timeout, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftQueue entity) => entity.ftQueueId;
    }
}
