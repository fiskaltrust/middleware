using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public class SQLiteQueueGRRepository : AbstractSQLiteRepository<Guid, ftQueueGR>, IConfigurationItemRepository<ftQueueGR>
    {
        public SQLiteQueueGRRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftQueueGR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueueGR> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftQueueGR>("Select * from ftQueueGR where ftQueueGRId = @QueueGRId", new { QueueGRId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftQueueGR>> GetAsync() => await DbConnection.QueryAsync<ftQueueGR>("select * from ftQueueGR").ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftQueueGR entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftQueueGR " +
                      "(ftQueueGRId, ftSignaturCreationUnitGRId, LastHash, LastSignature, CashBoxIdentification, SSCDFailCount, SSCDFailMoment, SSCDFailQueueItemId, UsedFailedCount, UsedFailedMomentMin, UsedFailedMomentMax, UsedFailedQueueItemId, InvoiceSeries, InvoiceNumerator, LastInvoiceMoment, LastInvoiceQueueItemId, LastInvoiceMark, TimeStamp) " +
                      "Values (@ftQueueGRId, @ftSignaturCreationUnitGRId, @LastHash, @LastSignature, @CashBoxIdentification, @SSCDFailCount, @SSCDFailMoment, @SSCDFailQueueItemId, @UsedFailedCount, @UsedFailedMomentMin, @UsedFailedMomentMax, @UsedFailedQueueItemId, @InvoiceSeries, @InvoiceNumerator, @LastInvoiceMoment, @LastInvoiceQueueItemId, @LastInvoiceMark, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftQueueGR entity) => entity.ftQueueGRId;
    }
}
