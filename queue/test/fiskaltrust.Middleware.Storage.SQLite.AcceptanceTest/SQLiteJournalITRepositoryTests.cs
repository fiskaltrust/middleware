﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.SQLite.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Storage.SQLite.Connection;
using fiskaltrust.Middleware.Storage.SQLite.DatabaseInitialization;
using fiskaltrust.Middleware.Storage.SQLite.Repositories.ME;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;

namespace fiskaltrust.Middleware.Storage.SQLite.AcceptanceTest
{
    public class SQLiteJournalITRepositoryTests : AbstractJournalITRepositoryTests
    {
        private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Guid.NewGuid().ToString());
        private SQLiteJournalITRepository _repo;
        private readonly SqliteConnectionFactory _sqliteConnectionFactory = new SqliteConnectionFactory();

        public override async Task<IReadOnlyJournalITRepository> CreateReadOnlyRepository(IEnumerable<ftJournalIT> entries) => await CreateRepository(entries);

        public override async Task<IMiddlewareJournalITRepository> CreateRepository(IEnumerable<ftJournalIT> entries)
        {
            var databasMigrator = new DatabaseMigrator(_sqliteConnectionFactory, 30 * 60, _path, new Dictionary<string, object>(), Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databasMigrator.MigrateAsync();

            _repo = new SQLiteJournalITRepository(_sqliteConnectionFactory, _path);
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
