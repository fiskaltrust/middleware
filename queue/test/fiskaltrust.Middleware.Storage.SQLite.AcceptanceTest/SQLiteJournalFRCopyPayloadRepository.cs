using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR.TempSpace;
using fiskaltrust.Middleware.Storage.SQLite.Connection;
using fiskaltrust.Middleware.Storage.SQLite.DatabaseInitialization;
using fiskaltrust.Middleware.Storage.SQLite.Repositories.FR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Storage.SQLite.AcceptanceTest
{
    public class SQLiteJournalFRCopyPayloadRepositoryTests : IDisposable
    {
        private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Guid.NewGuid().ToString());
        private SQLiteJournalFRCopyPayloadRepository _repo;
        private readonly SqliteConnectionFactory _sqliteConnectionFactory = new SqliteConnectionFactory();

        public SQLiteJournalFRCopyPayloadRepositoryTests()
        {
            Init().Wait();
        }

        [Fact]
        public async Task Init()
        {
            var databasMigrator = new DatabaseMigrator(_sqliteConnectionFactory, 30 * 60, _path, new Dictionary<string, object>(), Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databasMigrator.MigrateAsync();

            _repo = new SQLiteJournalFRCopyPayloadRepository(_sqliteConnectionFactory, _path);
        }

        [Fact]
        public async Task CanInsertAndRetrieveCopyPayload()
        {
            var payload = new ftJournalFRCopyPayload
            {
                QueueId = Guid.NewGuid(),
                CashBoxIdentification = "test",
                Siret = "12345",
                ReceiptId = "receipt1",
                ReceiptMoment = DateTime.UtcNow,
                QueueItemId = Guid.NewGuid(),
                CopiedReceiptReference = "ref1",
                CertificateSerialNumber = "cert123",
                TimeStamp = DateTime.UtcNow.Ticks
            };

            var inserted = await _repo.InsertAsync(payload);
            var retrieved = await _repo.GetAsync(payload.QueueItemId);

            Assert.True(inserted);
            Assert.NotNull(retrieved);
            Assert.Equal(payload.QueueItemId, retrieved.QueueItemId);
            Assert.Equal(payload.CopiedReceiptReference, retrieved.CopiedReceiptReference);
        }

        public void Dispose()
        {
            _sqliteConnectionFactory.Dispose();
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
    }
}
