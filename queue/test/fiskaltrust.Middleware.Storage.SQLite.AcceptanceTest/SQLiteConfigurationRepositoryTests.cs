using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.Middleware.Storage.SQLite.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Storage.SQLite.Connection;
using fiskaltrust.Middleware.Storage.SQLite.DatabaseInitialization;
using fiskaltrust.Middleware.Storage.SQLite.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;

namespace fiskaltrust.Middleware.Storage.SQLite.AcceptanceTest
{
    public class SQLiteConfigurationRepositoryTests : AbstractConfigurationRepositoryTests
    {
        private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Guid.NewGuid().ToString());
        private SQLiteConfigurationRepository _repo;
        private readonly SqliteConnectionFactory _sqliteConnectionFactory = new SqliteConnectionFactory();

        public override async Task<IReadOnlyConfigurationRepository> CreateReadOnlyRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null,IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftQueueME> queuesME = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null, IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null) => await CreateRepository(cashBoxes, queues, queuesAT, queuesDE, queuesFR, queuesME, signatureCreateUnitsAT, signatureCreateUnitsDE, signatureCreateUnitsFR, signatureCreateUnitsME);

        public override async Task<IConfigurationRepository> CreateRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftQueueME> queuesME = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null, IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null)
        {
            var databasMigrator = new DatabaseMigrator(_sqliteConnectionFactory, 30 * 60, _path, new Dictionary<string, object>(), Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databasMigrator.MigrateAsync();

            _repo = new SQLiteConfigurationRepository(_sqliteConnectionFactory, _path);
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

        public override void DisposeDatabase()
        {
            _sqliteConnectionFactory.Dispose();
            if (File.Exists(_path))
            {
                FileHelpers.WaitUntilFileIsAccessible(_path);
                File.Delete(_path);
            }
        }
    }
}
