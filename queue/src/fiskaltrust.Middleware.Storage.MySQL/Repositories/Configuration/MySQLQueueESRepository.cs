using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.Configuration
{
    public class MySQLQueueESRepository : AbstractMySQLRepository<Guid, ftQueueES>, IConfigurationItemRepository<ftQueueES>
    {
        public MySQLQueueESRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftQueueES entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueueES> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftQueueES>("Select * from ftQueueES where ftQueueESId = @QueueESId", new { QueueESId = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftQueueES>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftQueueES>("select * from ftQueueES").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateAsync(ftQueueES entity)
        {
            EntityUpdated(entity);
            var sql = "REPLACE INTO ftQueueES " +
                      "(ftQueueESId, ftSignaturCreationUnitESId, LastHash, SSCDFailCount, SSCDFailMoment, SSCDFailQueueItemId, UsedFailedCount, UsedFailedMomentMin, UsedFailedMomentMax, UsedFailedQueueItemId, TimeStamp) " +
                      "Values (@ftQueueESId, @ftSignaturCreationUnitESId, @LastHash, @SSCDFailCount, @SSCDFailMoment, @SSCDFailQueueItemId, @UsedFailedCount, @UsedFailedMomentMin, @UsedFailedMomentMax, @UsedFailedQueueItemId, @TimeStamp);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftQueueES entity) => entity.ftQueueESId;
    }
}
