using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.DE.MasterData
{
    public class SQLitePosSystemMasterDataRepository : AbstractSQLiteRepository<Guid, PosSystemMasterData>, IMasterDataRepository<PosSystemMasterData>
    {
        private const string TABLE_NAME = "PosSystemMasterData";

        public SQLitePosSystemMasterDataRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(PosSystemMasterData entity) { }

        public async Task CreateAsync(PosSystemMasterData entity)
        {
            var sql = $"INSERT INTO {TABLE_NAME} (PosSystemId, Brand, Model, SoftwareVersion, BaseCurrency, Type) " +
                                               "Values (@PosSystemId, @Brand, @Model, @SoftwareVersion, @BaseCurrency, @Type);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        public async Task ClearAsync()
        {
            var sql = $"DELETE FROM {TABLE_NAME};";
            await DbConnection.ExecuteAsync(sql).ConfigureAwait(false);
        }

        public override async Task<IEnumerable<PosSystemMasterData>> GetAsync() => await DbConnection.QueryAsync<PosSystemMasterData>($"select * from {TABLE_NAME}").ConfigureAwait(false);

        protected override Guid GetIdForEntity(PosSystemMasterData entity) => entity.PosSystemId;

        public override Task<PosSystemMasterData> GetAsync(Guid id) => throw new NotImplementedException();
    }
}
