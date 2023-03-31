using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.IT
{
    public class MySQLJournalITRepository : AbstractMySQLRepository<Guid, ftJournalIT>, IJournalITRepository
    {
        public MySQLJournalITRepository(string connectionString) : base(connectionString) { }
        public override void EntityUpdated(ftJournalIT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;
        public override async Task<ftJournalIT> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftJournalIT>("Select * from ftJournalIT where ftJournalITId = @JournalITId", new { JournalITId = id }).ConfigureAwait(false);
            }
        }
        public override async Task<IEnumerable<ftJournalIT>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftJournalIT>("select * from ftJournalIT").ConfigureAwait(false);
            }
        }

        public async Task InsertAsync(ftJournalIT journal)
        {
            if (await GetAsync(GetIdForEntity(journal)).ConfigureAwait(false) != null)
            {
                throw new Exception("Already exists");
            }
            EntityUpdated(journal);
            var sql = "INSERT INTO ftJournalIT " +
                      "(ftJournalITId, ftQueueItemId, ftQueueId, ftSignaturCreationUnitITId, RecNumber, ZRecNumber, JournalType, cbReceiptReference, RecordDataJson, RecDate, TimeStamp) " +
                      "Values (@ftJournalITId, @ftQueueItemId, @ftQueueId, @ftSignaturCreationUnitITId, @RecNumber, @ZRecNumber, @JournalType, @cbReceiptReference, @RecordDataJson, @RecDate, @TimeStamp);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, journal).ConfigureAwait(false);
            }
        }
        protected override Guid GetIdForEntity(ftJournalIT entity) => entity.ftJournalITId;
    }
}
