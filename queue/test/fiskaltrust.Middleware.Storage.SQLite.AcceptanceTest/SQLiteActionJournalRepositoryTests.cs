﻿using System;
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
    public class SQLiteActionJournalRepositoryTests : AbstractActionJournalRepositoryTests
    {
        private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Guid.NewGuid().ToString());
        private SQLiteActionJournalRepository _repo;
        private readonly SqliteConnectionFactory _sqliteConnectionFactory = new SqliteConnectionFactory();

        public override async Task<IReadOnlyActionJournalRepository> CreateReadOnlyRepository(IEnumerable<ftActionJournal> entries) => await CreateRepository(entries);

        public override async Task<IActionJournalRepository> CreateRepository(IEnumerable<ftActionJournal> entries)
        {
            var databasMigrator = new DatabaseMigrator(_sqliteConnectionFactory, 30 * 60, _path, new Dictionary<string, object>(), Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databasMigrator.MigrateAsync();

            _repo = new SQLiteActionJournalRepository(_sqliteConnectionFactory, _path);
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
