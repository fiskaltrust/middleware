using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.Configuration
{
    public class MySQLQueueRepository : AbstractMySQLRepository<Guid, ftQueue>, IConfigurationItemRepository<ftQueue>
    {
        public MySQLQueueRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftQueue entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueue> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftQueue>("Select * from ftQueue where ftQueueId = @QueueId", new { QueueId = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftQueue>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftQueue>("select * from ftQueue").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateAsync(ftQueue entity)
        {
            EntityUpdated(entity);
            var sql = "REPLACE INTO ftQueue " +
                                     "(ftQueueId, ftCashBoxId, ftCurrentRow, ftQueuedRow, ftReceiptNumerator, ftReceiptTotalizer, ftReceiptHash, StartMoment, StopMoment, CountryCode, Timeout, TimeStamp) " +
                                     "Values (@ftQueueId, @ftCashBoxId, @ftCurrentRow, @ftQueuedRow, @ftReceiptNumerator, @ftReceiptTotalizer, @ftReceiptHash, @StartMoment, @StopMoment, @CountryCode, @Timeout, @TimeStamp);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftQueue entity) => entity.ftQueueId;
    }
}
