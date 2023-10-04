using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.FR
{
    public class MySQLJournalFRCopyPayloadRepository : AbstractMySQLRepository<Guid, ftJournalFRCopyPayload>, IJournalFRCopyPayloadRepository
    {
        public MySQLJournalFRCopyPayloadRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftJournalFRCopyPayload entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftJournalFRCopyPayload> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftJournalFRCopyPayload>(
                    "SELECT * FROM ftJournalFRCopyPayload WHERE QueueItemId = @Id",
                    new { Id = id }
                ).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftJournalFRCopyPayload>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftJournalFRCopyPayload>("SELECT * FROM ftJournalFRCopyPayload").ConfigureAwait(false);
            }
        }

        public async Task<int> GetCountOfCopiesAsync(string cbPreviousReceiptReference)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM ftJournalFRCopyPayload WHERE CopiedReceiptReference = @Reference",
                    new { Reference = cbPreviousReceiptReference }
                ).ConfigureAwait(false);
            }
        }

        public async Task<bool> InsertAsync(ftJournalFRCopyPayload payload)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var existingEntity = await connection.QueryFirstOrDefaultAsync<ftJournalFRCopyPayload>(
                    "SELECT * FROM ftJournalFRCopyPayload WHERE QueueItemId = @Id LIMIT 1",
                    new { Id = GetIdForEntity(payload) }
                ).ConfigureAwait(false);

                if (existingEntity != null)
                {
                    throw new Exception("Entity with the same ID already exists.");
                }

                EntityUpdated(payload);
                var affectedRows = await connection.ExecuteAsync(
                    "INSERT INTO ftJournalFRCopyPayload (QueueId, CashBoxIdentification, Siret, ReceiptId, ReceiptMoment, QueueItemId, CopiedReceiptReference, CertificateSerialNumber, TimeStamp) " +
                    "Values (@QueueId, @CashBoxIdentification, @Siret, @ReceiptId, @ReceiptMoment, @QueueItemId, @CopiedReceiptReference, @CertificateSerialNumber, @TimeStamp);",
                    payload
                ).ConfigureAwait(false);

                return affectedRows > 0;
            }
        }

        protected override Guid GetIdForEntity(ftJournalFRCopyPayload entity) => entity.QueueItemId;
    }
}
