using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteSignaturCreationUnitPLRepository : AbstractSQLiteRepository<Guid, ftSignaturCreationUnitPL>, IConfigurationItemRepository<ftSignaturCreationUnitPL>
    {
        public SQLiteSignaturCreationUnitPLRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftSignaturCreationUnitPL entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftSignaturCreationUnitPL> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftSignaturCreationUnitPL>("Select * from ftSignaturCreationUnitPL where ftSignaturCreationUnitPLId = @ftSignaturCreationUnitPLId", new { ftSignaturCreationUnitPLId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftSignaturCreationUnitPL>> GetAsync() => await DbConnection.QueryAsync<ftSignaturCreationUnitPL>("select * from ftSignaturCreationUnitPL").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitPL entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftSignaturCreationUnitPL " +
                            "(ftSignaturCreationUnitPLId, TimeStamp, Url, TseInfoJson) " +
                            "Values (@ftSignaturCreationUnitPLId,  @TimeStamp, @Url, @TseInfoJson);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftSignaturCreationUnitPL entity) => entity.ftSignaturCreationUnitPLId;
    }
}
