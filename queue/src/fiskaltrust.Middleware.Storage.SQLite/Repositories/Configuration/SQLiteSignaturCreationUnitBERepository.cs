using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteSignaturCreationUnitBERepository : AbstractSQLiteRepository<Guid, ftSignaturCreationUnitBE>, IConfigurationItemRepository<ftSignaturCreationUnitBE>
    {
        public SQLiteSignaturCreationUnitBERepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftSignaturCreationUnitBE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftSignaturCreationUnitBE> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftSignaturCreationUnitBE>("Select * from ftSignaturCreationUnitBE where ftSignaturCreationUnitBEId = @ftSignaturCreationUnitBEId", new { ftSignaturCreationUnitBEId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftSignaturCreationUnitBE>> GetAsync() => await DbConnection.QueryAsync<ftSignaturCreationUnitBE>("select * from ftSignaturCreationUnitBE").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitBE entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftSignaturCreationUnitBE " +
                            "(ftSignaturCreationUnitBEId, TimeStamp, Url) " +
                            "Values (@ftSignaturCreationUnitBEId,  @TimeStamp, @Url);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftSignaturCreationUnitBE entity) => entity.ftSignaturCreationUnitBEId;
    }
}