using System;
using System.Data.Entity.Infrastructure;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Storage.Ef;
using fiskaltrust.Middleware.Storage.EF.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.EF.Repositories.FR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Storage.EF.AcceptanceTest
{
    [Collection(EfStorageCollectionFixture.CollectionName)]
    public class EfJournalFRCopyPayloadRepositoryAcceptanceTests : IDisposable
    {
        private readonly EfJournalFRCopyPayloadRepository _repo;
        private MiddlewareDbContext _dbContext;

        public EfJournalFRCopyPayloadRepositoryAcceptanceTests()
        {
            _repo = CreateRepository();
        }

        private EfJournalFRCopyPayloadRepository CreateRepository()
        {
            var queueId = Guid.NewGuid();
            _dbContext = new MiddlewareDbContext(EfConnectionStringFixture.DatabaseConnectionString, queueId);
            EfStorageBootstrapper.Update(EfConnectionStringFixture.DatabaseConnectionString, 30 * 60, queueId,
                Mock.Of<ILogger<IMiddlewareBootstrapper>>());

            return new EfJournalFRCopyPayloadRepository(_dbContext);
        }

        [Fact]
        public async Task GivenNewPayloads_WhenInserted_ThenCorrectNumberOfCopiesShouldBeReturned()
        {
            var payload1 = CreatePayload("test1", "12345", "receipt1", "ref1", "cert123");
            var payload1b = CreatePayload("test1", "12345", "receipt1", "ref1", "cert123");
            var payload2 = CreatePayload("test2", "54321", "receipt2", "ref2", "cert456");

            await _repo.InsertAsync(payload1);
            await _repo.InsertAsync(payload1b);
            await _repo.InsertAsync(payload2);

            var countReference1 = await _repo.GetCountOfCopiesAsync("ref1");
            var countReference2 = await _repo.GetCountOfCopiesAsync("ref2");

            Assert.Equal(2, countReference1);
            Assert.Equal(1, countReference2);
        }

        [Fact]
        public async Task InsertingSameEntityTwice_ShouldThrowException()
        {
            var payload1 = CreatePayload("test1", "12345", "receipt1", "ref1", "cert123");

            await _repo.InsertAsync(payload1);

            await Assert.ThrowsAsync<DbUpdateException>(() => _repo.InsertAsync(payload1));
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

        public void Dispose()
        {
            _dbContext.Set<ftJournalFRCopyPayload>().RemoveRange(_dbContext.Set<ftJournalFRCopyPayload>());
            _dbContext.SaveChanges();
            _dbContext.Dispose();
        }
    }
}
