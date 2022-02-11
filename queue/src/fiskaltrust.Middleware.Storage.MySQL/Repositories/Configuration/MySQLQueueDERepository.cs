using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.Configuration
{
    public class MySQLQueueDERepository : AbstractMySQLRepository<Guid, ftQueueDE>, IConfigurationItemRepository<ftQueueDE>
    {
        public MySQLQueueDERepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftQueueDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueueDE> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftQueueDE>("Select * from ftQueueDE where ftQueueDEId = @QueueDEId", new { QueueDEId = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftQueueDE>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftQueueDE>("select * from ftQueueDE").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateAsync(ftQueueDE entity)
        {
            EntityUpdated(entity);
            var sql = "REPLACE INTO ftQueueDE " +
                      "(ftQueueDEId, ftSignaturCreationUnitDEId, LastHash, CashBoxIdentification, SSCDFailCount, SSCDFailMoment, SSCDFailQueueItemId, UsedFailedCount, UsedFailedMomentMin, UsedFailedMomentMax, UsedFailedQueueItemId, TimeStamp, DailyClosingNumber) " +
                      "Values (@ftQueueDEId, @ftSignaturCreationUnitDEId, @LastHash, @CashBoxIdentification, @SSCDFailCount, @SSCDFailMoment, @SSCDFailQueueItemId, @UsedFailedCount, @UsedFailedMomentMin, @UsedFailedMomentMax, @UsedFailedQueueItemId, @TimeStamp, @DailyClosingNumber);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftQueueDE entity) => entity.ftQueueDEId;
    }
}
