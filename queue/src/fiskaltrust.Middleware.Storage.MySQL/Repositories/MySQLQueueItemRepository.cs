using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Base.Extensions;
using fiskaltrust.storage.V0;
using MySqlConnector;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories
{
    public class MySQLQueueItemRepository : AbstractMySQLRepository<Guid, ftQueueItem>, IMiddlewareQueueItemRepository
    {
        public MySQLQueueItemRepository(string connectionString) : base(connectionString) { }

        protected override Guid GetIdForEntity(ftQueueItem entity) => entity.ftQueueItemId;

        public override void EntityUpdated(ftQueueItem entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueueItem> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftQueueItem>("Select * from ftQueueItem where ftQueueItemId = @QueueItemId", new { QueueItemId = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftQueueItem>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftQueueItem>("select * from ftQueueItem").ConfigureAwait(false);
            }
        }

        public async Task<ftQueueItem> GetByQueueRowAsync(long queueRow)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftQueueItem>("Select * from ftQueueItem where ftQueueRow = @MyQueueRow", new { MyQueueRow = queueRow }).ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateAsync(ftQueueItem entity)
        {
            EntityUpdated(entity);
            var sql = "REPLACE INTO ftQueueItem " +
                          "(ftQueueItemId, ftQueueId, ftQueueRow, ftQueueMoment, ftQueueTimeout, ftWorkMoment, ftDoneMoment, cbReceiptMoment, cbTerminalID, cbReceiptReference, country, version, request, requestHash, response, responseHash, TimeStamp) " +
                          "Values (@ftQueueItemId, @ftQueueId, @ftQueueRow, @ftQueueMoment, @ftQueueTimeout, @ftWorkMoment, @ftDoneMoment, @cbReceiptMoment, @cbTerminalID, @cbReceiptReference, @country, @version, @request, @requestHash, @response, @responseHash, @TimeStamp);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        public async IAsyncEnumerable<ftQueueItem> GetByReceiptReferenceAsync(string cbReceiptReference, string cbTerminalId)
        {
            var query = "Select * from ftQueueItem where cbReceiptReference = @cbReceiptReference";
            if (!string.IsNullOrEmpty(cbTerminalId))
            {
                query += " AND cbTerminalId = @cbTerminalId";
            }
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await foreach (var entry in connection.Query<ftQueueItem>(query, new { cbReceiptReference, cbTerminalId }, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
                {
                    yield return entry;
                }
            }
        }

        public async IAsyncEnumerable<ftQueueItem> GetPreviousReceiptReferencesAsync(ftQueueItem ftQueueItem)
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(ftQueueItem.request);

            if (!receiptRequest.IncludeInReferences() || (string.IsNullOrWhiteSpace(receiptRequest.cbPreviousReceiptReference) && string.IsNullOrWhiteSpace(ftQueueItem.cbReceiptReference)))
            {
                yield break;
            }

            var query = "SELECT * FROM ftQueueItem WHERE ftQueueRow < @ftQueueRow AND (cbReceiptReference = @cbPreviousReceiptReference OR cbReceiptReference = @cbReceiptReference)";

            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync();
                await foreach (var entry in connection.Query<ftQueueItem>(query, new { ftQueueItem.ftQueueRow, receiptRequest.cbPreviousReceiptReference, ftQueueItem.cbReceiptReference }, buffered: false).ToAsyncEnumerable())
                {

                    if (JsonConvert.DeserializeObject<ReceiptRequest>(entry.request).IncludeInReferences())
                    {
                        yield return entry;
                    }
                }
            }
        }

        public async IAsyncEnumerable<ftQueueItem> GetQueueItemsAfterQueueItem(ftQueueItem ftQueueItem)
        {
            var query = "SELECT * FROM ftQueueItem WHERE ftQueueRow >= @ftQueueRow";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await foreach (var entry in connection.Query<ftQueueItem>(query, new { ftQueueItem.ftQueueRow}, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
                {
                    yield return entry;
                }
            }
        }
        public IAsyncEnumerable<ftQueueItem> GetQueueItemsForReceiptReference(string receiptReference) => throw new NotImplementedException();
        public IAsyncEnumerable<string> GetGroupedReceiptReference(long from, long to) => throw new NotImplementedException();
        public Task<ftQueueItem> GetFirstPreviousReceiptReferencesAsync(ftQueueItem ftQueueItem) => throw new NotImplementedException();
    }
}
