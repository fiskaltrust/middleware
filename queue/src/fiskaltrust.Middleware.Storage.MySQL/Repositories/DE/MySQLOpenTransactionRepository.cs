using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.DE
{
    public class MySQLOpenTransactionRepository : AbstractMySQLRepository<string, OpenTransaction>, IPersistentTransactionRepository<OpenTransaction>
    {
        public MySQLOpenTransactionRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(OpenTransaction entity) { }

        public override async Task<OpenTransaction> GetAsync(string cbReceiptReference)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<OpenTransaction>("Select * from OpenTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<OpenTransaction>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<OpenTransaction>("select * from OpenTransaction").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateTransactionAsync(OpenTransaction openTransaction)
        {
            var sql = "REPLACE INTO OpenTransaction " +
                         "(cbReceiptReference, StartMoment, TransactionNumber, StartTransactionSignatureBase64) " +
                         "Values (@cbReceiptReference, @StartMoment, @TransactionNumber, @StartTransactionSignatureBase64);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, openTransaction).ConfigureAwait(false);
            }
        }

        public async Task<OpenTransaction> RemoveAsync(string cbReceiptReference)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                var entity = await connection.QueryFirstOrDefaultAsync<OpenTransaction>("Select * from OpenTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync("DELETE FROM OpenTransaction where cbReceiptReference = @cbReceiptReference", new { cbReceiptReference }).ConfigureAwait(false);
                return entity;
            }
        }

        public async Task<bool> ExistsAsync(string cbReceiptReference)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstAsync<bool>("SELECT EXISTS(SELECT 1 FROM OpenTransaction where cbReceiptReference = @cbReceiptReference)", new { cbReceiptReference }).ConfigureAwait(false);
            }
        }

        protected override string GetIdForEntity(OpenTransaction entity) => entity.cbReceiptReference;
    }
}
