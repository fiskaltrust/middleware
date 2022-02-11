using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.AT
{
    public class SQLiteJournalATRepository : AbstractSQLiteRepository<Guid, ftJournalAT>, IJournalATRepository
    {
        public SQLiteJournalATRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftJournalAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftJournalAT> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftJournalAT>("Select * from ftJournalAT where ftJournalATId = @JournalATId", new { JournalATId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftJournalAT>> GetAsync() => await DbConnection.QueryAsync<ftJournalAT>("select * from ftJournalAT").ConfigureAwait(false);

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
            await DbConnection.ExecuteAsync(sql, journal).ConfigureAwait(false);
        }

        protected override Guid GetIdForEntity(ftJournalAT entity) => entity.ftJournalATId;
    }
}
