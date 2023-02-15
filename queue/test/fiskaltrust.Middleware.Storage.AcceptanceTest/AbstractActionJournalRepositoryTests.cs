using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AcceptanceTest
{
    public abstract class AbstractActionJournalRepositoryTests : IDisposable
    {
        public abstract Task<IActionJournalRepository> CreateRepository(IEnumerable<ftActionJournal> entries);
        public abstract Task<IReadOnlyActionJournalRepository> CreateReadOnlyRepository(IEnumerable<ftActionJournal> entries);

        public virtual void DisposeDatabase() { return; }

        public void Dispose() => DisposeDatabase();

        [Fact]
        public async Task GetAsync_ShouldReturnAllEntriesThatExistInRepository()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftActionJournal>(10);

            var sut = await CreateReadOnlyRepository(expectedEntries);
            var actualEntries = await sut.GetAsync();

            actualEntries.Should().BeEquivalentTo(expectedEntries);
        }

        [Fact]
        public async Task GetAsync_WithId_ShouldReturn_TheElementWithTheGivenId_IfTheIdExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftActionJournal>(10).ToList();
            var expectedEntry = entries[4];

            var sut = await CreateReadOnlyRepository(entries);
            var actualEntry = await sut.GetAsync(expectedEntry.ftActionJournalId);

            actualEntry.Should().BeEquivalentTo(expectedEntry);
        }

        [Fact]
        public async Task GetAsync_WithId_ShouldReturn_Null_IfTheGivenIdDoesntExist()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftActionJournal>(10).ToList();

            var sut = await CreateReadOnlyRepository(entries);
            var actualEntry = await sut.GetAsync(Guid.Empty);

            actualEntry.Should().BeNull();
        }


        [Fact]
        public async Task GetByTimeStampAsync_ShouldReturnAllEntries_WithinAGivenTimeStamp_ShouldReturnOnlyTheseEntries()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftActionJournal>(10).ToList();
            expectedEntries = await StorageTestFixtureProvider.SetDifferentTimestamps(expectedEntries).ConfigureAwait(false);
            expectedEntries = expectedEntries.OrderBy(x => x.TimeStamp).ToList();
            var sut = await CreateRepository(expectedEntries);

            var allEntries = (await sut.GetAsync()).OrderBy(x => x.TimeStamp).ToList();
            var firstSearchedEntryTimeStamp = allEntries[1].TimeStamp;
            var lastSearchedEntryTimeStamp = allEntries[5].TimeStamp;

            var actualEntries = await ((IMiddlewareRepository<ftActionJournal>) sut).GetByTimeStampRangeAsync(firstSearchedEntryTimeStamp, lastSearchedEntryTimeStamp).ToListAsync();

            actualEntries.Should().BeEquivalentTo(allEntries.Skip(1).Take(5));
        }

        [Fact]
        public async Task GetByTimeStampAsync_ShouldReturnAllEntries_FromAGivenTimeStamp_ShouldReturnOnlyTheseEntries()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftActionJournal>(10).ToList();
            expectedEntries = await StorageTestFixtureProvider.SetDifferentTimestamps(expectedEntries).ConfigureAwait(false);
            expectedEntries = expectedEntries.OrderBy(x => x.TimeStamp).ToList();
            var sut = await CreateRepository(expectedEntries);

            var allEntries = (await sut.GetAsync()).OrderBy(x => x.TimeStamp).ToList();
            var firstSearchedEntryTimeStamp = allEntries[1].TimeStamp;

            var actualEntries = await ((IMiddlewareRepository<ftActionJournal>) sut).GetEntriesOnOrAfterTimeStampAsync(firstSearchedEntryTimeStamp).ToListAsync();

            actualEntries.Should().BeEquivalentTo(allEntries.Skip(1));
        }

        [Fact]
        public async Task GetByTimeStampAsync_ShouldReturnAllEntries_FromAGivenTimeStamp_WithTake_ShouldReturnOnlyTheSpecifiedAmountOfEntries()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftActionJournal>(10).ToList();
            expectedEntries = await StorageTestFixtureProvider.SetDifferentTimestamps(expectedEntries).ConfigureAwait(false);
            expectedEntries = expectedEntries.OrderBy(x => x.TimeStamp).ToList();
            var sut = await CreateRepository(expectedEntries);

            var allEntries = (await sut.GetAsync()).OrderBy(x => x.TimeStamp).ToList();
            var firstSearchedEntryTimeStamp = allEntries[1].TimeStamp;

            var actualEntries = await ((IMiddlewareRepository<ftActionJournal>) sut).GetEntriesOnOrAfterTimeStampAsync(firstSearchedEntryTimeStamp, 2).ToListAsync();

            actualEntries.Should().BeEquivalentTo(allEntries.Skip(1).Take(2));
        }

        [Fact]
        public async Task InsertAsync_ShouldAddEntry_ToTheDatabase()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftActionJournal>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftActionJournal>();

            var sut = await CreateRepository(entries);
            await sut.InsertAsync(entryToInsert);

            var insertedEntry = await sut.GetAsync(entryToInsert.ftActionJournalId);
            insertedEntry.Should().BeEquivalentTo(entryToInsert);
        }

        [Fact]
        public virtual async Task InsertAsync_ShouldThrowException_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftActionJournal>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftActionJournal>();

            var sut = await CreateRepository(entries);
            Func<Task> action = async () => await sut.InsertAsync(entries[0]);

            await action.Should().ThrowExactlyAsync<Exception>();
        }

        [Fact]
        public async Task InsertAsync_ShouldUpdateTimeStampOfTheInsertedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftActionJournal>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftActionJournal>();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entryToInsert.TimeStamp = initialTimeStamp;

            var sut = await CreateRepository(entries);
            await sut.InsertAsync(entryToInsert);

            var insertedEntry = await sut.GetAsync(entryToInsert.ftActionJournalId);
            insertedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }
    }
}
