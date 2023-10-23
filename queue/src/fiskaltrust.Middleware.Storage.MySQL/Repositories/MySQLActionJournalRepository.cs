using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.Repositories;
using System.Linq;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories
{
    public class MySQLActionJournalRepository : AbstractMySQLRepository<Guid, ftActionJournal>, IActionJournalRepository, IMiddlewareActionJournalRepository
    {
        public MySQLActionJournalRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftActionJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftActionJournal> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftActionJournal>("Select * from ftActionJournal where ftActionJournalId = @ActionJournalId", new { ActionJournalId = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftActionJournal>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftActionJournal>("select * from ftActionJournal").ConfigureAwait(false);
            }
        }

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
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        public async IAsyncEnumerable<ftActionJournal> GetByQueueItemId(Guid queueItemId)
        {
            var query = "SELECT * FROM ftActionJournal WHERE ftQueueItemId = @queueItemId;";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await foreach (var entry in connection.Query<ftActionJournal>(query, new { queueItemId }, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
                {
                    yield return entry;
                }
            }
        }

        public async Task<ftActionJournal> GetWithLastTimestampAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftActionJournal>("SELECT * FROM ftActionJournal ORDER BY TimeStamp DESC LIMIT 1").ConfigureAwait(false);
            }
        }

        public IAsyncEnumerable<ftActionJournal> GetByPriorityAfterTimestampAsync(int lowerThanPriority, long fromTimestampInclusive)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                return connection.Query<ftActionJournal>($"SELECT * FROM ftActionJournal WHERE TimeStamp >= @from AND Priority < @prio ORDER BY TimeStamp", new { from = fromTimestampInclusive, prio = lowerThanPriority }, buffered: false).ToAsyncEnumerable();
            }
        }
    }
}
