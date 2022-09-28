﻿using System;
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
    }
}
