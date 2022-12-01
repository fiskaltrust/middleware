using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.DE
{
    public class SQLiteJournalDERepository : AbstractSQLiteRepository<Guid, ftJournalDE>, IJournalDERepository, IMiddlewareJournalDERepository
    {
        public SQLiteJournalDERepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftJournalDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftJournalDE> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftJournalDE>("Select * from ftJournalDE where ftJournalDEId = @JournalDEId", new { JournalDEId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftJournalDE>> GetAsync() => await DbConnection.QueryAsync<ftJournalDE>("select * from ftJournalDE").ConfigureAwait(false);

        public async IAsyncEnumerable<ftJournalDE> GetByFileName(string fileName)
        {
            var query = $"SELECT * FROM {typeof(ftJournalDE).Name} WHERE FileName = @fileName";
            await foreach (var entry in DbConnection.Query<ftJournalDE>(query, new { fileName = fileName }, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
            {
                yield return entry;
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
            await DbConnection.ExecuteAsync(sql, journal).ConfigureAwait(false);
        }

        public async Task Insert(IAsyncEnumerable<ftJournalDE> ftJournalDEs)
        {
            using (var transaction = DbConnection.BeginTransaction())
            {
                var command = DbConnection.CreateCommand();
                command.CommandText = @"INSERT INTO ftJournalDE " +
                      "(ftJournalDEId, Number, FileName, FileExtension, FileContentBase64, ftQueueItemId, ftQueueId, TimeStamp) " +
                      "Values (@ftJournalDEId, @Number, @FileName, @FileExtension, @FileContentBase64, @ftQueueItemId, @ftQueueId, @TimeStamp);";

                var ftJournalDEId = command.CreateParameter();
                ftJournalDEId.ParameterName = "@ftJournalDEId";
                command.Parameters.Add(ftJournalDEId);
                var Number = command.CreateParameter();
                Number.ParameterName = "@Number";
                command.Parameters.Add(Number);
                var FileName = command.CreateParameter();
                FileName.ParameterName = "@FileName";
                command.Parameters.Add(FileName);
                var FileExtension = command.CreateParameter();
                FileExtension.ParameterName = "@FileExtension";
                command.Parameters.Add(FileExtension);
                var FileContentBase64 = command.CreateParameter();
                FileContentBase64.ParameterName = "@FileContentBase64";
                command.Parameters.Add(FileContentBase64);
                var ftQueueItemId = command.CreateParameter();
                ftQueueItemId.ParameterName = "@ftQueueItemId";
                command.Parameters.Add(ftQueueItemId);
                var ftQueueId = command.CreateParameter();
                ftQueueId.ParameterName = "@ftQueueId";
                command.Parameters.Add(ftQueueId);
                var TimeStamp = command.CreateParameter();
                TimeStamp.ParameterName = "@TimeStamp";
                command.Parameters.Add(TimeStamp);
 
                await foreach (var item in ftJournalDEs)
                {
                    ftJournalDEId.Value = item.ftJournalDEId;
                    Number.Value = item.Number;
                    FileName.Value = item.FileName;
                    FileExtension.Value = item.FileExtension;
                    FileContentBase64.Value = item.FileContentBase64;
                    ftQueueItemId.Value = item.ftQueueItemId;
                    ftQueueId.Value = item.ftQueueId;
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

        protected override Guid GetIdForEntity(ftJournalDE entity) => entity.ftJournalDEId;
    }
}
