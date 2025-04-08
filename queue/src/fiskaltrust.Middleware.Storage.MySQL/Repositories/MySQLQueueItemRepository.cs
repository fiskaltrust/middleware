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
                      "(ftQueueItemId, ftQueueId, ftQueueRow, ftQueueMoment, ftQueueTimeout, ftWorkMoment, ftDoneMoment, cbReceiptMoment, cbTerminalID, cbReceiptReference, country, version, request, requestHash, response, responseHash, TimeStamp, ProcessingVersion) " +
                      "Values (@ftQueueItemId, @ftQueueId, @ftQueueRow, @ftQueueMoment, @ftQueueTimeout, @ftWorkMoment, @ftDoneMoment, @cbReceiptMoment, @cbTerminalID, @cbReceiptReference, @country, @version, @request, @requestHash, @response, @responseHash, @TimeStamp, @ProcessingVersion);";
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

        public async IAsyncEnumerable<string> GetGroupedReceiptReferenceAsync(long? fromIncl, long? toIncl)
        {
            var query = $"SELECT cbReceiptReference  FROM " +
                         "(SELECT cbReceiptReference FROM ftQueueItem " +
                         "WHERE " +
                         (fromIncl.HasValue ? " ftQueueItem.TimeStamp >= @fromIncl " : " ") +
                         (fromIncl.HasValue && toIncl.HasValue ? " AND " : " ") +
                         (toIncl.HasValue ? " ftQueueItem.TimeStamp <= @toIncl  " : " ") +
                         (fromIncl.HasValue || toIncl.HasValue ? "AND " : " ") +
                         "response IS NOT NULL" +
                        ") AS groupedReferences GROUP BY cbReceiptReference; ";

            object obj = null;
            if (fromIncl.HasValue && toIncl.HasValue)
            {
                obj = new { fromIncl, toIncl };
            }
            else if (fromIncl.HasValue)
            {
                obj = new { fromIncl };
            }
            else if (toIncl.HasValue)
            {
                obj = new { toIncl };
            };
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await foreach (var entry in connection.Query<string>(query, obj, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
                {
                    yield return entry;
                }
            }
        }

        public async IAsyncEnumerable<ftQueueItem> GetQueueItemsForReceiptReferenceAsync(string receiptReference)
        {
            var query = "SELECT * FROM ftQueueItem WHERE cbReceiptReference = @receiptReference " +
                "AND response IS NOT NULL " +
                "ORDER BY timestamp;";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await foreach (var entry in connection.Query<ftQueueItem>(query, new { receiptReference }, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
                {
                    var request = JsonConvert.DeserializeObject<ReceiptRequest>(entry.request);
                    if (request.IncludeInReferences())
                    {
                        yield return entry;
                    }
                }
            }
        }

        public async Task<ftQueueItem> GetClosestPreviousReceiptReferencesAsync(ftQueueItem ftQueueItem)
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(ftQueueItem.request);

            if (string.IsNullOrWhiteSpace(receiptRequest.cbPreviousReceiptReference) || string.IsNullOrWhiteSpace(ftQueueItem.cbReceiptReference) || receiptRequest.cbPreviousReceiptReference == ftQueueItem.cbReceiptReference)
            {
                return null;
            }
            var query = "SELECT * FROM ftQueueItem WHERE ftQueueRow < @ftQueueRow AND cbReceiptReference = @cbPreviousReceiptReference " +
                            "AND response IS NOT NULL " +
                            "ORDER BY timestamp DESC LIMIT 1;";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var entry =  await connection.QueryFirstOrDefaultAsync<ftQueueItem>(query, new { ftQueueItem.ftQueueRow, receiptRequest.cbPreviousReceiptReference }).ConfigureAwait(false);
                if (entry == null)
                {
                    return null;
                }
                var request = JsonConvert.DeserializeObject<ReceiptRequest>(entry.request);
                if (request.IncludeInReferences())
                {
                    return await Task.FromResult(entry);
                }
                else
                {
                    return null;
                }
            }
        }

        public async Task<int> CountAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM ftQueueItem").ConfigureAwait(false);
            }
        }

        public async Task<ftQueueItem> GetLastQueueItemAsync()
        {
            var query = "SELECT * FROM ftQueueItem " +
                            "ORDER BY TimeStamp DESC LIMIT 1;";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftQueueItem>(query).ConfigureAwait(false);
            }
        }
    }
}
