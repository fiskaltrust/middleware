using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.EFCore.AcceptanceTest.PostgreSQL.Fixtures;
using fiskaltrust.Middleware.Storage.EFCore.Repositories.FR;
using fiskaltrust.storage.V0;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace fiskaltrust.Middleware.Storage.EFCore.AcceptanceTest.PostgreSQL
{
    [Collection(EFCorePostgreSQLStorageCollectionFixture.CollectionName)]
    public class EFCorePostgreSQLJournalFRCopyPayloadRepositoryTests
    {
        private readonly EFCorePostgreSQLStorageCollectionFixture _fixture;

        public EFCorePostgreSQLJournalFRCopyPayloadRepositoryTests(EFCorePostgreSQLStorageCollectionFixture fixture) => _fixture = fixture;

        private Task<IJournalFRCopyPayloadRepository> CreateRepository()
        {
            return Task.FromResult<IJournalFRCopyPayloadRepository>(new EFCoreJournalFRCopyPayloadRepository(_fixture.Context));
        }

        [Fact]
        public void DisposeDatabase() => _ = EFCorePostgreSQLStorageCollectionFixture.TruncateTableAsync("ftJournalFRCopyPayload");

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

            await Assert.ThrowsAsync<DbUpdateException>(() => repo.InsertAsync(payload1));
        }
    }
}
