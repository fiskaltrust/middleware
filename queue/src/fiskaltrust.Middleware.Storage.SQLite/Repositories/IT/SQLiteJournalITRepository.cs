using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.ME
{
    public class SQLiteJournalITRepository : AbstractSQLiteRepository<Guid, ftJournalIT>, IJournalITRepository
    {
        public SQLiteJournalITRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }
        
        public override void EntityUpdated(ftJournalIT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;
       
        public override async Task<ftJournalIT> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftJournalIT>("Select * from ftJournalIT where ftJournalITId = @JournalITId", new { JournalITId = id }).ConfigureAwait(false);
       
        public override async Task<IEnumerable<ftJournalIT>> GetAsync() => await DbConnection.QueryAsync<ftJournalIT>("select * from ftJournalIT").ConfigureAwait(false);
        
        public async Task InsertAsync(ftJournalIT journal)
        {
            if (await GetAsync(GetIdForEntity(journal)).ConfigureAwait(false) != null)
            {
                throw new Exception("Already exists");
            }
            EntityUpdated(journal);
            var sql = "INSERT INTO ftJournalIT " +
                      "(ftJournalITId, ftQueueItemId, ftQueueId, ftSignaturCreationUnitITId, ReceiptNumber, ZRepNumber, JournalType, cbReceiptReference, DataJson, ReceiptDateTime, TimeStamp) " +
                      "Values (@ftJournalITId, @ftQueueItemId, @ftQueueId, @ftSignaturCreationUnitITId, @ReceiptNumber, @ZRepNumber, @JournalType, @cbReceiptReference, @DataJson, @ReceiptDateTime, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, journal).ConfigureAwait(false);
        }
    
        protected override Guid GetIdForEntity(ftJournalIT entity) => entity.ftJournalITId;       
    }
}
