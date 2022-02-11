using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteSignaturCreationUnitDERepository : AbstractSQLiteRepository<Guid, ftSignaturCreationUnitDE>, IConfigurationItemRepository<ftSignaturCreationUnitDE>
    {
        public SQLiteSignaturCreationUnitDERepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftSignaturCreationUnitDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftSignaturCreationUnitDE> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftSignaturCreationUnitDE>("Select * from ftSignaturCreationUnitDE where ftSignaturCreationUnitDEId = @SignaturCreationUnitDE", new { SignaturCreationUnitDE = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftSignaturCreationUnitDE>> GetAsync() => await DbConnection.QueryAsync<ftSignaturCreationUnitDE>("select * from ftSignaturCreationUnitDE").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitDE entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftSignaturCreationUnitDE " +
                            "(ftSignaturCreationUnitDEId, Url, TimeStamp, TseInfoJson, Mode, ModeConfigurationJson) " +
                            "Values (@ftSignaturCreationUnitDEId, @Url, @TimeStamp, @TseInfoJson, @Mode, @ModeConfigurationJson);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftSignaturCreationUnitDE entity) => entity.ftSignaturCreationUnitDEId;
    }
}
