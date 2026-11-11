using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteSignaturCreationUnitGRRepository : AbstractSQLiteRepository<Guid, ftSignaturCreationUnitGR>, IConfigurationItemRepository<ftSignaturCreationUnitGR>
    {
        public SQLiteSignaturCreationUnitGRRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftSignaturCreationUnitGR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftSignaturCreationUnitGR> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftSignaturCreationUnitGR>("Select * from ftSignaturCreationUnitGR where ftSignaturCreationUnitGRId = @ftSignaturCreationUnitGRId", new { ftSignaturCreationUnitGRId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftSignaturCreationUnitGR>> GetAsync() => await DbConnection.QueryAsync<ftSignaturCreationUnitGR>("select * from ftSignaturCreationUnitGR").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitGR entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftSignaturCreationUnitGR " +
                      "(ftSignaturCreationUnitGRId, Url, TimeStamp) " +
                      "Values (@ftSignaturCreationUnitGRId, @Url, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftSignaturCreationUnitGR entity) => entity.ftSignaturCreationUnitGRId;
    }
}
