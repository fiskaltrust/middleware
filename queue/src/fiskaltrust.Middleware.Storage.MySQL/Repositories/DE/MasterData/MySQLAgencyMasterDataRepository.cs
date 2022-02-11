using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.DE.MasterData
{
    public class MySQLAgencyMasterDataRepository : AbstractMySQLRepository<Guid, AgencyMasterData>, IMasterDataRepository<AgencyMasterData>
    {
        private const string TABLE_NAME = "AgencyMasterData";

        public MySQLAgencyMasterDataRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(AgencyMasterData entity) { }

        public async Task CreateAsync(AgencyMasterData entity)
        {
            var sql = $"INSERT INTO {TABLE_NAME} (AgencyId, Name, Street, Zip, City, Country, TaxId, VatId) " +
                                               "Values (@AgencyId, @Name, @Street, @Zip, @City, @Country, @TaxId, @VatId);";
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

        public override async Task<IEnumerable<AgencyMasterData>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<AgencyMasterData>($"select * from {TABLE_NAME}").ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(AgencyMasterData entity) => entity.AgencyId;

        public override Task<AgencyMasterData> GetAsync(Guid id) => throw new NotImplementedException();
    }
}
