using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.AT
{
    public class MySQLJournalATRepository : AbstractMySQLRepository<Guid, ftJournalAT>, IJournalATRepository
    {
        public MySQLJournalATRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftJournalAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftJournalAT> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftJournalAT>("Select * from ftJournalAT where ftJournalATId = @JournalATId", new { JournalATId = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftJournalAT>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftJournalAT>("select * from ftJournalAT").ConfigureAwait(false);
            }
        }

        public async Task InsertAsync(ftJournalAT journal)
        {
            if (await GetAsync(GetIdForEntity(journal)).ConfigureAwait(false) != null)
            {
                throw new Exception("Already exists");
            }

            EntityUpdated(journal);
            var sql = "INSERT INTO ftJournalAT " +
                      "( ftJournalATId, ftSignaturCreationUnitId, Number, JWSHeaderBase64url, JWSPayloadBase64url, JWSSignatureBase64url,ftQueueId,TimeStamp) " +
                      "Values (@ftJournalATId, @ftSignaturCreationUnitId, @Number, @JWSHeaderBase64url, @JWSPayloadBase64url, @JWSSignatureBase64url, @ftQueueId, @TimeStamp);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, journal).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftJournalAT entity) => entity.ftJournalATId;
    }
}
