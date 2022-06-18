using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.FR
{
    public class MySQLJournalFRRepository : AbstractMySQLRepository<Guid, ftJournalFR>, IJournalFRRepository, IMiddlewareJournalFRRepository
    {
        public MySQLJournalFRRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftJournalFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftJournalFR> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftJournalFR>("Select * from ftJournalFR where ftJournalFRId = @JournalFRId", new { JournalFRId = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftJournalFR>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftJournalFR>("select * from ftJournalFR").ConfigureAwait(false);
            }
        }

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
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, journal).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftJournalFR entity) => entity.ftJournalFRId;

        public async Task<ftJournalFR> GetWithLastTimestampAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftJournalFR>("SELECT * FROM ftJournalFR ORDER BY TimeStamp DESC LIMIT 1").ConfigureAwait(false);
            }
        }
    }
}
