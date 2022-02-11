using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.Configuration
{
    public class MySQLSignaturCreationUnitDERepository : AbstractMySQLRepository<Guid, ftSignaturCreationUnitDE>, IConfigurationItemRepository<ftSignaturCreationUnitDE>
    {
        public MySQLSignaturCreationUnitDERepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftSignaturCreationUnitDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftSignaturCreationUnitDE> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftSignaturCreationUnitDE>("Select * from ftSignaturCreationUnitDE where ftSignaturCreationUnitDEId = @SignaturCreationUnitDE", new { SignaturCreationUnitDE = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftSignaturCreationUnitDE>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftSignaturCreationUnitDE>("select * from ftSignaturCreationUnitDE").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitDE entity)
        {
            EntityUpdated(entity);
            var sql = "REPLACE INTO ftSignaturCreationUnitDE " +
                            "(ftSignaturCreationUnitDEId, Url, TimeStamp, TseInfoJson, Mode, ModeConfigurationJson) " +
                            "Values (@ftSignaturCreationUnitDEId, @Url, @TimeStamp, @TseInfoJson, @Mode, @ModeConfigurationJson);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftSignaturCreationUnitDE entity) => entity.ftSignaturCreationUnitDEId;
    }
}
