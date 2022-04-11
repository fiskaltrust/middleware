using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteSignaturCreationUnitMERepository : AbstractSQLiteRepository<Guid, ftSignaturCreationUnitME>, IConfigurationItemRepository<ftSignaturCreationUnitME>
    {
        public SQLiteSignaturCreationUnitMERepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftSignaturCreationUnitME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftSignaturCreationUnitME> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftSignaturCreationUnitME>("Select * from ftSignaturCreationUnitME where ftSignaturCreationUnitMEId = @SignaturCreationUnitME", new { SignaturCreationUnitDE = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftSignaturCreationUnitME>> GetAsync() => await DbConnection.QueryAsync<ftSignaturCreationUnitME>("select * from ftSignaturCreationUnitE").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitME entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftSignaturCreationUnitME " +
                            "(ftSignaturCreationUnitMEId, Url, TimeStamp, TseInfoJson, Mode, ModeConfigurationJson) " +
                            "Values (@ftSignaturCreationUnitMEId, @Url, @TimeStamp, @TseInfoJson, @Mode, @ModeConfigurationJson);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftSignaturCreationUnitME entity) => entity.ftSignaturCreationUnitMEId;
    }
}
