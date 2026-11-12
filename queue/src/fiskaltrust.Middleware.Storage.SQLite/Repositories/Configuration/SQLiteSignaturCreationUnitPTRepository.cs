using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteSignaturCreationUnitPTRepository : AbstractSQLiteRepository<Guid, ftSignaturCreationUnitPT>, IConfigurationItemRepository<ftSignaturCreationUnitPT>
    {
        public SQLiteSignaturCreationUnitPTRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftSignaturCreationUnitPT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftSignaturCreationUnitPT> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftSignaturCreationUnitPT>("Select * from ftSignaturCreationUnitPT where ftSignaturCreationUnitPTId = @ftSignaturCreationUnitPTId", new { ftSignaturCreationUnitPTId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftSignaturCreationUnitPT>> GetAsync() => await DbConnection.QueryAsync<ftSignaturCreationUnitPT>("select * from ftSignaturCreationUnitPT").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitPT entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftSignaturCreationUnitPT " +
                      "(ftSignaturCreationUnitPTId, PrivateKey, SoftwareCertificateNumber, Url, TimeStamp) " +
                      "Values (@ftSignaturCreationUnitPTId, @PrivateKey, @SoftwareCertificateNumber, @Url, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftSignaturCreationUnitPT entity) => entity.ftSignaturCreationUnitPTId;
    }
}
