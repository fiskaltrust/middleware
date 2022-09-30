using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.MySQL.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.MySQL.DatabaseInitialization;
using fiskaltrust.Middleware.Storage.MySQL.Repositories.DE;
using Microsoft.Extensions.Logging;
using Moq;
using MySqlConnector;
using Xunit;

namespace fiskaltrust.Middleware.Storage.MySQL.AcceptanceTest
{

    [Collection(MySQLStorageCollectionFixture.CollectionName)]
    public class MySQLFailedFinishTransactionRepositoryTests : AbstractFailedFinishTransactionRepositoryTests, IDisposable
    {
        private MySQLFailedFinishTransactionRepository _repo;

        public override Task<IPersistentTransactionRepository<FailedFinishTransaction>> CreateReadOnlyRepository(IEnumerable<FailedFinishTransaction> entries) => CreateRepository(entries);

        public override async Task<IPersistentTransactionRepository<FailedFinishTransaction>> CreateRepository(IEnumerable<FailedFinishTransaction> entries)
        {
            var databasMigrator = new DatabaseMigrator(MySQLConnectionStringFixture.ServerConnectionString, 30 * 60, MySQLConnectionStringFixture.QueueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databasMigrator.MigrateAsync();

            _repo = new MySQLFailedFinishTransactionRepository(MySQLConnectionStringFixture.DatabaseConnectionString);
            foreach (var entry in entries)
            {
                await _repo.InsertOrUpdateTransactionAsync(entry);
            }
            return _repo;
        }

        //Clear Database before each test
        public override void DisposeDatabase()
        {
            using (var mySqlConnetion = new MySqlConnection(MySQLConnectionStringFixture.DatabaseConnectionString))
            {
                mySqlConnetion.Open();
                using (var command = new MySqlCommand($@"DELETE FROM {TableNames.FailedFinishTransaction}", mySqlConnetion))
                {
                    command.ExecuteNonQuery();
                }
            }

        }
    }
}
