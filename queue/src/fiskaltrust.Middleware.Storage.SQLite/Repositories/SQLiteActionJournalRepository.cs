﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories
{
    public class SQLiteActionJournalRepository : AbstractSQLiteRepository<Guid, ftActionJournal>, IActionJournalRepository, IMiddlewareActionJournalRepository
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

        public async IAsyncEnumerable<ftActionJournal> GetByQueueItemId(Guid queueItemId)
        {
            var query = "Select * from ftActionJournal where ftQueueItemId = @queueItemId;";

            await foreach (var entry in DbConnection.Query<ftActionJournal>(query, new { queueItemId }, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
            {
                yield return entry;
            }
        }

        public async Task<ftActionJournal> GetWithLastTimestampAsync() 
            => await DbConnection.QueryFirstOrDefaultAsync<ftActionJournal>("Select * from ftActionJournal ORDER BY TimeStamp DESC LIMIT 1").ConfigureAwait(false);

        public IAsyncEnumerable<ftActionJournal> GetByPriorityAfterTimestampAsync(int lowerThanPriority, long fromTimestampInclusive) 
            => DbConnection.Query<ftActionJournal>($"SELECT * FROM ftActionJournal WHERE TimeStamp >= @from AND Priority <= @prio ORDER BY TimeStamp", new { from = fromTimestampInclusive, prio = lowerThanPriority }, buffered: false).ToAsyncEnumerable();
    }
}
