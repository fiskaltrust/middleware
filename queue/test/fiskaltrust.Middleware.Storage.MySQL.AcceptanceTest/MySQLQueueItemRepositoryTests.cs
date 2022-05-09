using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.MySQL.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.MySQL.DatabaseInitialization;
using fiskaltrust.Middleware.Storage.MySQL.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;
using MySqlConnector;
using Xunit;

namespace fiskaltrust.Middleware.Storage.MySQL.AcceptanceTest
{
    [Collection(MySQLStorageCollectionFixture.CollectionName)]
    public class MySQLQueueItemRepositoryTests : AbstractQueueItemRepositoryTests
    {
        private MySQLQueueItemRepository _repo;

        public override async Task<IReadOnlyQueueItemRepository> CreateReadOnlyRepository(IEnumerable<ftQueueItem> entries) => await CreateRepository(entries);

        public override async Task<IMiddlewareQueueItemRepository> CreateRepository(IEnumerable<ftQueueItem> entries)
        {
            var databasMigrator = new DatabaseMigrator(MySQLConnectionStringFixture.ServerConnectionString, MySQLConnectionStringFixture.QueueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databasMigrator.MigrateAsync();

            _repo = new MySQLQueueItemRepository(MySQLConnectionStringFixture.DatabaseConnectionString);
            foreach (var entry in entries)
            { await _repo.InsertOrUpdateAsync(entry); }
            return _repo;
        }

        //Clear Database before each test
        public override void DisposeDatabase()
        {
            using (var mySqlConnetion = new MySqlConnection(MySQLConnectionStringFixture.DatabaseConnectionString))
            {
                mySqlConnetion.Open();
                using (var command = new MySqlCommand($@"DELETE FROM {TableNames.FtQueueItem}", mySqlConnetion))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

    }
}
