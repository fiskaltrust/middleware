using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.Configuration
{
    public class MySQLSignaturCreationUnitATRepository : AbstractMySQLRepository<Guid, ftSignaturCreationUnitAT>, IConfigurationItemRepository<ftSignaturCreationUnitAT>
    {
        public MySQLSignaturCreationUnitATRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftSignaturCreationUnitAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftSignaturCreationUnitAT> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftSignaturCreationUnitAT>("Select * from ftSignaturCreationUnitAT where ftSignaturCreationUnitATId = @SignaturCreationUnitATId", new { SignaturCreationUnitATId = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftSignaturCreationUnitAT>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftSignaturCreationUnitAT>("select * from ftSignaturCreationUnitAT").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitAT entity)
        {
            EntityUpdated(entity);
            var sql = "REPLACE INTO ftSignaturCreationUnitAT " +
                                 "(ftSignaturCreationUnitATId, Url, ZDA, SN, CertificateBase64, Mode, TimeStamp) " +
                                 "Values (@ftSignaturCreationUnitATId, @Url, @ZDA, @SN, @CertificateBase64, @Mode, @TimeStamp);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftSignaturCreationUnitAT entity) => entity.ftSignaturCreationUnitATId;
    }
}
