using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.DE.MasterData
{
    public class MySQLAccountMasterDataRepository : AbstractMySQLRepository<Guid, AccountMasterData>, IMasterDataRepository<AccountMasterData>
    {
        private const string TABLE_NAME = "AccountMasterData";

        public MySQLAccountMasterDataRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(AccountMasterData entity) { }

        public async Task CreateAsync(AccountMasterData entity)
        {
            var sql = $"INSERT INTO {TABLE_NAME} (AccountId, AccountName, Street, Zip, City, Country, TaxId, VatId) " +
                                               "Values (@AccountId, @AccountName, @Street, @Zip, @City, @Country, @TaxId, @VatId);";
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

        public override async Task<IEnumerable<AccountMasterData>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<AccountMasterData>($"select * from {TABLE_NAME}").ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(AccountMasterData entity) => entity.AccountId;

        public override Task<AccountMasterData> GetAsync(Guid id) => throw new NotImplementedException();
    }
}
