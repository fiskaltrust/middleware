using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.DE
{
    public class MySQLFailedFinishTransactionRepository : AbstractMySQLRepository<string, FailedFinishTransaction>, IPersistentTransactionRepository<FailedFinishTransaction>
    {
        public MySQLFailedFinishTransactionRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(FailedFinishTransaction entity) { }

        public override async Task<FailedFinishTransaction> GetAsync(string cbReceiptReference)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<FailedFinishTransaction>("Select * from FailedFinishTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<FailedFinishTransaction>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<FailedFinishTransaction>("select * from FailedFinishTransaction").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateTransactionAsync(FailedFinishTransaction finishTransaction)
        {
            var sql = "REPLACE INTO FailedFinishTransaction " +
                      "(cbReceiptReference, TransactionNumber, FinishMoment, ftQueueItemId, CashBoxIdentification, Request) " +
                      "Values (@cbReceiptReference, @TransactionNumber, @FinishMoment, @ftQueueItemId, @CashBoxIdentification, @Request);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, finishTransaction).ConfigureAwait(false);
            }
        }

        public async Task<FailedFinishTransaction> RemoveAsync(string cbReceiptReference)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var entity = await connection.QueryFirstOrDefaultAsync<FailedFinishTransaction>("Select * from FailedFinishTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);
                await connection.ExecuteAsync("DELETE FROM FailedFinishTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);
                return entity;
            }
        }

        public async Task<bool> ExistsAsync(string cbReceiptReference)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstAsync<bool>("SELECT EXISTS(SELECT 1 FROM FailedFinishTransaction where cbReceiptReference = @cbReceiptReference)", new { cbReceiptReference }).ConfigureAwait(false);
            }
        }

        protected override string GetIdForEntity(FailedFinishTransaction entity) => entity.cbReceiptReference;
    }
}
