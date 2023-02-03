using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.DE.MasterData
{
    public class MySQLPosSystemMasterDataRepository : AbstractMySQLRepository<Guid, PosSystemMasterData>, IMasterDataRepository<PosSystemMasterData>
    {
        private const string TABLE_NAME = "PosSystemMasterData";

        public MySQLPosSystemMasterDataRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(PosSystemMasterData entity) { }

        public async Task CreateAsync(PosSystemMasterData entity)
        {
            var sql = $"INSERT INTO {TABLE_NAME} (PosSystemId, Brand, Model, SoftwareVersion, BaseCurrency, Type) " +
                                               "Values (@PosSystemId, @Brand, @Model, @SoftwareVersion, @BaseCurrency, @Type);";
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

        public override async Task<IEnumerable<PosSystemMasterData>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<PosSystemMasterData>($"select * from {TABLE_NAME}").ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(PosSystemMasterData entity) => entity.PosSystemId;

        public override Task<PosSystemMasterData> GetAsync(Guid id) => throw new NotImplementedException();
    }
}
