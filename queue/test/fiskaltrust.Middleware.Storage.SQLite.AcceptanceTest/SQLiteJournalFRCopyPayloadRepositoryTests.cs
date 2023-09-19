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

        private ftJournalFRCopyPayload CreatePayload(string cashBoxId, string siret, string receiptId, string copiedReceiptRef, string certSerial)
        {
            return new ftJournalFRCopyPayload
            {
                QueueId = Guid.NewGuid(),
                CashBoxIdentification = cashBoxId,
                Siret = siret,
                ReceiptId = receiptId,
                ReceiptMoment = DateTime.UtcNow,
                QueueItemId = Guid.NewGuid(),
                CopiedReceiptReference = copiedReceiptRef,
                CertificateSerialNumber = certSerial,
                TimeStamp = DateTime.UtcNow.Ticks
            };
        }

        [Fact]
        public async Task GetCountOfCopiesAsync_ShouldReturnCorrectNumberOfCopies()
        {
            var repo = await CreateRepository();

            var payload1 = CreatePayload("test1", "12345", "receipt1", "ref1", "cert123");
            var payload1b = CreatePayload("test1", "12345", "receipt1", "ref1", "cert123");
            var payload2 = CreatePayload("test2", "54321", "receipt2", "ref2", "cert456");

            await repo.InsertAsync(payload1);
            await repo.InsertAsync(payload1b);
            await repo.InsertAsync(payload2);

            var countReference1 = await repo.GetCountOfCopiesAsync("ref1");
            var countReference2 = await repo.GetCountOfCopiesAsync("ref2");

            Assert.Equal(2, countReference1);
            Assert.Equal(1, countReference2);
        }

        [Fact]
        public async Task InsertingSameEntityTwice_ShouldThrowException()
        {
            var repo = await CreateRepository();

            var payload1 = CreatePayload("test1", "12345", "receipt1", "ref1", "cert123");

            await repo.InsertAsync(payload1);
            
            await Assert.ThrowsAsync<System.Data.DataException>(() => repo.InsertAsync(payload1));
        }
    }
}