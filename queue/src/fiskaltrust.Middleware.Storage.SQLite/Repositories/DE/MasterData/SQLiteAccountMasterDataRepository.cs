using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.DE.MasterData
{
    public class SQLiteAccountMasterDataRepository : AbstractSQLiteRepository<Guid, AccountMasterData>, IMasterDataRepository<AccountMasterData>
    {
        private const string TABLE_NAME = "AccountMasterData";

        public SQLiteAccountMasterDataRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(AccountMasterData entity) { }

        public async Task CreateAsync(AccountMasterData entity)
        {
            var sql = $"INSERT INTO {TABLE_NAME} (AccountId, AccountName, Street, Zip, City, Country, TaxId, VatId) " +
                                               "Values (@AccountId, @AccountName, @Street, @Zip, @City, @Country, @TaxId, @VatId);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        public async Task ClearAsync()
        {
            var sql = $"DELETE FROM {TABLE_NAME};";
            await DbConnection.ExecuteAsync(sql).ConfigureAwait(false);
        }

        public override async Task<IEnumerable<AccountMasterData>> GetAsync() => await DbConnection.QueryAsync<AccountMasterData>($"select * from {TABLE_NAME}").ConfigureAwait(false);

        protected override Guid GetIdForEntity(AccountMasterData entity) => entity.AccountId;

        public override Task<AccountMasterData> GetAsync(Guid id) => throw new NotImplementedException();
    }
}
