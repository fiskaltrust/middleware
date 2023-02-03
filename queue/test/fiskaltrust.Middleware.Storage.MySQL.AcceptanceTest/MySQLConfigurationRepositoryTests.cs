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
    public class MySQLConfigurationRepositoryTests : AbstractConfigurationRepositoryTests
    {
        private MySQLConfigurationRepository _repo;

        public override async Task<IReadOnlyConfigurationRepository> CreateReadOnlyRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftQueueME> queuesME = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null, IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null) => await CreateRepository(cashBoxes, queues, queuesAT, queuesDE, queuesFR, queuesME, signatureCreateUnitsAT, signatureCreateUnitsDE, signatureCreateUnitsFR, signatureCreateUnitsME);

        public override async Task<IConfigurationRepository> CreateRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftQueueME> queuesME = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null, IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null)
        {
            var databasMigrator = new DatabaseMigrator(MySQLConnectionStringFixture.ServerConnectionString, 30 * 60, MySQLConnectionStringFixture.QueueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databasMigrator.MigrateAsync();

            _repo = new MySQLConfigurationRepository(MySQLConnectionStringFixture.DatabaseConnectionString);
            if (cashBoxes != null)
            {
                foreach (var cashBox in cashBoxes)
                { await _repo.InsertOrUpdateCashBoxAsync(cashBox); }
            }

            if (queues != null)
            {
                foreach (var queue in queues)
                { await _repo.InsertOrUpdateQueueAsync(queue); }
            }

            if (queuesAT != null)
            {
                foreach (var queueAT in queuesAT)
                { await _repo.InsertOrUpdateQueueATAsync(queueAT); }
            }

            if (queuesDE != null)
            {
                foreach (var queueDE in queuesDE)
                { await _repo.InsertOrUpdateQueueDEAsync(queueDE); }
            }

            if (queuesFR != null)
            {
                foreach (var queueFR in queuesFR)
                { await _repo.InsertOrUpdateQueueFRAsync(queueFR); }
            }

            if (queuesME != null)
            {
                foreach (var queueME in queuesME)
                { await _repo.InsertOrUpdateQueueMEAsync(queueME); }
            }

            if (signatureCreateUnitsAT != null)
            {
                foreach (var scuAT in signatureCreateUnitsAT)
                { await _repo.InsertOrUpdateSignaturCreationUnitATAsync(scuAT); }
            }

            if (signatureCreateUnitsDE != null)
            {
                foreach (var scuDE in signatureCreateUnitsDE)
                { await _repo.InsertOrUpdateSignaturCreationUnitDEAsync(scuDE); }
            }

            if (signatureCreateUnitsFR != null)
            {
                foreach (var scuFR in signatureCreateUnitsFR)
                { await _repo.InsertOrUpdateSignaturCreationUnitFRAsync(scuFR); }
            }

            if (signatureCreateUnitsME != null)
            {
                foreach (var scuME in signatureCreateUnitsME)
                { await _repo.InsertOrUpdateSignaturCreationUnitMEAsync(scuME); }
            }

            return _repo;
        }

        //Clear Database before each test
        public override void DisposeDatabase()
        {
            using (var mySqlConnetion = new MySqlConnection(MySQLConnectionStringFixture.DatabaseConnectionString))
            {
                mySqlConnetion.Open();
                using (var command = new MySqlCommand("", mySqlConnetion))
                {
                    command.CommandText = $@"DELETE FROM {TableNames.FtCashBox}";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DELETE FROM {TableNames.FtQueue}";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DELETE FROM {TableNames.FtQueueAT}";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DELETE FROM {TableNames.FtQueueDE}";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DELETE FROM {TableNames.FtQueueFR}";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DELETE FROM {TableNames.FtQueueME}";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DELETE FROM {TableNames.FtSignaturCreationUnitAT}";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DELETE FROM {TableNames.FtSignaturCreationUnitDE}";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DELETE FROM {TableNames.FtSignaturCreationUnitFR}";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DELETE FROM {TableNames.FtSignaturCreationUnitME}";
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
