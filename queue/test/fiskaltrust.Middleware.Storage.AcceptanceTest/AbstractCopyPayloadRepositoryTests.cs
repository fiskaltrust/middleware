using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AcceptanceTest
{
    public abstract class AbstractCopyPayloadRepositoryTests
    {
        protected abstract Task<IJournalFRCopyPayloadRepository> CreateRepository();
        protected abstract Task DisposeDatabase();

        [Fact]
        public async Task CanInsertAndRetrieveCopyPayload()
        {
            var repo = await CreateRepository();

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

            await repo.InsertAsync(payload);

            Assert.Equal(payload, await repo.GetAsync(payload.QueueItemId));
        }
    }
}