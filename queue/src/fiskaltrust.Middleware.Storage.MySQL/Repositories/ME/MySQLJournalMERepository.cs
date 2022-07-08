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
    public class MySQLJournalMERepository : AbstractMySQLRepository<Guid, ftJournalME>, IJournalMERepository, IMiddlewareJournalMERepository
    {
        public MySQLJournalMERepository(string connectionString) : base(connectionString) { }
        public override void EntityUpdated(ftJournalME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;
        public override async Task<ftJournalME> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftJournalME>("Select * from ftJournalME where ftJournalMEId = @JournalMEId", new { JournalMEId = id }).ConfigureAwait(false);
            }
        }
        public override async Task<IEnumerable<ftJournalME>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftJournalME>("select * from ftJournalME").ConfigureAwait(false);
            }
        }
        public async Task<ftJournalME> GetLastEntryAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftJournalME>("Select * from ftJournalME order by Number desc limit 1").ConfigureAwait(false);
            }
        }
        public async Task InsertAsync(ftJournalME journal)
        {
            if (await GetAsync(GetIdForEntity(journal)).ConfigureAwait(false) != null)
            {
                throw new Exception("Already exists");
            }
            EntityUpdated(journal);
            var sql = "INSERT INTO ftJournalME " +
                      "(ftJournalMEId, cbReference, ftInvoiceNumber, ftOrdinalNumber, ftQueueItemId, ftQueueId, TimeStamp, Number, FCDC, JournalType, FIC, IIC) " +
                      "Values (@ftJournalMEId, @cbReference, @ftInvoiceNumber, @ftOrdinalNumber, @ftQueueItemId, @ftQueueId, @TimeStamp, @Number, @FCDC, @JournalType, @FIC, @IIC);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, journal).ConfigureAwait(false);
            }
        }
        protected override Guid GetIdForEntity(ftJournalME entity) => entity.ftJournalMEId;

        public async IAsyncEnumerable<ftJournalME> GetByQueueItemId(Guid queueItemId)
        {
            var query = "SELECT * FROM ftJournalME WHERE ftQueueItemId = @queueItemId;";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await foreach (var entry in connection.Query<ftJournalME>(query, new { queueItemId }, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
                {
                    yield return entry;
                }
            }
        }

        public async IAsyncEnumerable<ftJournalME> GetByReceiptReference(string cbReceiptReference)
        {
            var query = "SELECT * FROM ftJournalME WHERE cbReference = @cbReceiptReference;";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await foreach (var entry in connection.Query<ftJournalME>(query, new { cbReceiptReference }, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
                {
                    yield return entry;
                }
            }
        }
    }
}
