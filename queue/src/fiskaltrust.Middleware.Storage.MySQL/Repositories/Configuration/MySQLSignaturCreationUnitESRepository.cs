using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.Configuration
{
    public class MySQLSignaturCreationUnitESRepository : AbstractMySQLRepository<Guid, ftSignaturCreationUnitES>, IConfigurationItemRepository<ftSignaturCreationUnitES>
    {
        public MySQLSignaturCreationUnitESRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftSignaturCreationUnitES entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftSignaturCreationUnitES> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftSignaturCreationUnitES>("Select * from ftSignaturCreationUnitES where ftSignaturCreationUnitESId = @SignaturCreationUnitES", new { SignaturCreationUnitES = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftSignaturCreationUnitES>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftSignaturCreationUnitES>("select * from ftSignaturCreationUnitES").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitES entity)
        {
            EntityUpdated(entity);
            var sql = "REPLACE INTO ftSignaturCreationUnitES " +
                            "(ftSignaturCreationUnitESId, Url, TimeStamp) " +
                            "Values (@ftSignaturCreationUnitESId, @Url, @TimeStamp);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftSignaturCreationUnitES entity) => entity.ftSignaturCreationUnitESId;
    }
}
