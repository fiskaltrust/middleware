using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.DE
{
    public class SQLiteFailedFinishTransactionRepository : AbstractSQLiteRepository<string, FailedFinishTransaction>, IPersistentTransactionRepository<FailedFinishTransaction>
    {
        public SQLiteFailedFinishTransactionRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(FailedFinishTransaction entity) { }

        public override async Task<FailedFinishTransaction> GetAsync(string cbReceiptReference) => await DbConnection.QueryFirstOrDefaultAsync<FailedFinishTransaction>("Select * from FailedFinishTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);

        public override async Task<IEnumerable<FailedFinishTransaction>> GetAsync() => await DbConnection.QueryAsync<FailedFinishTransaction>("select * from FailedFinishTransaction").ConfigureAwait(false);

        public async Task InsertOrUpdateTransactionAsync(FailedFinishTransaction finishTransaction)
        {
            var sql = "INSERT or REPLACE INTO FailedFinishTransaction " +
                      "(cbReceiptReference, TransactionNumber, FinishMoment, ftQueueItemId, CashBoxIdentification, Request) " +
                      "Values (@cbReceiptReference, @TransactionNumber, @FinishMoment, @ftQueueItemId, @CashBoxIdentification, @Request);";
            await DbConnection.ExecuteAsync(sql, finishTransaction).ConfigureAwait(false);
        }

        public async Task<FailedFinishTransaction> RemoveAsync(string cbReceiptReference)
        {
            var entity = await DbConnection.QueryFirstOrDefaultAsync<FailedFinishTransaction>("Select * from FailedFinishTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);
            await DbConnection.ExecuteAsync("DELETE FROM FailedFinishTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);
            return entity;
        }

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await DbConnection.QueryFirstAsync<bool>("SELECT EXISTS(SELECT 1 FROM FailedFinishTransaction where cbReceiptReference = @cbReceiptReference)", new { cbReceiptReference }).ConfigureAwait(false);

        protected override string GetIdForEntity(FailedFinishTransaction entity) => entity.cbReceiptReference;
    }
}
