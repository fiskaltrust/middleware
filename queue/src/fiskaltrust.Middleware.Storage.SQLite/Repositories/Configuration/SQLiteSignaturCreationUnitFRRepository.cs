using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteSignaturCreationUnitFRRepository : AbstractSQLiteRepository<Guid, ftSignaturCreationUnitFR>, IConfigurationItemRepository<ftSignaturCreationUnitFR>
    {
        public SQLiteSignaturCreationUnitFRRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftSignaturCreationUnitFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftSignaturCreationUnitFR> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftSignaturCreationUnitFR>("Select * from ftSignaturCreationUnitFR where ftSignaturCreationUnitFRId = @SignaturCreationUnitFRId", new { SignaturCreationUnitFRId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftSignaturCreationUnitFR>> GetAsync() => await DbConnection.QueryAsync<ftSignaturCreationUnitFR>("select * from ftSignaturCreationUnitFR").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitFR entity)
        {
            EntityUpdated(entity);

            var sql = "INSERT OR REPLACE INTO ftSignaturCreationUnitFR " +
                      "(ftSignaturCreationUnitFRId, Siret, PrivateKey, CertificateBase64, CertificateSerialNumber, TimeStamp) " +
                      "Values (@ftSignaturCreationUnitFRId, @Siret, @PrivateKey, @CertificateBase64, @CertificateSerialNumber, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftSignaturCreationUnitFR entity) => entity.ftSignaturCreationUnitFRId;
    }
}
