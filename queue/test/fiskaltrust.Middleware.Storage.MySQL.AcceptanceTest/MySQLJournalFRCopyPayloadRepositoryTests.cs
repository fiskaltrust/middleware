using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.MySQL.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.MySQL.DatabaseInitialization;
using fiskaltrust.Middleware.Storage.MySQL.Repositories.FR;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;
using MySqlConnector;
using Xunit;

namespace fiskaltrust.Middleware.Storage.MySQL.AcceptanceTest
{
    [Collection(MySQLStorageCollectionFixture.CollectionName)]
    public class MySQLJournalFRCopyPayloadRepositoryTests : AbstractCopyPayloadRepositoryTests, IDisposable
    {
        private MySQLJournalFRCopyPayloadRepository _repo;

        public MySQLJournalFRCopyPayloadRepositoryTests()
        {
            Init().Wait();
        }

        private async Task Init()
        {
            try
            {
                var databasMigrator = new DatabaseMigrator(MySQLConnectionStringFixture.ServerConnectionString, 30 * 60, MySQLConnectionStringFixture.QueueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());
                await databasMigrator.MigrateAsync();
                _repo = new MySQLJournalFRCopyPayloadRepository(MySQLConnectionStringFixture.DatabaseConnectionString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during initialization: {ex.Message}");
                throw;
            }
        }

        protected override Task<IJournalFRCopyPayloadRepository> CreateRepository()
        {
            return Task.FromResult<IJournalFRCopyPayloadRepository>(_repo ??= new MySQLJournalFRCopyPayloadRepository(MySQLConnectionStringFixture.DatabaseConnectionString));
        }

        protected override Task DisposeDatabase()
        {
            using (var mySqlConnection = new MySqlConnection(MySQLConnectionStringFixture.DatabaseConnectionString))
            {
                mySqlConnection.Open();
                using (var command = new MySqlCommand($@"DELETE FROM {TableNames.FtJournalFRCopyPayload}", mySqlConnection))
                {
                    command.ExecuteNonQuery();
                }
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
            
            await Assert.ThrowsAsync<MySqlException>(() => repo.InsertAsync(payload1));
        }
    }
}
