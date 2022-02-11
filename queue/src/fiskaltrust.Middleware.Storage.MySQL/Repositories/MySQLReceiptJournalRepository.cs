using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories
{
    public class MySQLReceiptJournalRepository : AbstractMySQLRepository<Guid, ftReceiptJournal>, IReceiptJournalRepository
    {
        public MySQLReceiptJournalRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftReceiptJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftReceiptJournal> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftReceiptJournal>("Select * from ftReceiptJournal where ftReceiptJournalId = @ReceiptJournalId", new { ReceiptJournalId = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftReceiptJournal>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftReceiptJournal>("select * from ftReceiptJournal").ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftReceiptJournal entity) => entity.ftReceiptJournalId;

        public async Task InsertAsync(ftReceiptJournal entity)
        {
            if (await GetAsync(GetIdForEntity(entity)).ConfigureAwait(false) != null)
            {
                throw new Exception("Already exists");
            }
            EntityUpdated(entity);
            var sql = "INSERT INTO ftReceiptJournal " +
                      "( ftReceiptJournalId, ftReceiptMoment, ftReceiptNumber, ftReceiptTotal, ftQueueId, ftQueueItemId, ftReceiptHash, TimeStamp) " +
                      "Values ( @ftReceiptJournalId, @ftReceiptMoment, @ftReceiptNumber, @ftReceiptTotal, @ftQueueId, @ftQueueItemId, @ftReceiptHash, @TimeStamp);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }
    }
}
