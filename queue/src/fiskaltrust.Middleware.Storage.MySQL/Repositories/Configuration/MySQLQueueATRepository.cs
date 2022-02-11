using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.Configuration
{
    public class MySQLQueueATRepository : AbstractMySQLRepository<Guid, ftQueueAT>, IConfigurationItemRepository<ftQueueAT>
    {
        public MySQLQueueATRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftQueueAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueueAT> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftQueueAT>("Select * from ftQueueAT where ftQueueATId = @QueueATId", new { QueueATId = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftQueueAT>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftQueueAT>("select * from ftQueueAT").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateAsync(ftQueueAT entity)
        {
            EntityUpdated(entity);
            var sql = "REPLACE INTO ftQueueAT " +
                      "(ftQueueATId, CashBoxIdentification, EncryptionKeyBase64, SignAll, ClosedSystemKind, ClosedSystemValue, ClosedSystemNote, LastSettlementMonth, LastSettlementMoment, LastSettlementQueueItemId, SSCDFailCount, SSCDFailMoment, SSCDFailQueueItemId, SSCDFailMessageSent, UsedFailedCount, UsedFailedMomentMin, UsedFailedMomentMax, UsedFailedQueueItemId, UsedMobileCount, UsedMobileMoment, UsedMobileQueueItemId, MessageCount, MessageMoment, LastSignatureHash, LastSignatureZDA, LastSignatureCertificateSerialNumber, ftCashNumerator, ftCashTotalizer,TimeStamp) " +
                      "Values ( @ftQueueATId, @CashBoxIdentification, @EncryptionKeyBase64, @SignAll, @ClosedSystemKind, @ClosedSystemValue, @ClosedSystemNote, @LastSettlementMonth, @LastSettlementMoment, @LastSettlementQueueItemId, @SSCDFailCount, @SSCDFailMoment, @SSCDFailQueueItemId, @SSCDFailMessageSent, @UsedFailedCount, @UsedFailedMomentMin, @UsedFailedMomentMax, @UsedFailedQueueItemId, @UsedMobileCount, @UsedMobileMoment, @UsedMobileQueueItemId, @MessageCount, @MessageMoment, @LastSignatureHash, @LastSignatureZDA, @LastSignatureCertificateSerialNumber, @ftCashNumerator, @ftCashTotalizer,@TimeStamp);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftQueueAT entity) => entity.ftQueueATId;
    }
}
