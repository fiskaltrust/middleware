using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteQueueEURepository : AbstractSQLiteRepository<Guid, ftQueueEU>, IConfigurationItemRepository<ftQueueEU>
    {
        public SQLiteQueueEURepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftQueueEU entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueueEU> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftQueueEU>("Select * from ftQueueEU where ftQueueEUId = @QueueEUId", new { QueueEUId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftQueueEU>> GetAsync() => await DbConnection.QueryAsync<ftQueueEU>("select * from ftQueueEU").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftQueueEU entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftQueueEU " +
                      "(ftQueueEUId, CashBoxIdentification, TimeStamp) " +
                      "Values (@ftQueueEUId, @CashBoxIdentification, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftQueueEU entity) => entity.ftQueueEUId;
    }
}
