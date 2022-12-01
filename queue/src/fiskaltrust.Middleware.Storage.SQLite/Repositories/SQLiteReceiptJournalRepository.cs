using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories
{
    public class SQLiteReceiptJournalRepository : AbstractSQLiteRepository<Guid, ftReceiptJournal>, IReceiptJournalRepository
    {
        public SQLiteReceiptJournalRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftReceiptJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        // We're using CAST(ftReceiptTotal AS FLOAT) here because the type of this column was initially (wrongly) set to INT instead of FLOAT
        // Since SQLite is ignoring the type while storing values, but not during reading, we were losing the decimals and hence need to cast.
        // New customers are anyway not affected, as we fixed the type in the initial migration.
        // This also applies for the other methods below
        public override async Task<ftReceiptJournal> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftReceiptJournal>("Select ftReceiptJournalId, ftReceiptMoment, ftReceiptNumber, CAST(ftReceiptTotal AS FLOAT) AS ftReceiptTotal, ftQueueId, ftQueueItemId, ftReceiptHash, TimeStamp from ftReceiptJournal where ftReceiptJournalId = @ReceiptJournalId", new { ReceiptJournalId = id }).ConfigureAwait(false);
        
        public override async Task<IEnumerable<ftReceiptJournal>> GetAsync() => await DbConnection.QueryAsync<ftReceiptJournal>("Select ftReceiptJournalId, ftReceiptMoment, ftReceiptNumber, CAST(ftReceiptTotal AS FLOAT) AS ftReceiptTotal, ftQueueId, ftQueueItemId, ftReceiptHash, TimeStamp from ftReceiptJournal").ConfigureAwait(false);

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
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        public async Task Insert(IAsyncEnumerable<ftReceiptJournal> ftReceiptJournals)
        {
            using (var transaction = DbConnection.BeginTransaction())
            {
                var command = DbConnection.CreateCommand();
                command.CommandText = @"INSERT INTO ftReceiptJournal " +
                      "( ftReceiptJournalId, ftReceiptMoment, ftReceiptNumber, ftReceiptTotal, ftQueueId, ftQueueItemId, ftReceiptHash, TimeStamp) " +
                      "Values ( @ftReceiptJournalId, @ftReceiptMoment, @ftReceiptNumber, @ftReceiptTotal, @ftQueueId, @ftQueueItemId, @ftReceiptHash, @TimeStamp);";

                var ftReceiptJournalId = command.CreateParameter();
                ftReceiptJournalId.ParameterName = "@ftReceiptJournalId";
                command.Parameters.Add(ftReceiptJournalId);
                var ftReceiptMoment = command.CreateParameter();
                ftReceiptMoment.ParameterName = "@ftReceiptMoment";
                command.Parameters.Add(ftReceiptMoment);
                var ftReceiptNumber = command.CreateParameter();
                ftReceiptNumber.ParameterName = "@ftReceiptNumber";
                command.Parameters.Add(ftReceiptNumber);
                var ftReceiptTotal = command.CreateParameter();
                ftReceiptTotal.ParameterName = "@ftReceiptTotal";
                command.Parameters.Add(ftReceiptTotal);
                var ftQueueId = command.CreateParameter();
                ftQueueId.ParameterName = "@ftQueueId";
                command.Parameters.Add(ftQueueId);
                var ftQueueItemId = command.CreateParameter();
                ftQueueItemId.ParameterName = "@ftQueueItemId";
                command.Parameters.Add(ftQueueItemId);
                var ftReceiptHash = command.CreateParameter();
                ftReceiptHash.ParameterName = "@ftReceiptHash";
                command.Parameters.Add(ftReceiptHash);
                var TimeStamp = command.CreateParameter();
                TimeStamp.ParameterName = "@TimeStamp";
                command.Parameters.Add(TimeStamp);

                await foreach (var item in ftReceiptJournals)
                {
                    ftReceiptJournalId.Value = item.ftReceiptJournalId;
                    ftReceiptMoment.Value = item.ftReceiptMoment;
                    ftReceiptNumber.Value = item.ftReceiptNumber;
                    ftReceiptTotal.Value = item.ftReceiptTotal;
                    ftQueueId.Value = item.ftQueueId;
                    ftQueueItemId.Value = item.ftQueueItemId;
                    ftReceiptHash.Value = item.ftReceiptHash;
                    TimeStamp.Value = item.TimeStamp;

                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {

                        var msg = e.Message;
                    };
                }
                transaction.Commit();
            }
        }

        public override async IAsyncEnumerable<ftReceiptJournal> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive)
        {
            await foreach (var entry in DbConnection.Query<ftReceiptJournal>($"Select ftReceiptJournalId, ftReceiptMoment, ftReceiptNumber, CAST(ftReceiptTotal AS FLOAT) AS ftReceiptTotal, ftQueueId, ftQueueItemId, ftReceiptHash, TimeStamp from ftReceiptJournal WHERE TimeStamp >= @from AND TimeStamp <= @to ORDER BY TimeStamp", new { from = fromInclusive, to = toInclusive }, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
            {
                yield return entry;
            }
        }

        public override async IAsyncEnumerable<ftReceiptJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var query = $"Select ftReceiptJournalId, ftReceiptMoment, ftReceiptNumber, CAST(ftReceiptTotal AS FLOAT) AS ftReceiptTotal, ftQueueId, ftQueueItemId, ftReceiptHash, TimeStamp from ftReceiptJournal WHERE TimeStamp >= @from ORDER BY TimeStamp";
            if (take.HasValue)
            {
                query += $" LIMIT {take}";
            }
            await foreach (var entry in DbConnection.Query<ftReceiptJournal>(query, new { from = fromInclusive }, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
            {
                yield return entry;
            }
        }
    }
}
