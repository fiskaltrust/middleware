using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.Configuration
{
    public class MySQLSignaturCreationUnitFRRepository : AbstractMySQLRepository<Guid, ftSignaturCreationUnitFR>, IConfigurationItemRepository<ftSignaturCreationUnitFR>
    {
        public MySQLSignaturCreationUnitFRRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftSignaturCreationUnitFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftSignaturCreationUnitFR> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftSignaturCreationUnitFR>("Select * from ftSignaturCreationUnitFR where ftSignaturCreationUnitFRId = @SignaturCreationUnitFRId", new { SignaturCreationUnitFRId = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftSignaturCreationUnitFR>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftSignaturCreationUnitFR>("select * from ftSignaturCreationUnitFR").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitFR entity)
        {
            EntityUpdated(entity);

            var sql = "REPLACE INTO ftSignaturCreationUnitFR " +
                      "(ftSignaturCreationUnitFRId, Siret, PrivateKey, CertificateBase64, CertificateSerialNumber, TimeStamp) " +
                      "Values (@ftSignaturCreationUnitFRId, @Siret, @PrivateKey, @CertificateBase64, @CertificateSerialNumber, @TimeStamp);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftSignaturCreationUnitFR entity) => entity.ftSignaturCreationUnitFRId;
    }
}
