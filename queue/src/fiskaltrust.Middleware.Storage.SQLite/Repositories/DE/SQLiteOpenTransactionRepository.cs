using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.DE
{
    public class SQLiteOpenTransactionRepository : AbstractSQLiteRepository<string, OpenTransaction>, IPersistentTransactionRepository<OpenTransaction>
    {
        public SQLiteOpenTransactionRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(OpenTransaction entity) { }

        public override async Task<OpenTransaction> GetAsync(string cbReceiptReference) => await DbConnection.QueryFirstOrDefaultAsync<OpenTransaction>("Select * from OpenTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);

        public override async Task<IEnumerable<OpenTransaction>> GetAsync() => await DbConnection.QueryAsync<OpenTransaction>("select * from OpenTransaction").ConfigureAwait(false);

        public async Task InsertOrUpdateTransactionAsync(OpenTransaction openTransaction)
        {
            var sql = "INSERT or REPLACE INTO OpenTransaction " +
                         "(cbReceiptReference, StartMoment, TransactionNumber, StartTransactionSignatureBase64) " +
                         "Values (@cbReceiptReference, @StartMoment, @TransactionNumber, @StartTransactionSignatureBase64);";
            await DbConnection.ExecuteAsync(sql, openTransaction).ConfigureAwait(false);
        }

        public async Task<OpenTransaction> RemoveAsync(string cbReceiptReference)
        {
            var entity = await DbConnection.QueryFirstOrDefaultAsync<OpenTransaction>("Select * from OpenTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);
            await DbConnection.ExecuteAsync("DELETE FROM OpenTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);
            return entity;
        }

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await DbConnection.QueryFirstAsync<bool>("SELECT EXISTS(SELECT 1 FROM OpenTransaction where cbReceiptReference = @cbReceiptReference)", new { cbReceiptReference }).ConfigureAwait(false);

        protected override string GetIdForEntity(OpenTransaction entity) => entity.cbReceiptReference;
    }
}
