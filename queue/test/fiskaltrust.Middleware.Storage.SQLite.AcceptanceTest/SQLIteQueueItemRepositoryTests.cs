using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using AutoFixture;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.Middleware.Storage.SQLite.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Storage.SQLite.Connection;
using fiskaltrust.Middleware.Storage.SQLite.DatabaseInitialization;
using fiskaltrust.Middleware.Storage.SQLite.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Linq;

namespace fiskaltrust.Middleware.Storage.SQLite.AcceptanceTest
{
    public class SQLiteQueueItemRepositoryTests : AbstractQueueItemRepositoryTests
    {
        private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Guid.NewGuid().ToString());
        private SQLiteQueueItemRepository _repo;
        private readonly SqliteConnectionFactory _sqliteConnectionFactory = new SqliteConnectionFactory();

        public override async Task<IMiddlewareQueueItemRepository> CreateRepository(string path)
        {
            var databasMigrator = new DatabaseMigrator(_sqliteConnectionFactory, 30 * 60, path, new Dictionary<string, object>(), Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databasMigrator.MigrateAsync();

            return new SQLiteQueueItemRepository(_sqliteConnectionFactory, path);
        }

        public override async Task<IReadOnlyQueueItemRepository> CreateReadOnlyRepository(IEnumerable<ftQueueItem> entries) => await CreateRepository(entries);

        public override async Task<IMiddlewareQueueItemRepository> CreateRepository(IEnumerable<ftQueueItem> entries)
        {
            var databasMigrator = new DatabaseMigrator(_sqliteConnectionFactory, 30 * 60, _path, new Dictionary<string, object>(), Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databasMigrator.MigrateAsync();
            await SetQueueRowAndTimeStamp(entries.ToList());
            _repo = new SQLiteQueueItemRepository(_sqliteConnectionFactory, _path);
            foreach (var entry in entries)
            { await _repo.InsertOrUpdateAsync(entry); }
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
