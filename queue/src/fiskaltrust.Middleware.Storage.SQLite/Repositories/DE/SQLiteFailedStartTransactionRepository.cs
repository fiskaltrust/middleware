using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.DE
{
    public class SQLiteFailedStartTransactionRepository : AbstractSQLiteRepository<string, FailedStartTransaction>, IPersistentTransactionRepository<FailedStartTransaction>
    {
        public SQLiteFailedStartTransactionRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(FailedStartTransaction entity) { }

        public override async Task<FailedStartTransaction> GetAsync(string cbReceiptReference) => await DbConnection.QueryFirstOrDefaultAsync<FailedStartTransaction>("Select * from FailedStartTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);

        public override async Task<IEnumerable<FailedStartTransaction>> GetAsync() => await DbConnection.QueryAsync<FailedStartTransaction>("select * from FailedStartTransaction").ConfigureAwait(false);

        public async Task InsertOrUpdateTransactionAsync(FailedStartTransaction startTransaction)
        {
            var sql = "INSERT or REPLACE INTO FailedStartTransaction " +
                      "(cbReceiptReference, StartMoment, ftQueueItemId, CashBoxIdentification, Request) " +
                      "Values (@cbReceiptReference, @StartMoment, @ftQueueItemId, @CashBoxIdentification, @Request);";
            await DbConnection.ExecuteAsync(sql, startTransaction).ConfigureAwait(false);
        }

        public async Task<FailedStartTransaction> RemoveAsync(string cbReceiptReference)
        {
            var entity = await DbConnection.QueryFirstOrDefaultAsync<FailedStartTransaction>("Select * from FailedStartTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);
            await DbConnection.ExecuteAsync("DELETE FROM FailedStartTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);
            return entity;
        }

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await DbConnection.QueryFirstAsync<bool>("SELECT EXISTS(SELECT 1 FROM FailedStartTransaction where cbReceiptReference = @cbReceiptReference)", new { cbReceiptReference }).ConfigureAwait(false);

        protected override string GetIdForEntity(FailedStartTransaction entity) => entity.cbReceiptReference;
    }
}
