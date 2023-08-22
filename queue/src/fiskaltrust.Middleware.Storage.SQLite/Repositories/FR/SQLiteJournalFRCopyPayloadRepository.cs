using System;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories.FR;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.FR
{
    public class SQLiteJournalFRCopyPayloadRepository : AbstractSQLiteRepository<Guid, ftJournalFRCopyPayload>, IJournalFRCopyPayloadRepository
    {
        public SQLiteJournalFRCopyPayloadRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftJournalFRCopyPayload entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public async Task<int> JournalFRGetCountOfCopies(string cbPreviousReceiptReference)
        {
            var count = await DbConnection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM ftJournalFRCopyPayload WHERE CopiedReceiptReference = @Reference", 
                new { Reference = cbPreviousReceiptReference }
            ).ConfigureAwait(false);
            return count;
        }

        public async Task<bool> InsertJournalFRCopyPayload(ftJournalFRCopyPayload payload)
        {
            try
            {
                var sql = "INSERT INTO ftJournalFRCopyPayload " +
                              "(QueueId, CashBoxIdentification, Siret, ReceiptId, ReceiptMoment, QueueItemId, CopiedReceiptReference, CertificateSerialNumber) " +
                              "Values (@QueueId, @CashBoxIdentification, @Siret, @ReceiptId, @ReceiptMoment, @QueueItemId, @CopiedReceiptReference, @CertificateSerialNumber);";
                await DbConnection.ExecuteAsync(sql, payload).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> HasJournalFRCopyPayloads()
        {
            var count = await DbConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ftJournalFRCopyPayload").ConfigureAwait(false);
            return count > 0;
        }

        protected override Guid GetIdForEntity(ftJournalFRCopyPayload entity) => entity.QueueItemId;
    }
}