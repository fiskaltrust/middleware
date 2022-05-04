using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.ME
{
    public class SQLiteJournalMERepository : AbstractSQLiteRepository<Guid, ftJournalME>, IJournalMERepository
    {
        public SQLiteJournalMERepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }
        public override void EntityUpdated(ftJournalME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;
        public override async Task<ftJournalME> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftJournalME>("Select * from ftJournalME where ftJournalMEId = @JournalMEId", new { JournalMEId = id }).ConfigureAwait(false);
        public override async Task<IEnumerable<ftJournalME>> GetAsync() => await DbConnection.QueryAsync<ftJournalME>("select * from ftJournalME").ConfigureAwait(false);
        public async Task<ftJournalME> GetLastEntryAsync() => await DbConnection.QueryFirstOrDefaultAsync<ftJournalME>("Select * from ftJournalME order by TimeStamp desc limit 1").ConfigureAwait(false);
        public async Task InsertAsync(ftJournalME journal)
        {
            if (await GetAsync(GetIdForEntity(journal)).ConfigureAwait(false) != null)
            {
                throw new Exception("Already exists");
            }
            EntityUpdated(journal);
            var sql = "INSERT INTO ftJournalME " +
                      "(ftJournalMEId, cbReference, ftInvoiceNumber, ftOrdinalNumber, ftQueueItemId, ftQueueId, TimeStamp) " +
                      "Values (@ftJournalMEId, @cbReference, @ftInvoiceNumber, @ftOrdinalNumber, @ftQueueItemId, @ftQueueId, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, journal).ConfigureAwait(false);
        }
        protected override Guid GetIdForEntity(ftJournalME entity) => entity.ftJournalMEId;
    }
}
