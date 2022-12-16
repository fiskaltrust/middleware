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
    public abstract class AbstractReceiptJournalRepositoryTests : IDisposable
    {
        public abstract Task<IReceiptJournalRepository> CreateRepository(IEnumerable<ftReceiptJournal> entries);
        public abstract Task<IReadOnlyReceiptJournalRepository> CreateReadOnlyRepository(IEnumerable<ftReceiptJournal> entries);

        public virtual void DisposeDatabase() { return; }

        public void Dispose() => DisposeDatabase();

        [Fact]
        public async Task GetAsync_ShouldReturnAllReceiptJournalsThatExistInRepository()
        {
            var expectedEntry = StorageTestFixtureProvider.GetFixture().CreateMany<ftReceiptJournal>(10);

            var sut = await CreateReadOnlyRepository(expectedEntry);
            var actualEntry = await sut.GetAsync();

            actualEntry.Should().BeEquivalentTo(expectedEntry);
        }

        [Fact]
        public async Task GetAsync_WithId_ShouldReturn_TheElementWithTheGivenId_IfTheIdExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftReceiptJournal>(10).ToList();
            var expectedEntry = entries[4];

            var sut = await CreateReadOnlyRepository(entries);
            var actualEntry = await sut.GetAsync(expectedEntry.ftReceiptJournalId);

            actualEntry.Should().BeEquivalentTo(expectedEntry);
        }

        [Fact]
        public async Task GetAsync_WithId_ShouldReturn_Null_IfTheGivenIdDoesntExist()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftReceiptJournal>(10).ToList();

            var sut = await CreateReadOnlyRepository(entries);
            var actualEntry = await sut.GetAsync(Guid.Empty);

            actualEntry.Should().BeNull();
        }

        [Fact]
        public async Task GetByTimeStampAsync_ShouldReturnAllEntries_WithinAGivenTimeStamp_ShouldReturnOnlyTheseEntries()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftReceiptJournal>(10).OrderByDescending(x => x.TimeStamp).ToList();

            var sut = await CreateRepository(expectedEntries);

            var allEntries = (await sut.GetAsync()).OrderByDescending(x => x.TimeStamp).ToList();
            var firstSearchedEntryTimeStamp = allEntries[5].TimeStamp;
            var lastSearchedEntryTimeStamp = allEntries[1].TimeStamp;

            var actualEntries = await ((IMiddlewareRepository<ftReceiptJournal>) sut).GetByTimeStampRangeAsync(firstSearchedEntryTimeStamp, lastSearchedEntryTimeStamp).ToListAsync();

            actualEntries.Should().BeEquivalentTo(allEntries.Skip(1).Take(5));
        }

        [Fact]
        public async Task GetByTimeStampAsync_ShouldReturnAllEntries_FromAGivenTimeStamp_ShouldReturnOnlyTheseEntries()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftReceiptJournal>(10).OrderBy(x => x.TimeStamp).ToList();

            var sut = await CreateRepository(expectedEntries);

            var allEntries = (await sut.GetAsync()).OrderBy(x => x.TimeStamp).ToList();
            var firstSearchedEntryTimeStamp = allEntries[1].TimeStamp;

            var actualEntries = await ((IMiddlewareRepository<ftReceiptJournal>) sut).GetEntriesOnOrAfterTimeStampAsync(firstSearchedEntryTimeStamp).ToListAsync();

            actualEntries.Should().BeEquivalentTo(allEntries.Skip(1));
        }

        [Fact]
        public async Task GetByTimeStampAsync_ShouldReturnAllEntries_FromAGivenTimeStamp_WithTake_ShouldReturnOnlyTheSpecifiedAmountOfEntries()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftReceiptJournal>(10).OrderBy(x => x.TimeStamp).ToList();

            var sut = await CreateRepository(expectedEntries);

            var allEntries = (await sut.GetAsync()).OrderBy(x => x.TimeStamp).ToList();
            var firstSearchedEntryTimeStamp = allEntries[1].TimeStamp;

            var actualEntries = await ((IMiddlewareRepository<ftReceiptJournal>) sut).GetEntriesOnOrAfterTimeStampAsync(firstSearchedEntryTimeStamp, 2).ToListAsync();

            actualEntries.Should().BeEquivalentTo(allEntries.Skip(1).Take(2));
        }

        [Fact]
        public async Task InsertAsync_ShouldAddEntry_ToTheDatabase()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftReceiptJournal>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftReceiptJournal>();

            var sut = await CreateRepository(entries);
            await sut.InsertAsync(entryToInsert);

            var insertedEntry = await sut.GetAsync(entryToInsert.ftReceiptJournalId);
            insertedEntry.Should().BeEquivalentTo(entryToInsert);
        }

        [Fact]
        public async Task InsertAsync_ShouldThrowException_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftReceiptJournal>(10).ToList();

            var sut = await CreateRepository(entries);
            Func<Task> action = async () => await sut.InsertAsync(entries[0]);

            await action.Should().ThrowExactlyAsync<Exception>();
        }

        [Fact]
        public async Task InsertAsync_ShouldUpdateTimeStampOfTheInsertedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftReceiptJournal>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftReceiptJournal>();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entryToInsert.TimeStamp = initialTimeStamp;

            var sut = await CreateRepository(entries);
            await sut.InsertAsync(entryToInsert);

            var insertedEntry = await sut.GetAsync(entryToInsert.ftReceiptJournalId);
            insertedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }
    }
}
