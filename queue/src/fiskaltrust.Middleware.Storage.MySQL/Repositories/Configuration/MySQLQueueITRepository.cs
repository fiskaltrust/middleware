using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.Configuration
{
    public class MySQLQueueITRepository : AbstractMySQLRepository<Guid, ftQueueIT>, IConfigurationItemRepository<ftQueueIT>
    {
        public MySQLQueueITRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftQueueIT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueueIT> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftQueueIT>("Select * from ftQueueIT where ftQueueITId = @QueueITId", new { QueueITId = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftQueueIT>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftQueueIT>("select * from ftQueueIT").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateAsync(ftQueueIT entity)
        {
            EntityUpdated(entity);
            var sql = "REPLACE INTO ftQueueIT " +
                      "(ftQueueITId, ftSignaturCreationUnitITId, LastHash, SSCDFailCount, SSCDFailMoment, SSCDFailQueueItemId, UsedFailedCount, UsedFailedMomentMin, UsedFailedMomentMax, UsedFailedQueueItemId, TimeStamp) " +
                      "Values (@ftQueueITId, @ftSignaturCreationUnitITId, @LastHash, @SSCDFailCount, @SSCDFailMoment, @SSCDFailQueueItemId, @UsedFailedCount, @UsedFailedMomentMin, @UsedFailedMomentMax, @UsedFailedQueueItemId, @TimeStamp);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftQueueIT entity) => entity.ftQueueITId;
    }
}
