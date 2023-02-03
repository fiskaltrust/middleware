using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.Configuration
{
    public class MySQLSignaturCreationUnitMERepository : AbstractMySQLRepository<Guid, ftSignaturCreationUnitME>, IConfigurationItemRepository<ftSignaturCreationUnitME>
    {
        public MySQLSignaturCreationUnitMERepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftSignaturCreationUnitME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftSignaturCreationUnitME> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftSignaturCreationUnitME>("Select * from ftSignaturCreationUnitME where ftSignaturCreationUnitMEId = @SignaturCreationUnitDE", new { SignaturCreationUnitME = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftSignaturCreationUnitME>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftSignaturCreationUnitME>("select * from ftSignaturCreationUnitME").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitME entity)
        {
            EntityUpdated(entity);
            var sql = "REPLACE INTO ftSignaturCreationUnitME " +
                            "(ftSignaturCreationUnitMEId, TimeStamp, TseInfoJson, IssuerTin, BusinessUnitCode, TcrIntId, SoftwareCode, MaintainerCode, ValidFrom, ValidTo, EnuType, TcrCode) " +
                            "Values (@ftSignaturCreationUnitMEId,  @TimeStamp, @TseInfoJson, @IssuerTin, @BusinessUnitCode, @TcrIntId, @SoftwareCode, @MaintainerCode, @ValidFrom, @ValidTo, @EnuType, @TcrCode);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftSignaturCreationUnitME entity) => entity.ftSignaturCreationUnitMEId;
    }
}
