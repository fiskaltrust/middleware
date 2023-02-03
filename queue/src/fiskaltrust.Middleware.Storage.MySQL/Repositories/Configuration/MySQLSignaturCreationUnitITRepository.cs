using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.Configuration
{
    public class MySQLSignaturCreationUnitITRepository : AbstractMySQLRepository<Guid, ftSignaturCreationUnitIT>, IConfigurationItemRepository<ftSignaturCreationUnitIT>
    {
        public MySQLSignaturCreationUnitITRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftSignaturCreationUnitIT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftSignaturCreationUnitIT> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftSignaturCreationUnitIT>("Select * from ftSignaturCreationUnitIT where ftSignaturCreationUnitITId = @SignaturCreationUnitIT", new { SignaturCreationUnitIT = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftSignaturCreationUnitIT>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftSignaturCreationUnitIT>("select * from ftSignaturCreationUnitIT").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitIT entity)
        {
            EntityUpdated(entity);
            var sql = "REPLACE INTO ftSignaturCreationUnitIT " +
                            "(ftSignaturCreationUnitITId, Url, TimeStamp) " +
                            "Values (@ftSignaturCreationUnitITId, @Url, @TimeStamp);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftSignaturCreationUnitIT entity) => entity.ftSignaturCreationUnitITId;
    }
}
