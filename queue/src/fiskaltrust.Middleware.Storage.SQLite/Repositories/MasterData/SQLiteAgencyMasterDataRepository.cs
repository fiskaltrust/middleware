using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.MasterData
{
    public class SQLiteAgencyMasterDataRepository : AbstractSQLiteRepository<Guid, AgencyMasterData>, IMasterDataRepository<AgencyMasterData>
    {
        private const string TABLE_NAME = "AgencyMasterData";

        public SQLiteAgencyMasterDataRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(AgencyMasterData entity) { }

        public async Task CreateAsync(AgencyMasterData entity)
        {
            var sql = $"INSERT INTO {TABLE_NAME} (AgencyId, Name, Street, Zip, City, Country, TaxId, VatId) " +
                                               "Values (@AgencyId, @Name, @Street, @Zip, @City, @Country, @TaxId, @VatId);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        public async Task ClearAsync()
        {
            var sql = $"DELETE FROM {TABLE_NAME};";
            await DbConnection.ExecuteAsync(sql).ConfigureAwait(false);
        }

        public override async Task<IEnumerable<AgencyMasterData>> GetAsync() => await DbConnection.QueryAsync<AgencyMasterData>($"select * from {TABLE_NAME}").ConfigureAwait(false);

        protected override Guid GetIdForEntity(AgencyMasterData entity) => entity.AgencyId;

        public override Task<AgencyMasterData> GetAsync(Guid id) => throw new NotImplementedException();
    }
}
