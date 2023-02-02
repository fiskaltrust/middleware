using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteSignaturCreationUnitITRepository : AbstractSQLiteRepository<Guid, ftSignaturCreationUnitIT>, IConfigurationItemRepository<ftSignaturCreationUnitIT>
    {
        public SQLiteSignaturCreationUnitITRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftSignaturCreationUnitIT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftSignaturCreationUnitIT> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftSignaturCreationUnitIT>("Select * from ftSignaturCreationUnitIT where ftSignaturCreationUnitITId = @SignaturCreationUnitIT", new { SignaturCreationUnitIT = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftSignaturCreationUnitIT>> GetAsync() => await DbConnection.QueryAsync<ftSignaturCreationUnitIT>("select * from ftSignaturCreationUnitIT").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitIT entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftSignaturCreationUnitIT " +
                            "(ftSignaturCreationUnitITId, Url, TimeStamp) " +
                            "Values (@ftSignaturCreationUnitITId, @Url, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftSignaturCreationUnitIT entity) => entity.ftSignaturCreationUnitITId;
    }
}
