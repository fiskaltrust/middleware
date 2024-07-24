using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.MySQL.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.MySQL.DatabaseInitialization;
using fiskaltrust.Middleware.Storage.MySQL.Repositories.FR;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;
using MySqlConnector;
using Xunit;

namespace fiskaltrust.Middleware.Storage.MySQL.AcceptanceTest
{
    [Collection(MySQLStorageCollectionFixture.CollectionName)]
    public class MySQLJournalFRCopyPayloadRepositoryTests : AbstractJournalFRCopyPayloadRepositoryTests
    {
        private MySQLJournalFRCopyPayloadRepository _repo;

        public override async Task<IJournalFRCopyPayloadRepository> CreateRepository(IEnumerable<ftJournalFRCopyPayload> entries)
        {
            var databasMigrator = new DatabaseMigrator(MySQLConnectionStringFixture.ServerConnectionString, 30 * 60, MySQLConnectionStringFixture.QueueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databasMigrator.MigrateAsync();

            _repo = new MySQLJournalFRCopyPayloadRepository(MySQLConnectionStringFixture.DatabaseConnectionString);
            foreach (var entry in entries)
            { await _repo.InsertAsync(entry); }
            return _repo;
        }

        //Clear Database before each test
        public override void DisposeDatabase()
        {
            using (var mySqlConnetion = new MySqlConnection(MySQLConnectionStringFixture.DatabaseConnectionString))
            {
                mySqlConnetion.Open();
                using (var command = new MySqlCommand($@"DELETE FROM {TableNames.FtJournalFRCopyPayload}", mySqlConnetion))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
