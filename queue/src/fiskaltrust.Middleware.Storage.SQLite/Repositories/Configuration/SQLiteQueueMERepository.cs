using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteQueueMERepository : AbstractSQLiteRepository<Guid, ftQueueME>, IConfigurationItemRepository<ftQueueME>
    {
        public SQLiteQueueMERepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftQueueME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueueME> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftQueueME>("Select * from ftQueueME where ftQueueMEId = @QueueMEId", new { QueueMEId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftQueueME>> GetAsync() => await DbConnection.QueryAsync<ftQueueME>("select * from ftQueueME").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftQueueME entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftQueueME " +
                      "(ftQueueMEId, ftSignaturCreationUnitMEId, LastHash, CashBoxIdentification, SSCDFailCount, SSCDFailMoment, SSCDFailQueueItemId, UsedFailedCount, UsedFailedMomentMin, UsedFailedMomentMax, UsedFailedQueueItemId, TimeStamp, DailyClosingNumber, " +
                      "IssuerTIN, BusinUnitCode, TCRIntID, SoftCode, MaintainerCode, ValidFrom, ValidTo, Type, TCRCode) " +
                      "Values (@ftQueueDEId, @ftSignaturCreationUnitDEId, @LastHash, @CashBoxIdentification, @SSCDFailCount, @SSCDFailMoment, @SSCDFailQueueItemId, @UsedFailedCount, @UsedFailedMomentMin, @UsedFailedMomentMax, @UsedFailedQueueItemId, @TimeStamp, @DailyClosingNumber, " +
                      "@IssuerTIN, @BusinUnitCode, @TCRIntID, @SoftCode, @MaintainerCode, @ValidFrom, @ValidTo, @Type, @TCRCode); ";
                       await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftQueueME entity) => entity.ftQueueMEId;
    }
}
