using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.Configuration
{
    public class MySQLQueueMERepository : AbstractMySQLRepository<Guid, ftQueueME>, IConfigurationItemRepository<ftQueueME>
    {
        public MySQLQueueMERepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftQueueME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueueME> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftQueueME>("Select * from ftQueueME where ftQueueMEId = @QueueMEId", new { QueueMEId = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftQueueME>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftQueueME>("select * from ftQueueME").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateAsync(ftQueueME entity)
        {
            EntityUpdated(entity);
            var sql = "REPLACE INTO ftQueueME " +
                      "(ftQueueMEId, ftSignaturCreationUnitMEId, LastHash, SSCDFailCount, SSCDFailMoment, SSCDFailQueueItemId, UsedFailedCount, UsedFailedMomentMin, UsedFailedMomentMax, UsedFailedQueueItemId, DailyClosingNumber ) " +
                      "Values (@ftQueueMEId, @ftSignaturCreationUnitMEId, @LastHash, @SSCDFailCount, @SSCDFailMoment, @SSCDFailQueueItemId, @UsedFailedCount, @UsedFailedMomentMin, @UsedFailedMomentMax, @UsedFailedQueueItemId, @DailyClosingNumber ); ";

            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftQueueME entity) => entity.ftQueueMEId;
    }
}
