using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.DE
{
    public class MySQLFailedStartTransactionRepository : AbstractMySQLRepository<string, FailedStartTransaction>, IPersistentTransactionRepository<FailedStartTransaction>
    {
        public MySQLFailedStartTransactionRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(FailedStartTransaction entity) { }

        public override async Task<FailedStartTransaction> GetAsync(string cbReceiptReference)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<FailedStartTransaction>("Select * from FailedStartTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<FailedStartTransaction>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<FailedStartTransaction>("select * from FailedStartTransaction").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateTransactionAsync(FailedStartTransaction startTransaction)
        {
            var sql = "REPLACE INTO FailedStartTransaction " +
                      "(cbReceiptReference, StartMoment, ftQueueItemId, CashBoxIdentification, Request) " +
                      "Values (@cbReceiptReference, @StartMoment, @ftQueueItemId, @CashBoxIdentification, @Request);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, startTransaction).ConfigureAwait(false);
            }
        }

        public async Task<FailedStartTransaction> RemoveAsync(string cbReceiptReference)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var entity = await connection.QueryFirstOrDefaultAsync<FailedStartTransaction>("Select * from FailedStartTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);
                await connection.ExecuteAsync("DELETE FROM FailedStartTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);
                return entity;
            }
        }

        public async Task<bool> ExistsAsync(string cbReceiptReference)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstAsync<bool>("SELECT EXISTS(SELECT 1 FROM FailedStartTransaction where cbReceiptReference = @cbReceiptReference)", new { cbReceiptReference }).ConfigureAwait(false);
            }
        }

        protected override string GetIdForEntity(FailedStartTransaction entity) => entity.cbReceiptReference;
    }
}
