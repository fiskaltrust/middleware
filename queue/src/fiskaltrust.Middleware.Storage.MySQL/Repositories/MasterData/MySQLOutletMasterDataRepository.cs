using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.MasterData
{
    public class MySQLOutletMasterDataRepository : AbstractMySQLRepository<Guid, OutletMasterData>, IMasterDataRepository<OutletMasterData>
    {
        private const string TABLE_NAME = "OutletMasterData";

        public MySQLOutletMasterDataRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(OutletMasterData entity) { }

        public async Task CreateAsync(OutletMasterData entity)
        {
            var sql = $"INSERT INTO {TABLE_NAME} (OutletId, OutletName, Street, Zip, City, Country, VatId, LocationId) " +
                                               "Values (@OutletId, @OutletName, @Street, @Zip, @City, @Country, @VatId, @LocationId);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        public async Task ClearAsync()
        {
            var sql = $"TRUNCATE TABLE {TABLE_NAME};";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<OutletMasterData>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<OutletMasterData>($"select * from {TABLE_NAME}").ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(OutletMasterData entity) => entity.OutletId;

        public override Task<OutletMasterData> GetAsync(Guid id) => throw new NotImplementedException();
    }
}
