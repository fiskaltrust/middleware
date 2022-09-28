using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.MySQL.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.MySQL.DatabaseInitialization;
using fiskaltrust.Middleware.Storage.MySQL.Repositories.DE.MasterData;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;
using Moq;
using MySqlConnector;
using Xunit;

namespace fiskaltrust.Middleware.Storage.MySQL.AcceptanceTest
{
    [Collection(MySQLStorageCollectionFixture.CollectionName)]
    public class MySQLAgencyMasterDataRepositoryTests : AbstractAgencyMasterDataRepositoryTests
    {
        private MySQLAgencyMasterDataRepository _repo;

        public override Task<IMasterDataRepository<AgencyMasterData>> CreateReadOnlyRepository(IEnumerable<AgencyMasterData> entries) => CreateRepository(entries);

        public override async Task<IMasterDataRepository<AgencyMasterData>> CreateRepository(IEnumerable<AgencyMasterData> entries)
        {
            var databasMigrator = new DatabaseMigrator(MySQLConnectionStringFixture.ServerConnectionString, 30 * 60, MySQLConnectionStringFixture.QueueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databasMigrator.MigrateAsync();

            _repo = new MySQLAgencyMasterDataRepository(MySQLConnectionStringFixture.DatabaseConnectionString);
            foreach (var entry in entries)
            {
                await _repo.CreateAsync(entry);
            }
            return _repo;
        }

        //Clear Database before each test
        public override void DisposeDatabase()
        {
            using (var mySqlConnetion = new MySqlConnection(MySQLConnectionStringFixture.ServerConnectionString))
            {
                mySqlConnetion.Open();
                using (var command = new MySqlCommand($@"DELETE FROM {TableNames.AgencyMasterData}", mySqlConnetion))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
