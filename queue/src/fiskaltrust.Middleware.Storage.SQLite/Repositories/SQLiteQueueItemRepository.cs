using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories
{
    public class SQLiteQueueItemRepository : AbstractSQLiteRepository<Guid, ftQueueItem>, IMiddlewareQueueItemRepository
    {
        public SQLiteQueueItemRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        protected override Guid GetIdForEntity(ftQueueItem entity) => entity.ftQueueItemId;

        public override void EntityUpdated(ftQueueItem entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueueItem> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftQueueItem>("Select * from ftQueueItem where ftQueueItemId = @QueueItemId", new { QueueItemId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftQueueItem>> GetAsync() => await DbConnection.QueryAsync<ftQueueItem>("select * from ftQueueItem").ConfigureAwait(false);

        public async Task<ftQueueItem> GetByQueueRowAsync(long queueRow) => await DbConnection.QueryFirstOrDefaultAsync<ftQueueItem>("Select * from ftQueueItem where ftQueueRow = @MyQueueRow", new { MyQueueRow = queueRow }).ConfigureAwait(false);

        public async Task InsertOrUpdateAsync(ftQueueItem entity)
        {
            EntityUpdated(entity);
            var sql = "INSERT OR REPLACE INTO ftQueueItem " +
                          "(ftQueueItemId, ftQueueId, ftQueueRow, ftQueueMoment, ftQueueTimeout, ftWorkMoment, ftDoneMoment, cbReceiptMoment, cbTerminalID, cbReceiptReference, country, version, request, requestHash, response, responseHash, TimeStamp) " +
                          "Values (@ftQueueItemId, @ftQueueId, @ftQueueRow, @ftQueueMoment, @ftQueueTimeout, @ftWorkMoment, @ftDoneMoment, @cbReceiptMoment, @cbTerminalID, @cbReceiptReference, @country, @version, @request, @requestHash, @response, @responseHash, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<ftQueueItem> GetByReceiptReferenceAsync(string cbReceiptReference, string cbTerminalId)
        {
            var query = "Select * from ftQueueItem where cbReceiptReference = @cbReceiptReference";
            if (!string.IsNullOrEmpty(cbTerminalId))
            {
                query += " AND cbTerminalId = @cbTerminalId";
            }
            await foreach (var entry in DbConnection.Query<ftQueueItem>(query, new { cbReceiptReference, cbTerminalId }, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
            {
                yield return entry;
            }
        }

        public async Task<ftQueueItem> GetClosestPreviousReceiptReferencesAsync(ftQueueItem ftQueueItem)
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(ftQueueItem.request);

            if (string.IsNullOrWhiteSpace(receiptRequest.cbPreviousReceiptReference) || string.IsNullOrWhiteSpace(ftQueueItem.cbReceiptReference) || receiptRequest.cbPreviousReceiptReference == ftQueueItem.cbReceiptReference)
            {
                return null;
            }

            var query = "SELECT *, json_extract(request, '$.ftReceiptCase') AS ReceiptCase FROM ftQueueItem WHERE ftQueueRow < @ftQueueRow AND cbReceiptReference = @cbPreviousReceiptReference "+
                "AND NOT (ReceiptCase & 0xFFFF = 0x0002 OR ReceiptCase & 0xFFFF = 0x0003 OR ReceiptCase & 0xFFFF = 0x0005 OR ReceiptCase & 0xFFFF = 0x0006 OR ReceiptCase & 0xFFFF = 0x0007) " +
                "AND response IS NOT NULL " +
                "ORDER BY timestamp DESC LIMIT 1;";
            return await DbConnection.QueryFirstOrDefaultAsync<ftQueueItem>(query, new { ftQueueItem.ftQueueRow, receiptRequest.cbPreviousReceiptReference }).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<ftQueueItem> GetQueueItemsAfterQueueItem(ftQueueItem ftQueueItem)
        {
            var query = "SELECT * FROM ftQueueItem WHERE ftQueueRow >= @ftQueueRow;";
            await foreach (var entry in DbConnection.Query<ftQueueItem>(query, new { ftQueueItem.ftQueueRow}, buffered: false).ToAsyncEnumerable())
            {
                yield return entry;
            }
        }

        public async IAsyncEnumerable<string> GetGroupedReceiptReferenceAsync(long? fromIncl, long? toIncl) 
        {
            var query = $"SELECT cbReceiptReference  FROM " +
                         "(SELECT cbReceiptReference, json_extract(request, '$.ftReceiptCase') AS ReceiptCase FROM ftQueueItem " +
                         "WHERE " +
                         (fromIncl.HasValue ? " ftQueueItem.TimeStamp >= @fromIncl " : " ") +
                         (fromIncl.HasValue && toIncl.HasValue ? " AND " : " ") +
                         (toIncl.HasValue ? " ftQueueItem.TimeStamp <= @toIncl  " : " ") +
                         (fromIncl.HasValue || toIncl.HasValue ? "AND " : " ") +
                         "NOT (ReceiptCase & 0xFFFF = 0x0002 OR ReceiptCase & 0xFFFF = 0x0003 OR ReceiptCase & 0xFFFF = 0x0005 OR ReceiptCase & 0xFFFF = 0x0006 OR ReceiptCase & 0xFFFF = 0x0007) " +
                         "AND response IS NOT NULL" +
                         ") GROUP BY cbReceiptReference; ";

            object obj = null;
            if (fromIncl.HasValue && toIncl.HasValue) {
                obj = new { fromIncl, toIncl };
            } else if (fromIncl.HasValue) {
                obj = new { fromIncl };
            } else if (toIncl.HasValue) {
                obj = new { toIncl };
            };
            await foreach (var entry in DbConnection.Query<string>(query, obj,  buffered: false).ToAsyncEnumerable())
            {
                yield return entry;
            }
        }

        public async IAsyncEnumerable<ftQueueItem> GetQueueItemsForReceiptReferenceAsync(string receiptReference)
        {
            var query = "SELECT *, json_extract(request, '$.ftReceiptCase') AS ReceiptCase FROM ftQueueItem WHERE cbReceiptReference = @receiptReference " +
                "AND NOT (ReceiptCase & 0xFFFF = 0x0002 OR ReceiptCase & 0xFFFF = 0x0003 OR ReceiptCase & 0xFFFF = 0x0005 OR ReceiptCase & 0xFFFF = 0x0006 OR ReceiptCase & 0xFFFF = 0x0007) " +
                "AND response IS NOT NULL " +
                "ORDER BY timestamp;";
            await foreach (var entry in DbConnection.Query<ftQueueItem>(query, new { receiptReference }, buffered: false).ToAsyncEnumerable())
            {
                yield return entry;
            }
        }
    }
}
