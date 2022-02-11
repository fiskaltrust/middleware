using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteSignaturCreationUnitATRepository : AbstractSQLiteRepository<Guid, ftSignaturCreationUnitAT>, IConfigurationItemRepository<ftSignaturCreationUnitAT>
    {
        public SQLiteSignaturCreationUnitATRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftSignaturCreationUnitAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftSignaturCreationUnitAT> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftSignaturCreationUnitAT>("Select * from ftSignaturCreationUnitAT where ftSignaturCreationUnitATId = @SignaturCreationUnitATId", new { SignaturCreationUnitATId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftSignaturCreationUnitAT>> GetAsync() => await DbConnection.QueryAsync<ftSignaturCreationUnitAT>("select * from ftSignaturCreationUnitAT").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitAT entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftSignaturCreationUnitAT " +
                                 "(ftSignaturCreationUnitATId, Url, ZDA, SN, CertificateBase64, Mode, TimeStamp) " +
                                 "Values (@ftSignaturCreationUnitATId, @Url, @ZDA, @SN, @CertificateBase64, @Mode, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftSignaturCreationUnitAT entity) => entity.ftSignaturCreationUnitATId;
    }
}
