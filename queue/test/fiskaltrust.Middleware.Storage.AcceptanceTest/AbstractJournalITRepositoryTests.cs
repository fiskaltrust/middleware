using AutoFixture;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AcceptanceTest
{
    public abstract class AbstractJournalITRepositoryTests : IDisposable
    {
        public abstract Task<IMiddlewareJournalITRepository> CreateRepository(IEnumerable<ftJournalIT> entries);
        public abstract Task<IReadOnlyJournalITRepository> CreateReadOnlyRepository(IEnumerable<ftJournalIT> entries);

        public virtual void DisposeDatabase() { return; }

        public void Dispose() => DisposeDatabase();

        [Fact]
        public async Task GetAsync_ShouldReturnAllEntriesThatExistInRepository()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftJournalIT>(10);

            var sut = await CreateReadOnlyRepository(expectedEntries);
            var actualEntries = await sut.GetAsync();

            actualEntries.Should().BeEquivalentTo(expectedEntries);
        }

        [Fact]
        public async Task GetAsync_WithId_ShouldReturn_TheElementWithTheGivenId_IfTheIdExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftJournalIT>(10).ToList();
            var expectedEntry = entries[4];

            var sut = await CreateReadOnlyRepository(entries);
            var actualEntry = await sut.GetAsync(expectedEntry.ftJournalITId);

            actualEntry.Should().BeEquivalentTo(expectedEntry);
        }

        [Fact]
        public async Task GetAsync_WithId_ShouldReturn_Null_IfTheGivenIdDoesntExist()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftJournalIT>(10).ToList();

            var sut = await CreateReadOnlyRepository(entries);
            var actualEntry = await sut.GetAsync(Guid.Empty);

            actualEntry.Should().BeNull();
        }

        [Fact]
        public async Task GetAsync_ByQueueItemId_ReturnValidObject()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftJournalIT>(10).ToList();

            var queueItemId = Guid.NewGuid();
            entries[0].ftQueueItemId = queueItemId;

            var sut = await CreateRepository(entries);
            var actualEntry = await sut.GetByQueueItemId(queueItemId);

            actualEntry.Should().NotBeNull();
            actualEntry.ftJournalITId.Should().Be(entries[0].ftJournalITId);
        }

        [Fact]
        public async Task InsertAsync_ShouldAddEntry_ToTheDatabase()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftJournalIT>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftJournalIT>();

            var sut = await CreateRepository(entries);
            await sut.InsertAsync(entryToInsert);

            var insertedEntry = await sut.GetAsync(entryToInsert.ftJournalITId);
            insertedEntry.Should().BeEquivalentTo(entryToInsert);
        }

        [Fact]
        public virtual async Task InsertAsync_ShouldUpdateEntry_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftJournalIT>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftJournalIT>();

            var sut = await CreateRepository(entries);
            Func<Task> action = async () => await sut.InsertAsync(entries[0]);

            await action.Should().ThrowExactlyAsync<Exception>();
        }

        [Fact]
        public async Task InsertAsync_ShouldUpdateTimeStampOfTheInsertedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftJournalIT>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftJournalIT>();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entryToInsert.TimeStamp = initialTimeStamp;

            var sut = await CreateRepository(entries);
            await sut.InsertAsync(entryToInsert);

            var insertedEntry = await sut.GetAsync(entryToInsert.ftJournalITId);
            insertedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }
    }
}
