using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories
{
    public class SQLiteActionJournalRepository : AbstractSQLiteRepository<Guid, ftActionJournal>, IActionJournalRepository, IMiddlewareActionJournalRepository
    {
        public SQLiteActionJournalRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftActionJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftActionJournal> GetAsync(Guid id) => await DbConnection.QueryFirstOrDefaultAsync<ftActionJournal>("Select * from ftActionJournal where ftActionJournalId = @ActionJournalId", new { ActionJournalId = id }).ConfigureAwait(false);

        public override async Task<IEnumerable<ftActionJournal>> GetAsync() => await DbConnection.QueryAsync<ftActionJournal>("select * from ftActionJournal").ConfigureAwait(false);

        protected override Guid GetIdForEntity(ftActionJournal entity) => entity.ftActionJournalId;

        public async Task InsertAsync(ftActionJournal entity)
        {
            if (await GetAsync(GetIdForEntity(entity)).ConfigureAwait(false) != null)
            {
                throw new Exception("Already exists");
            }
            EntityUpdated(entity);
            var sql = "INSERT INTO ftActionJournal " +
                      "(ftActionJournalId, ftQueueId, ftQueueItemId, Moment, Priority, Type, Message, DataBase64, DataJson, TimeStamp) " +
                      "Values (@ftActionJournalId, @ftQueueId, @ftQueueItemId, @Moment, @Priority, @Type, @Message, @DataBase64, @DataJson, @TimeStamp);";
            await DbConnection.ExecuteAsync(sql, entity).ConfigureAwait(false);
        }

        public async Task Insert(IAsyncEnumerable<ftActionJournal> ftActionJournals)
        {
            using (var transaction = DbConnection.BeginTransaction())
            {
                var command = DbConnection.CreateCommand();
                command.CommandText = @"INSERT INTO ftActionJournal " +
                      "(ftActionJournalId, ftQueueId, ftQueueItemId, Moment, Priority, Type, Message, DataBase64, DataJson, TimeStamp) " +
                      "Values (@ftActionJournalId, @ftQueueId, @ftQueueItemId, @Moment, @Priority, @Type, @Message, @DataBase64, @DataJson, @TimeStamp);";

                var ftActionJournalId = command.CreateParameter();
                ftActionJournalId.ParameterName = "@ftActionJournalId";
                command.Parameters.Add(ftActionJournalId);
                var ftQueueId = command.CreateParameter();
                ftQueueId.ParameterName = "@ftQueueId";
                command.Parameters.Add(ftQueueId);
                var ftQueueItemId = command.CreateParameter();
                ftQueueItemId.ParameterName = "@ftQueueItemId";
                command.Parameters.Add(ftQueueItemId);
                var Moment = command.CreateParameter();
                Moment.ParameterName = "@Moment";
                command.Parameters.Add(Moment);
                var Priority = command.CreateParameter();
                Priority.ParameterName = "@Priority";
                command.Parameters.Add(Priority);
                var Type = command.CreateParameter();
                Type.ParameterName = "@Type";
                command.Parameters.Add(Type);
                var Message = command.CreateParameter();
                Message.ParameterName = "@Message";
                command.Parameters.Add(Message);
                var DataBase64 = command.CreateParameter();
                DataBase64.ParameterName = "@DataBase64";
                command.Parameters.Add(DataBase64);
                var DataJson = command.CreateParameter();
                DataJson.ParameterName = "@DataJson";
                command.Parameters.Add(DataJson);
                var TimeStamp = command.CreateParameter();
                TimeStamp.ParameterName = "@DataJson";
                command.Parameters.Add(TimeStamp);

                await foreach (var item in ftActionJournals)
                {
                    ftActionJournalId.Value = item.ftActionJournalId;
                    ftQueueId.Value = item.ftQueueId;
                    ftQueueItemId.Value = item.ftQueueItemId;
                    Moment.Value = item.Moment;
                    Priority.Value = item.Priority;
                    Type.Value = item.Type;
                    Message.Value = item.Message;
                    DataBase64.Value = item.DataBase64;
                    DataJson.Value = item.DataJson;
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

        public async IAsyncEnumerable<ftActionJournal> GetByQueueItemId(Guid queueItemId)
        {
            var query = "Select * from ftActionJournal where ftQueueItemId = @queueItemId;";

            await foreach (var entry in DbConnection.Query<ftActionJournal>(query, new { queueItemId }, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
            {
                yield return entry;
            }
        }
    }
}
