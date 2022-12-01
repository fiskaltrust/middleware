using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Storage.Base.Extensions;

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

        public async Task Insert(IAsyncEnumerable<ftQueueItem> ftQueueItems)
        {
            using (var transaction = DbConnection.BeginTransaction())
            {
                var command = DbConnection.CreateCommand();
                command.CommandText = @"INSERT OR REPLACE INTO ftQueueItem " +
                          "(ftQueueItemId, ftQueueId, ftQueueRow, ftQueueMoment, ftQueueTimeout, ftWorkMoment, ftDoneMoment, cbReceiptMoment, cbTerminalID, cbReceiptReference, country, version, request, requestHash, response, responseHash, TimeStamp) " +
                          "Values (@ftQueueItemId, @ftQueueId, @ftQueueRow, @ftQueueMoment, @ftQueueTimeout, @ftWorkMoment, @ftDoneMoment, @cbReceiptMoment, @cbTerminalID, @cbReceiptReference, @country, @version, @request, @requestHash, @response, @responseHash, @TimeStamp);";

                var ftQueueItemId = command.CreateParameter();
                ftQueueItemId.ParameterName = "@ftQueueItemId";
                command.Parameters.Add(ftQueueItemId);
                var ftQueueId = command.CreateParameter();
                ftQueueId.ParameterName = "@ftQueueId";
                command.Parameters.Add(ftQueueId);
                var ftQueueRow = command.CreateParameter();
                ftQueueRow.ParameterName = "@ftQueueRow";
                command.Parameters.Add(ftQueueRow);
                var ftQueueMoment = command.CreateParameter();
                ftQueueMoment.ParameterName = "@ftQueueMoment";
                command.Parameters.Add(ftQueueMoment);
                var ftQueueTimeout = command.CreateParameter();
                ftQueueTimeout.ParameterName = "@ftQueueTimeout";
                command.Parameters.Add(ftQueueTimeout);
                var ftWorkMoment = command.CreateParameter();
                ftWorkMoment.ParameterName = "@ftWorkMoment";
                command.Parameters.Add(ftWorkMoment);
                var ftDoneMoment = command.CreateParameter();
                ftDoneMoment.ParameterName = "@ftDoneMoment";
                command.Parameters.Add(ftDoneMoment);
                var cbReceiptMoment = command.CreateParameter();
                cbReceiptMoment.ParameterName = "@cbReceiptMoment";
                command.Parameters.Add(cbReceiptMoment);
                var cbTerminalID = command.CreateParameter();
                cbTerminalID.ParameterName = "@cbTerminalID";
                command.Parameters.Add(cbTerminalID);
                var cbReceiptReference = command.CreateParameter();
                cbReceiptReference.ParameterName = "@cbReceiptReference";
                command.Parameters.Add(cbReceiptReference);
                var country = command.CreateParameter();
                country.ParameterName = "@country";
                command.Parameters.Add(country);
                var version = command.CreateParameter();
                version.ParameterName = "@version";
                command.Parameters.Add(version);
                var request = command.CreateParameter();
                request.ParameterName = "@request";
                command.Parameters.Add(request);
                var requestHash = command.CreateParameter();
                requestHash.ParameterName = "@requestHash";
                command.Parameters.Add(requestHash);
                var response = command.CreateParameter();
                response.ParameterName = "@response";
                command.Parameters.Add(response);
                var responseHash = command.CreateParameter();
                responseHash.ParameterName = "@responseHash";
                command.Parameters.Add(responseHash);
                var TimeStamp = command.CreateParameter();
                TimeStamp.ParameterName = "@TimeStamp";
                command.Parameters.Add(TimeStamp);

                await foreach (var item in ftQueueItems)
                {
                    ftQueueItemId.Value = item.ftQueueItemId;
                    ftQueueId.Value = item.ftQueueId;
                    ftQueueRow.Value = item.ftQueueRow;
                    ftQueueMoment.Value = item.ftQueueMoment;
                    ftQueueTimeout.Value = item.ftQueueTimeout;
                    ftWorkMoment.Value = item.ftWorkMoment;
                    ftDoneMoment.Value = item.ftDoneMoment;
                    cbReceiptMoment.Value = item.cbReceiptMoment;
                    cbTerminalID.Value = item.cbTerminalID;
                    cbReceiptReference.Value = item.cbReceiptReference;
                    country.Value = item.country;
                    version.Value = item.version;
                    request.Value = item.request;
                    requestHash.Value = item.requestHash;
                    response.Value = item.response;
                    responseHash.Value = item.responseHash;
                    TimeStamp.Value = item.TimeStamp;
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
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

        public async IAsyncEnumerable<ftQueueItem> GetPreviousReceiptReferencesAsync(ftQueueItem ftQueueItem)
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(ftQueueItem.request);

            if (!receiptRequest.IncludeInReferences() || (string.IsNullOrWhiteSpace(receiptRequest.cbPreviousReceiptReference) && string.IsNullOrWhiteSpace(ftQueueItem.cbReceiptReference)))
            {
                yield break;
            }
            var query = "SELECT *, json_extract(request, '$.ftReceiptCase') AS ReceiptCase FROM ftQueueItem WHERE ftQueueRow < @ftQueueRow AND (cbReceiptReference = @cbPreviousReceiptReference OR cbReceiptReference = @cbReceiptReference) AND NOT (ReceiptCase & 0xFFFF = 0x0002 OR ReceiptCase & 0xFFFF = 0x0003 OR ReceiptCase & 0xFFFF = 0x0005 OR ReceiptCase & 0xFFFF = 0x0006 OR ReceiptCase & 0xFFFF = 0x0007)";

            await foreach (var entry in DbConnection.Query<ftQueueItem>(query, new { ftQueueItem.ftQueueRow, receiptRequest.cbPreviousReceiptReference, ftQueueItem.cbReceiptReference }, buffered: false).ToAsyncEnumerable())
            {
                yield return entry;
            }
        }

        public async IAsyncEnumerable<ftQueueItem> GetQueueItemsAfterQueueItem(ftQueueItem ftQueueItem)
        {
            var query = "SELECT * FROM ftQueueItem WHERE ftQueueRow >= @ftQueueRow;";
            await foreach (var entry in DbConnection.Query<ftQueueItem>(query, new { ftQueueItem.ftQueueRow}, buffered: false).ToAsyncEnumerable())
            {
                yield return entry;
            }
        }
    }
}
