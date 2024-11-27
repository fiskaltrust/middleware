using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.Middleware.Storage.SQLite.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Storage.SQLite.Connection;
using fiskaltrust.Middleware.Storage.SQLite.DatabaseInitialization;
using fiskaltrust.Middleware.Storage.SQLite.Repositories.FR;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;
using ftJournalFRCopyPayload = fiskaltrust.storage.V0.ftJournalFRCopyPayload;

namespace fiskaltrust.Middleware.Storage.SQLite.AcceptanceTest
{
    public class SQLiteJournalFRCopyPayloadRepositoryTests : AbstractJournalFRCopyPayloadRepositoryTests
    {
        private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Guid.NewGuid().ToString());
        private SQLiteJournalFRCopyPayloadRepository _repo;
        private readonly SqliteConnectionFactory _sqliteConnectionFactory = new SqliteConnectionFactory();

        public override async Task<IJournalFRCopyPayloadRepository> CreateRepository(IEnumerable<ftJournalFRCopyPayload> entries)
        {
            var databasMigrator = new DatabaseMigrator(_sqliteConnectionFactory, 30 * 60, _path, new Dictionary<string, object>(), Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databasMigrator.MigrateAsync();

            _repo = new SQLiteJournalFRCopyPayloadRepository(_sqliteConnectionFactory, _path);
            foreach (var entry in entries)
            { await _repo.InsertAsync(entry); }
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