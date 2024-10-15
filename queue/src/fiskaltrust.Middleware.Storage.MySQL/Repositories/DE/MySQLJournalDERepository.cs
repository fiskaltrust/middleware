using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.DE
{
    public class MySQLJournalDERepository : AbstractMySQLRepository<Guid, ftJournalDE>, IJournalDERepository, IMiddlewareJournalDERepository
    {
        public MySQLJournalDERepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftJournalDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftJournalDE> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftJournalDE>("Select * from ftJournalDE where ftJournalDEId = @JournalDEId", new { JournalDEId = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftJournalDE>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftJournalDE>("select * from ftJournalDE").ConfigureAwait(false);
            }
        }

        public async IAsyncEnumerable<ftJournalDE> GetByFileName(string fileName)
        {
            var query = $"SELECT * FROM {typeof(ftJournalDE).Name} WHERE FileName = @fileName";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await foreach (var entry in connection.Query<ftJournalDE>(query, new { fileName = fileName }, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
                {
                    yield return entry;
                }
            }
        }

        public async Task InsertAsync(ftJournalDE journal)
        {
            if (await GetAsync(GetIdForEntity(journal)).ConfigureAwait(false) != null)
            {
                throw new Exception("Already exists");
            }

            EntityUpdated(journal);
            var sql = "INSERT INTO ftJournalDE " +
                      "(ftJournalDEId, Number, FileName, FileExtension, FileContentBase64, ftQueueItemId, ftQueueId, TimeStamp) " +
                      "Values (@ftJournalDEId, @Number, @FileName, @FileExtension, @FileContentBase64, @ftQueueItemId, @ftQueueId, @TimeStamp);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, journal).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftJournalDE entity) => entity.ftJournalDEId;

        public async Task<int> CountAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM ftJournalDE").ConfigureAwait(false);
            }
        }
    }
}
