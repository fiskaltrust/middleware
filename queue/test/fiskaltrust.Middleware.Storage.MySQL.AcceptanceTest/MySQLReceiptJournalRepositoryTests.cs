using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
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
    public class MySQLReceiptJournalRepositoryTests : AbstractReceiptJournalRepositoryTests
    {
        private MySQLReceiptJournalRepository _repo;

        public override async Task<IReadOnlyReceiptJournalRepository> CreateReadOnlyRepository(IEnumerable<ftReceiptJournal> entries) => await CreateRepository(entries);

        public override async Task<IReceiptJournalRepository> CreateRepository(IEnumerable<ftReceiptJournal> entries)
        {
            var databasMigrator = new DatabaseMigrator(MySQLConnectionStringFixture.ServerConnectionString, MySQLConnectionStringFixture.QueueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databasMigrator.MigrateAsync();

            _repo = new MySQLReceiptJournalRepository(MySQLConnectionStringFixture.DatabaseConnectionString);
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
                using (var command = new MySqlCommand($@"DELETE FROM {TableNames.FtReceiptJournal}", mySqlConnetion))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
