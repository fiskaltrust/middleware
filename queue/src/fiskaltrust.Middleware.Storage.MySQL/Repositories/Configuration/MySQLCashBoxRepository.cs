using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.Configuration
{
    public class MySQLCashBoxRepository : AbstractMySQLRepository<Guid, ftCashBox>, IConfigurationItemRepository<ftCashBox>
    {
        public MySQLCashBoxRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftCashBox entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftCashBox> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftCashBox>("Select * from ftCashBox where ftCashBoxId = @CashBoxId", new { CashBoxId = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftCashBox>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftCashBox>("select * from ftCashBox").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateAsync(ftCashBox entity)
        {
            EntityUpdated(entity);
            var sql = "REPLACE INTO ftCashBox " +
                "(ftCashBoxId, TimeStamp) " +
                "Values (@ftCashBoxId, @TimeStamp);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftCashBox entity) => entity.ftCashBoxId;
    }
}
