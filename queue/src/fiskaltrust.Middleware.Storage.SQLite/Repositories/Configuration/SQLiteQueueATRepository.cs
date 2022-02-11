using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteQueueATRepository : AbstractSQLiteRepository<Guid, ftQueueAT>, IConfigurationItemRepository<ftQueueAT>
    {
        public SQLiteQueueATRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftQueueAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueueAT> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftQueueAT>("Select * from ftQueueAT where ftQueueATId = @QueueATId", new { QueueATId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftQueueAT>> GetAsync() => await DbConnection.QueryAsync<ftQueueAT>("select * from ftQueueAT").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftQueueAT entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftQueueAT " +
                      "(ftQueueATId, CashBoxIdentification, EncryptionKeyBase64, SignAll, ClosedSystemKind, ClosedSystemValue, ClosedSystemNote, LastSettlementMonth, LastSettlementMoment, LastSettlementQueueItemId, SSCDFailCount, SSCDFailMoment, SSCDFailQueueItemId, SSCDFailMessageSent, UsedFailedCount, UsedFailedMomentMin, UsedFailedMomentMax, UsedFailedQueueItemId, UsedMobileCount, UsedMobileMoment, UsedMobileQueueItemId, MessageCount, MessageMoment, LastSignatureHash, LastSignatureZDA, LastSignatureCertificateSerialNumber, ftCashNumerator, ftCashTotalizer,TimeStamp) " +
                      "Values ( @ftQueueATId, @CashBoxIdentification, @EncryptionKeyBase64, @SignAll, @ClosedSystemKind, @ClosedSystemValue, @ClosedSystemNote, @LastSettlementMonth, @LastSettlementMoment, @LastSettlementQueueItemId, @SSCDFailCount, @SSCDFailMoment, @SSCDFailQueueItemId, @SSCDFailMessageSent, @UsedFailedCount, @UsedFailedMomentMin, @UsedFailedMomentMax, @UsedFailedQueueItemId, @UsedMobileCount, @UsedMobileMoment, @UsedMobileQueueItemId, @MessageCount, @MessageMoment, @LastSignatureHash, @LastSignatureZDA, @LastSignatureCertificateSerialNumber, @ftCashNumerator, @ftCashTotalizer,@TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftQueueAT entity) => entity.ftQueueATId;
    }
}
