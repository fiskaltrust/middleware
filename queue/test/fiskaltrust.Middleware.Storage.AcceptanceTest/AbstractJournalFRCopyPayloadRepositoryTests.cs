using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;
using ftJournalFRCopyPayload = fiskaltrust.Middleware.Contracts.Models.FR.ftJournalFRCopyPayload;

namespace fiskaltrust.Middleware.Storage.AcceptanceTest
{
    public abstract class AbstractJournalFRCopyPayloadRepositoryTests : IDisposable
    {
        public abstract Task<IJournalFRCopyPayloadRepository> CreateRepository(IEnumerable<ftJournalFRCopyPayload> entries);

        public virtual void DisposeDatabase() { return; }

        public void Dispose() => DisposeDatabase();


        [Fact]
        public async Task InsertingSameEntityTwice_ShouldThrowException()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftJournalFRCopyPayload>(10);
            var repo = await CreateRepository(entries);

            await Assert.ThrowsAsync<Exception>(() => repo.InsertAsync(entries.First()));
        }

        [Fact]
        public async Task CanInsertAndRetrieveCopyPayload()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftJournalFRCopyPayload>(10);
            var entry = entries.First();
            var repo = await CreateRepository(entries);

            entry.Should().BeEquivalentTo(await repo.GetAsync(entry.QueueItemId));
        }

        [Fact]
        public async Task GetCountOfCopiesAsync_ShouldReturnCorrectNumberOfCopies()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftJournalFRCopyPayload>(3).ToList();
            entries[0].CopiedReceiptReference = "ref1";
            entries[1].CopiedReceiptReference = "ref1";
            entries[2].CopiedReceiptReference = "ref2";

            var repo = await CreateRepository(entries);

            var countReference1 = await repo.GetCountOfCopiesAsync("ref1");
            var countReference2 = await repo.GetCountOfCopiesAsync("ref2");

            Assert.Equal(2, countReference1);
            Assert.Equal(1, countReference2);
        }
    }
}