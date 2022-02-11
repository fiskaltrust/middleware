using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories
{
    public class SQLiteActionJournalRepository : AbstractSQLiteRepository<Guid, ftActionJournal>, IActionJournalRepository
    {
        public SQLiteActionJournalRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftActionJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftActionJournal> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftActionJournal>("Select * from ftActionJournal where ftActionJournalId = @ActionJournalId", new { ActionJournalId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftActionJournal>> GetAsync() => await DbConnection.QueryAsync<ftActionJournal>("select * from ftActionJournal").ConfigureAwait(false);

        protected override Guid GetIdForEntity(ftActionJournal entity) => entity.ftActionJournalId;

        public async Task InsertAsync(ftActionJournal entity)
        {
            if (await GetAsync(GetIdForEntity(entity)).ConfigureAwait(false) != null)
            {
                throw new Exception("Already exists");
            }
            EntityUpdated(entity);
            var sql = "INSERT INTO ftActionJournal " +
                      "(ftActionJournalId, ftQueueId, ftQueueItemId, Moment, Priority, Type, Message, DataBase64, DataJson, TimeStamp) " +
                      "Values (@ftActionJournalId, @ftQueueId, @ftQueueItemId, @Moment, @Priority, @Type, @Message, @DataBase64, @DataJson, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }
    }
}
