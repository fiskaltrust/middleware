using System;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using FluentAssertions;
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

            var payload = StorageTestFixtureProvider.GetFixture().Create<ftJournalFRCopyPayload>();

            await repo.InsertAsync(payload);

            payload.Should().BeEquivalentTo(await repo.GetAsync(payload.QueueItemId));
        }
    }
}