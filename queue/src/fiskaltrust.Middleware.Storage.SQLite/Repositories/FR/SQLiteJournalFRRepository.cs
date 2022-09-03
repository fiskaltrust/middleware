using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.FR
{
    public class SQLiteJournalFRRepository : AbstractSQLiteRepository<Guid, ftJournalFR>, IJournalFRRepository, IMiddlewareJournalFRRepository
    {
        public SQLiteJournalFRRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftJournalFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftJournalFR> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftJournalFR>("Select * from ftJournalFR where ftJournalFRId = @JournalFRId", new { JournalFRId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftJournalFR>> GetAsync() => await DbConnection.QueryAsync<ftJournalFR>("select * from ftJournalFR").ConfigureAwait(false);

        public async Task InsertAsync(ftJournalFR journal)
        {
            if (await GetAsync(GetIdForEntity(journal)).ConfigureAwait(false) != null)
            {
                throw new Exception("Already exists");
            }

            EntityUpdated(journal);
            var sql = "INSERT INTO ftJournalFR " +
                          "(ftJournalFRId, JWT, JsonData, ReceiptType, Number, ftQueueItemId,ftQueueId,TimeStamp) " +
                          "Values (@ftJournalFRId, @JWT, @JsonData, @ReceiptType, @Number, @ftQueueItemId, @ftQueueId, @TimeStamp);";

            await DbConnection.ExecuteAsync(sql, journal).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftJournalFR entity) => entity.ftJournalFRId;

        public async Task<ftJournalFR> GetWithLastTimestampAsync() => await DbConnection.QueryFirstOrDefaultAsync<ftJournalFR>("Select * from ftJournalFR ORDER BY TimeStamp DESC LIMIT 1").ConfigureAwait(false);

        public async IAsyncEnumerable<ftJournalFR> GetProcessedCopyReceiptsAsync()
        {
            foreach (var item in await DbConnection.QueryAsync<ftJournalFR>("select * from ftJournalFR where ReceiptType = 'C'"))
            {
                yield return item;
            }
        }
    }
}
