using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
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

        private async Task Init()
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
        
        
        [Fact]
        public async Task GetCountOfCopiesAsync_ShouldReturnCorrectNumberOfCopies()
        {
            var repo = await CreateRepository();

            var reference1 = "ref1";
            var reference2 = "ref2";

            var payload1 = new ftJournalFRCopyPayload
            {
                QueueId = Guid.NewGuid(),
                CashBoxIdentification = "test1",
                Siret = "12345",
                ReceiptId = "receipt1",
                ReceiptMoment = DateTime.UtcNow,
                QueueItemId = Guid.NewGuid(),
                CopiedReceiptReference = reference1,
                CertificateSerialNumber = "cert123",
                TimeStamp = DateTime.UtcNow.Ticks
            };

            var payload1b = new ftJournalFRCopyPayload
            {
                QueueId = Guid.NewGuid(),
                CashBoxIdentification = "test1",
                Siret = "12345",
                ReceiptId = "receipt1",
                ReceiptMoment = DateTime.UtcNow,
                QueueItemId = Guid.NewGuid(),
                CopiedReceiptReference = reference1,
                CertificateSerialNumber = "cert123",
                TimeStamp = DateTime.UtcNow.Ticks
            };

            var payload2 = new ftJournalFRCopyPayload
            {
                QueueId = Guid.NewGuid(),
                CashBoxIdentification = "test2",
                Siret = "54321",
                ReceiptId = "receipt2",
                ReceiptMoment = DateTime.UtcNow,
                QueueItemId = Guid.NewGuid(),
                CopiedReceiptReference = reference2,
                CertificateSerialNumber = "cert456",
                TimeStamp = DateTime.UtcNow.Ticks
            };

            await repo.InsertAsync(payload1);
            await repo.InsertAsync(payload1b);
            await repo.InsertAsync(payload2);

            var countReference1 = await repo.GetCountOfCopiesAsync(reference1);
            var countReference2 = await repo.GetCountOfCopiesAsync(reference2);

            Assert.Equal(2, countReference1);
            Assert.Equal(1, countReference2);
        }
    }
}