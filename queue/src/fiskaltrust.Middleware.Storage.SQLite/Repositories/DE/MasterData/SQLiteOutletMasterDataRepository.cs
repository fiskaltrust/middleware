using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.DE.MasterData
{
    public class SQLiteOutletMasterDataRepository : AbstractSQLiteRepository<Guid, OutletMasterData>, IMasterDataRepository<OutletMasterData>
    {
        private const string TABLE_NAME = "OutletMasterData";

        public SQLiteOutletMasterDataRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(OutletMasterData entity) { }

        public async Task CreateAsync(OutletMasterData entity)
        {
            var sql = $"INSERT INTO {TABLE_NAME} (OutletId, OutletName, Street, Zip, City, Country, VatId) " +
                                               "Values (@OutletId, @OutletName, @Street, @Zip, @City, @Country, @VatId);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        public async Task ClearAsync()
        {
            var sql = $"DELETE FROM {TABLE_NAME};";
            await DbConnection.ExecuteAsync(sql).ConfigureAwait(false);
        }

        public override async Task<IEnumerable<OutletMasterData>> GetAsync() => await DbConnection.QueryAsync<OutletMasterData>($"select * from {TABLE_NAME}").ConfigureAwait(false);

        protected override Guid GetIdForEntity(OutletMasterData entity) => entity.OutletId;

        public override Task<OutletMasterData> GetAsync(Guid id) => throw new NotImplementedException();
    }
}
