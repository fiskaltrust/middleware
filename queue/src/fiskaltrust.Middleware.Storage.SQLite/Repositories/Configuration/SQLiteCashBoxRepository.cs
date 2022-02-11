using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteCashBoxRepository : AbstractSQLiteRepository<Guid, ftCashBox>, IConfigurationItemRepository<ftCashBox>
    {
        public SQLiteCashBoxRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftCashBox entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftCashBox> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftCashBox>("Select * from ftCashBox where ftCashBoxId = @CashBoxId", new { CashBoxId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftCashBox>> GetAsync() => await DbConnection.QueryAsync<ftCashBox>("select * from ftCashBox").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftCashBox entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftCashBox " +
                "(ftCashBoxId, TimeStamp) " +
                "Values (@ftCashBoxId, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftCashBox entity) => entity.ftCashBoxId;
    }
}
