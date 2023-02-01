using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteSignaturCreationUnitESRepository : AbstractSQLiteRepository<Guid, ftSignaturCreationUnitES>, IConfigurationItemRepository<ftSignaturCreationUnitES>
    {
        public SQLiteSignaturCreationUnitESRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftSignaturCreationUnitES entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftSignaturCreationUnitES> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftSignaturCreationUnitES>("Select * from ftSignaturCreationUnitES where ftSignaturCreationUnitESId = @SignaturCreationUnitES", new { SignaturCreationUnitES = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftSignaturCreationUnitES>> GetAsync() => await DbConnection.QueryAsync<ftSignaturCreationUnitES>("select * from ftSignaturCreationUnitES").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitES entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftSignaturCreationUnitES " +
                            "(ftSignaturCreationUnitESId, Url, TimeStamp) " +
                            "Values (@ftSignaturCreationUnitESId, @Url, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftSignaturCreationUnitES entity) => entity.ftSignaturCreationUnitESId;
    }
}
