using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR.TempSpace;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.SQLite.Connection;
using fiskaltrust.Middleware.Storage.SQLite.DatabaseInitialization;
using fiskaltrust.Middleware.Storage.SQLite.Repositories.FR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Storage.SQLite.AcceptanceTest
{
    public class SQLiteJournalFRCopyPayloadRepositoryTests : AbstractCopyPayloadRepositoryTests, IDisposable
    {
        private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Guid.NewGuid().ToString());
        private SQLiteJournalFRCopyPayloadRepository _repo;
        private readonly SqliteConnectionFactory _sqliteConnectionFactory = new();

        public SQLiteJournalFRCopyPayloadRepositoryTests()
        {
            Init().Wait();
        }

        public async Task Init()
        {
            var databasMigrator = new DatabaseMigrator(_sqliteConnectionFactory, 30 * 60, _path, new Dictionary<string, object>(), Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databasMigrator.MigrateAsync();

            _repo = new SQLiteJournalFRCopyPayloadRepository(_sqliteConnectionFactory, _path);
        }

        protected override Task<IJournalFRCopyPayloadRepository> CreateRepository()
        {
            return Task.FromResult<IJournalFRCopyPayloadRepository>(_repo ??= new SQLiteJournalFRCopyPayloadRepository(_sqliteConnectionFactory, _path));
        }

        protected override Task DisposeDatabase()
        {
            _sqliteConnectionFactory.Dispose();
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            DisposeDatabase().Wait();
        }
    }
}