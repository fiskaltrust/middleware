using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AcceptanceTest
{
    public abstract class AbstractConfigurationRepositoryTests : IDisposable
    {
        public abstract Task<IConfigurationRepository> CreateRepository(IEnumerable<ftCashBox> cashBoxes = null,
            IEnumerable<ftQueue> queues = null,
            IEnumerable<ftQueueAT> queuesAT = null,
            IEnumerable<ftQueueDE> queuesDE = null,
            IEnumerable<ftQueueES> queuesES = null,
            IEnumerable<ftQueueFR> queuesFR = null,
            IEnumerable<ftQueueIT> queuesIT = null,
            IEnumerable<ftQueueME> queuesME = null,
            IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null,
            IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null,
            IEnumerable<ftSignaturCreationUnitES> signatureCreateUnitsES = null,
            IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null,
            IEnumerable<ftSignaturCreationUnitIT> signatureCreateUnitsIT = null,
            IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null);

        public abstract Task<IReadOnlyConfigurationRepository> CreateReadOnlyRepository(IEnumerable<ftCashBox> cashBoxes = null,
            IEnumerable<ftQueue> queues = null,
            IEnumerable<ftQueueAT> queuesAT = null,
            IEnumerable<ftQueueDE> queuesDE = null,
            IEnumerable<ftQueueES> queuesES = null,
            IEnumerable<ftQueueFR> queuesFR = null,
            IEnumerable<ftQueueIT> queuesIT = null,
            IEnumerable<ftQueueME> queuesME = null,
            IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null,
            IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null,
            IEnumerable<ftSignaturCreationUnitES> signatureCreateUnitsES = null,
            IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null,
            IEnumerable<ftSignaturCreationUnitIT> signatureCreateUnitsIT = null,
            IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null);

        public virtual void DisposeDatabase() { return; }

        public void Dispose() => DisposeDatabase();

        [Fact]
        public async Task GetCashBoxListAsync_ShouldReturnAllEntriesThatExistInRepository()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftCashBox>(10);

            var sut = await CreateReadOnlyRepository(cashBoxes: expectedEntries);
            var actualEntries = await sut.GetCashBoxListAsync();

            actualEntries.Should().BeEquivalentTo(expectedEntries);
        }

        [Fact]
        public async Task GetCashBoxAsync_WithId_ShouldReturn_TheElementWithTheGivenId_IfTheIdExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftCashBox>(10).ToList();
            var expectedEntry = entries[4];

            var sut = await CreateReadOnlyRepository(cashBoxes: entries);
            var actualEntry = await sut.GetCashBoxAsync(expectedEntry.ftCashBoxId);

            actualEntry.Should().BeEquivalentTo(expectedEntry);
        }

        [Fact]
        public async Task GetCashBoxAsync_WithId_ShouldReturn_Null_IfTheGivenIdDoesntExist()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftCashBox>(10).ToList();

            var sut = await CreateReadOnlyRepository(cashBoxes: entries);
            var actualEntry = await sut.GetCashBoxAsync(Guid.Empty);

            actualEntry.Should().BeNull();
        }

        [Fact]
        public async Task InsertOrUpdateCashBoxAsync_ShouldAddEntry_ToTheDatabase()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftCashBox>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftCashBox>();

            var sut = await CreateRepository(cashBoxes: entries);
            await sut.InsertOrUpdateCashBoxAsync(entryToInsert);

            var insertedEntry = await sut.GetCashBoxAsync(entryToInsert.ftCashBoxId);
            insertedEntry.Should().BeEquivalentTo(entryToInsert);
        }

        [Fact]
        public async Task InsertOrUpdateCashBoxAsync_ShouldUpdateEntry_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftCashBox>(10).ToList();

            var sut = await CreateRepository(cashBoxes: entries);

            var entryToUpdate = await sut.GetCashBoxAsync(entries[0].ftCashBoxId);
            // There is no entry that we can update that would take affect that is why we just try to reapply a existing entry

            await sut.InsertOrUpdateCashBoxAsync(entryToUpdate);

            var updatedEntry = await sut.GetCashBoxAsync(entries[0].ftCashBoxId);

            updatedEntry.ftCashBoxId.Should().Be(entries[0].ftCashBoxId);
        }

        [Fact]
        public async Task InsertOrUpdateCashBoxAsync_ShouldUpdateTimeStampOfTheInsertedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftCashBox>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftCashBox>();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entryToInsert.TimeStamp = initialTimeStamp;

            var sut = await CreateRepository(cashBoxes: entries);
            await sut.InsertOrUpdateCashBoxAsync(entryToInsert);

            var insertedEntry = await sut.GetCashBoxAsync(entryToInsert.ftCashBoxId);
            insertedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }

        [Fact]
        public async Task InsertOrUpdateCashBoxAsync_ShouldUpdateTimeStampOfTheUpdatedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftCashBox>(10).ToList();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entries[0].TimeStamp = initialTimeStamp;
            var sut = await CreateRepository(entries);

            var entryToUpdate = await sut.GetCashBoxAsync(entries[0].ftCashBoxId);
            // There is no entry that we can update that would take affect that is we we just try to reapply a existing entry

            await sut.InsertOrUpdateCashBoxAsync(entries[0]);

            var updatedEntry = await sut.GetCashBoxAsync(entries[0].ftCashBoxId);
            updatedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }

        [Fact]
        public async Task GetQueueListAsync_ShouldReturnAllEntriesThatExistInRepository()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueue>(10);

            var sut = await CreateReadOnlyRepository(queues: expectedEntries);
            var actualEntries = await sut.GetQueueListAsync();

            actualEntries.Should().BeEquivalentTo(expectedEntries);
        }

        [Fact]
        public async Task GetQueueAsync_WithId_ShouldReturn_TheElementWithTheGivenId_IfTheIdExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueue>(10).ToList();
            var expectedEntry = entries[4];

            var sut = await CreateReadOnlyRepository(queues: entries);
            var actualEntry = await sut.GetQueueAsync(expectedEntry.ftQueueId);

            actualEntry.Should().BeEquivalentTo(expectedEntry);
        }

        [Fact]
        public async Task GetQueueAsync_WithId_ShouldReturn_Null_IfTheGivenIdDoesntExist()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueue>(10).ToList();

            var sut = await CreateReadOnlyRepository(queues: entries);
            var actualEntry = await sut.GetQueueAsync(Guid.Empty);

            actualEntry.Should().BeNull();
        }

        [Fact]
        public async Task InsertOrUpdateQueueAsync_ShouldAddEntry_ToTheDatabase()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueue>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftQueue>();

            var sut = await CreateRepository(queues: entries);
            await sut.InsertOrUpdateQueueAsync(entryToInsert);

            var insertedEntry = await sut.GetQueueAsync(entryToInsert.ftQueueId);
            insertedEntry.Should().BeEquivalentTo(entryToInsert);
        }

        [Fact]
        public async Task InsertOrUpdateQueueAsync_ShouldUpdateEntry_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueue>(10).ToList();
            var updatedValue = Guid.NewGuid().ToString();

            var sut = await CreateRepository(queues: entries);

            var entryToUpdate = await sut.GetQueueAsync(entries[0].ftQueueId);
            entryToUpdate.ftReceiptHash = updatedValue;

            await sut.InsertOrUpdateQueueAsync(entryToUpdate);

            var updatedEntry = await sut.GetQueueAsync(entries[0].ftQueueId);

            updatedEntry.ftReceiptHash.Should().BeEquivalentTo(updatedValue);
        }

        [Fact]
        public async Task InsertOrUpdateQueueAsync_ShouldUpdateTimeStampOfTheInsertedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueue>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftQueue>();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entryToInsert.TimeStamp = initialTimeStamp;

            var sut = await CreateRepository(queues: entries);
            await sut.InsertOrUpdateQueueAsync(entryToInsert);

            var insertedEntry = await sut.GetQueueAsync(entryToInsert.ftQueueId);
            insertedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }

        [Fact]
        public async Task InsertOrUpdateQueueAsync_ShouldUpdateTimeStampOfTheUpdatedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueue>(10).ToList();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entries[0].TimeStamp = initialTimeStamp;
            var updatedValue = Guid.NewGuid().ToString();

            var sut = await CreateRepository(queues: entries);

            var entryToUpdate = await sut.GetQueueAsync(entries[0].ftQueueId);
            entryToUpdate.ftReceiptHash = updatedValue;

            await sut.InsertOrUpdateQueueAsync(entries[0]);

            var updatedEntry = await sut.GetQueueAsync(entries[0].ftQueueId);
            updatedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }

        [Fact]
        public async Task GetQueueATListAsync_ShouldReturnAllEntriesThatExistInRepository()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueAT>(10);

            var sut = await CreateReadOnlyRepository(queuesAT: expectedEntries);
            var actualEntries = await sut.GetQueueATListAsync();

            actualEntries.Should().BeEquivalentTo(expectedEntries);
        }

        [Fact]
        public async Task GetQueueATAsync_WithId_ShouldReturn_TheElementWithTheGivenId_IfTheIdExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueAT>(10).ToList();
            var expectedEntry = entries[4];

            var sut = await CreateReadOnlyRepository(queuesAT: entries);
            var actualEntry = await sut.GetQueueATAsync(expectedEntry.ftQueueATId);

            actualEntry.Should().BeEquivalentTo(expectedEntry);
        }

        [Fact]
        public async Task GetQueueATAsync_WithId_ShouldReturn_Null_IfTheGivenIdDoesntExist()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueAT>(10).ToList();

            var sut = await CreateReadOnlyRepository(queuesAT: entries);
            var actualEntry = await sut.GetQueueATAsync(Guid.Empty);

            actualEntry.Should().BeNull();
        }

        [Fact]
        public async Task InsertOrUpdateQueueATAsync_ShouldAddEntry_ToTheDatabase()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueAT>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftQueueAT>();

            var sut = await CreateRepository(queuesAT: entries);
            await sut.InsertOrUpdateQueueATAsync(entryToInsert);

            var insertedEntry = await sut.GetQueueATAsync(entryToInsert.ftQueueATId);
            insertedEntry.Should().BeEquivalentTo(entryToInsert);
        }

        [Fact]
        public async Task InsertOrUpdateQueueATAsync_ShouldUpdateEntry_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueAT>(10).ToList();
            var updatedValue = Guid.NewGuid().ToString();

            var sut = await CreateRepository(queuesAT: entries);

            var entryToUpdate = await sut.GetQueueATAsync(entries[0].ftQueueATId);
            entryToUpdate.CashBoxIdentification = updatedValue;

            await sut.InsertOrUpdateQueueATAsync(entryToUpdate);

            var updatedEntry = await sut.GetQueueATAsync(entries[0].ftQueueATId);

            updatedEntry.CashBoxIdentification.Should().BeEquivalentTo(updatedValue);
        }

        [Fact]
        public async Task InsertOrUpdateQueueATAsync_ShouldUpdateTimeStampOfTheInsertedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueAT>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftQueueAT>();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entryToInsert.TimeStamp = initialTimeStamp;

            var sut = await CreateRepository(queuesAT: entries);
            await sut.InsertOrUpdateQueueATAsync(entryToInsert);

            var insertedEntry = await sut.GetQueueATAsync(entryToInsert.ftQueueATId);
            insertedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }

        [Fact]
        public async Task InsertOrUpdateQueueATAsync_ShouldUpdateTimeStampOfTheUpdatedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueAT>(10).ToList();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entries[0].TimeStamp = initialTimeStamp;
            var updatedValue = Guid.NewGuid().ToString();

            var sut = await CreateRepository(queuesAT: entries);

            var entryToUpdate = await sut.GetQueueATAsync(entries[0].ftQueueATId);
            entryToUpdate.CashBoxIdentification = updatedValue;

            await sut.InsertOrUpdateQueueATAsync(entries[0]);

            var updatedEntry = await sut.GetQueueATAsync(entries[0].ftQueueATId);
            updatedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }

        [Fact]
        public async Task GetQueueDEListAsync_ShouldReturnAllEntriesThatExistInRepository()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueDE>(10);

            var sut = await CreateReadOnlyRepository(queuesDE: expectedEntries);
            var actualEntries = await sut.GetQueueDEListAsync();

            actualEntries.Should().BeEquivalentTo(expectedEntries);
        }

        [Fact]
        public async Task GetQueueDEAsync_WithId_ShouldReturn_TheElementWithTheGivenId_IfTheIdExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueDE>(10).ToList();
            var expectedEntry = entries[4];

            var sut = await CreateReadOnlyRepository(queuesDE: entries);
            var actualEntry = await sut.GetQueueDEAsync(expectedEntry.ftQueueDEId);

            actualEntry.Should().BeEquivalentTo(expectedEntry);
        }

        [Fact]
        public async Task GetQueueDEAsync_WithId_ShouldReturn_Null_IfTheGivenIdDoesntExist()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueDE>(10).ToList();

            var sut = await CreateReadOnlyRepository(queuesDE: entries);
            var actualEntry = await sut.GetQueueDEAsync(Guid.Empty);

            actualEntry.Should().BeNull();
        }

        [Fact]
        public async Task InsertOrUpdateQueueDEAsync_ShouldAddEntry_ToTheDatabase()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueDE>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftQueueDE>();

            var sut = await CreateRepository(queuesDE: entries);
            await sut.InsertOrUpdateQueueDEAsync(entryToInsert);

            var insertedEntry = await sut.GetQueueDEAsync(entryToInsert.ftQueueDEId);
            insertedEntry.Should().BeEquivalentTo(entryToInsert);
        }

        [Fact]
        public async Task InsertOrUpdateQueueDEAsync_ShouldUpdateEntry_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueDE>(10).ToList();
            var updatedValue = Guid.NewGuid().ToString();

            var sut = await CreateRepository(queuesDE: entries);

            var entryToUpdate = await sut.GetQueueDEAsync(entries[0].ftQueueDEId);
            entryToUpdate.LastHash = updatedValue;

            await sut.InsertOrUpdateQueueDEAsync(entryToUpdate);

            var updatedEntry = await sut.GetQueueDEAsync(entries[0].ftQueueDEId);

            updatedEntry.LastHash.Should().BeEquivalentTo(updatedValue);
        }

        [Fact]
        public async Task InsertOrUpdateQueueDEAsync_ShouldUpdateTimeStampOfTheInsertedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueDE>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftQueueDE>();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entryToInsert.TimeStamp = initialTimeStamp;

            var sut = await CreateRepository(queuesDE: entries);
            await sut.InsertOrUpdateQueueDEAsync(entryToInsert);

            var insertedEntry = await sut.GetQueueDEAsync(entryToInsert.ftQueueDEId);
            insertedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }

        [Fact]
        public async Task InsertOrUpdateQueueDEAsync_ShouldUpdateTimeStampOfTheUpdatedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueDE>(10).ToList();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entries[0].TimeStamp = initialTimeStamp;
            var updatedValue = Guid.NewGuid().ToString();

            var sut = await CreateRepository(queuesDE: entries);

            var entryToUpdate = await sut.GetQueueDEAsync(entries[0].ftQueueDEId);
            entryToUpdate.LastHash = updatedValue;

            await sut.InsertOrUpdateQueueDEAsync(entries[0]);

            var updatedEntry = await sut.GetQueueDEAsync(entries[0].ftQueueDEId);
            updatedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }

        [Fact]
        public async Task GetQueueFRListAsync_ShouldReturnAllEntriesThatExistInRepository()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueFR>(10);

            var sut = await CreateReadOnlyRepository(queuesFR: expectedEntries);
            var actualEntries = await sut.GetQueueFRListAsync();

            actualEntries.Should().BeEquivalentTo(expectedEntries);
        }

        [Fact]
        public async Task GetQueueFRAsync_WithId_ShouldReturn_TheElementWithTheGivenId_IfTheIdExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueFR>(10).ToList();
            var expectedEntry = entries[4];

            var sut = await CreateReadOnlyRepository(queuesFR: entries);
            var actualEntry = await sut.GetQueueFRAsync(expectedEntry.ftQueueFRId);

            actualEntry.Should().BeEquivalentTo(expectedEntry);
        }

        [Fact]
        public async Task GetQueueFRAsync_WithId_ShouldReturn_Null_IfTheGivenIdDoesntExist()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueFR>(10).ToList();

            var sut = await CreateReadOnlyRepository(queuesFR: entries);
            var actualEntry = await sut.GetQueueFRAsync(Guid.Empty);

            actualEntry.Should().BeNull();
        }

        [Fact]
        public async Task InsertOrUpdateQueueFRAsync_ShouldAddEntry_ToTheDatabase()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueFR>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftQueueFR>();

            var sut = await CreateRepository(queuesFR: entries);
            await sut.InsertOrUpdateQueueFRAsync(entryToInsert);

            var insertedEntry = await sut.GetQueueFRAsync(entryToInsert.ftQueueFRId);
            insertedEntry.Should().BeEquivalentTo(entryToInsert);
        }

        [Fact]
        public async Task InsertOrUpdateQueueFRAsync_ShouldUpdateEntry_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueFR>(10).ToList();
            var updatedValue = Guid.NewGuid().ToString();

            var sut = await CreateRepository(queuesFR: entries);

            var entryToUpdate = await sut.GetQueueFRAsync(entries[0].ftQueueFRId);
            entryToUpdate.ALastHash = updatedValue;

            await sut.InsertOrUpdateQueueFRAsync(entryToUpdate);

            var updatedEntry = await sut.GetQueueFRAsync(entries[0].ftQueueFRId);

            updatedEntry.ALastHash.Should().BeEquivalentTo(updatedValue);
        }

        [Fact]
        public async Task InsertOrUpdateQueueFRAsync_ShouldUpdateTimeStampOfTheInsertedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueFR>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftQueueFR>();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entryToInsert.TimeStamp = initialTimeStamp;

            var sut = await CreateRepository(queuesFR: entries);
            await sut.InsertOrUpdateQueueFRAsync(entryToInsert);

            var insertedEntry = await sut.GetQueueFRAsync(entryToInsert.ftQueueFRId);
            insertedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }

        [Fact]
        public async Task InsertOrUpdateQueueFRAsync_ShouldUpdateTimeStampOfTheUpdatedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueFR>(10).ToList();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entries[0].TimeStamp = initialTimeStamp;
            var updatedValue = Guid.NewGuid().ToString();

            var sut = await CreateRepository(queuesFR: entries);

            var entryToUpdate = await sut.GetQueueFRAsync(entries[0].ftQueueFRId);
            entryToUpdate.ALastHash = updatedValue;

            await sut.InsertOrUpdateQueueFRAsync(entries[0]);

            var updatedEntry = await sut.GetQueueFRAsync(entries[0].ftQueueFRId);
            updatedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }

        [Fact]
        public async Task GetSignaturCreationUnitATListAsync_ShouldReturnAllEntriesThatExistInRepository()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftSignaturCreationUnitAT>(10);

            var sut = await CreateReadOnlyRepository(signatureCreateUnitsAT: expectedEntries);
            var actualEntries = await sut.GetSignaturCreationUnitATListAsync();

            actualEntries.Should().BeEquivalentTo(expectedEntries);
        }

        [Fact]
        public async Task GetSignaturCreationUnitATAsync_WithId_ShouldReturn_TheElementWithTheGivenId_IfTheIdExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftSignaturCreationUnitAT>(10).ToList();
            var expectedEntry = entries[4];

            var sut = await CreateReadOnlyRepository(signatureCreateUnitsAT: entries);
            var actualEntry = await sut.GetSignaturCreationUnitATAsync(expectedEntry.ftSignaturCreationUnitATId);

            actualEntry.Should().BeEquivalentTo(expectedEntry);
        }

        [Fact]
        public async Task GetSignaturCreationUnitATAsync_WithId_ShouldReturn_Null_IfTheGivenIdDoesntExist()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftSignaturCreationUnitAT>(10).ToList();

            var sut = await CreateReadOnlyRepository(signatureCreateUnitsAT: entries);
            var actualEntry = await sut.GetSignaturCreationUnitATAsync(Guid.Empty);

            actualEntry.Should().BeNull();
        }

        [Fact]
        public async Task InsertOrUpdateSignaturCreationUnitATAsync_ShouldAddEntry_ToTheDatabase()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftSignaturCreationUnitAT>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftSignaturCreationUnitAT>();

            var sut = await CreateRepository(signatureCreateUnitsAT: entries);
            await sut.InsertOrUpdateSignaturCreationUnitATAsync(entryToInsert);

            var insertedEntry = await sut.GetSignaturCreationUnitATAsync(entryToInsert.ftSignaturCreationUnitATId);
            insertedEntry.Should().BeEquivalentTo(entryToInsert);
        }

        [Fact]
        public async Task InsertOrUpdateSignaturCreationUnitATAsync_ShouldUpdateEntry_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftSignaturCreationUnitAT>(10).ToList();
            var updatedValue = Guid.NewGuid().ToString();

            var sut = await CreateRepository(signatureCreateUnitsAT: entries);

            var entryToUpdate = await sut.GetSignaturCreationUnitATAsync(entries[0].ftSignaturCreationUnitATId);
            entryToUpdate.CertificateBase64 = updatedValue;

            await sut.InsertOrUpdateSignaturCreationUnitATAsync(entryToUpdate);

            var updatedEntry = await sut.GetSignaturCreationUnitATAsync(entries[0].ftSignaturCreationUnitATId);

            updatedEntry.CertificateBase64.Should().BeEquivalentTo(updatedValue);
        }

        [Fact]
        public async Task InsertOrUpdateSignaturCreationUnitATAsync_ShouldUpdateTimeStampOfTheInsertedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftSignaturCreationUnitAT>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftSignaturCreationUnitAT>();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entryToInsert.TimeStamp = initialTimeStamp;

            var sut = await CreateRepository(signatureCreateUnitsAT: entries);
            await sut.InsertOrUpdateSignaturCreationUnitATAsync(entryToInsert);

            var insertedEntry = await sut.GetSignaturCreationUnitATAsync(entryToInsert.ftSignaturCreationUnitATId);
            insertedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }

        [Fact]
        public async Task InsertOrUpdateSignaturCreationUnitATAsync_ShouldUpdateTimeStampOfTheUpdatedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftSignaturCreationUnitAT>(10).ToList();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entries[0].TimeStamp = initialTimeStamp;
            var updatedValue = Guid.NewGuid().ToString();

            var sut = await CreateRepository(signatureCreateUnitsAT: entries);

            var entryToUpdate = await sut.GetSignaturCreationUnitATAsync(entries[0].ftSignaturCreationUnitATId);
            entryToUpdate.CertificateBase64 = updatedValue;
            // There is no entry that we can update that would take affect that is we we just try to reapply a existing entry

            await sut.InsertOrUpdateSignaturCreationUnitATAsync(entries[0]);

            var updatedEntry = await sut.GetSignaturCreationUnitATAsync(entries[0].ftSignaturCreationUnitATId);
            updatedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }

        [Fact]
        public async Task GetSignaturCreationUnitDEListAsync_ShouldReturnAllEntriesThatExistInRepository()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftSignaturCreationUnitDE>(10);

            var sut = await CreateReadOnlyRepository(signatureCreateUnitsDE: expectedEntries);
            var actualEntries = await sut.GetSignaturCreationUnitDEListAsync();

            actualEntries.Should().BeEquivalentTo(expectedEntries);
        }

        [Fact]
        public async Task GetSignaturCreationUnitDEAsync_WithId_ShouldReturn_TheElementWithTheGivenId_IfTheIdExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftSignaturCreationUnitDE>(10).ToList();
            var expectedEntry = entries[4];

            var sut = await CreateReadOnlyRepository(signatureCreateUnitsDE: entries);
            var actualEntry = await sut.GetSignaturCreationUnitDEAsync(expectedEntry.ftSignaturCreationUnitDEId);

            actualEntry.Should().BeEquivalentTo(expectedEntry);
        }

        [Fact]
        public async Task GetSignaturCreationUnitDEAsync_WithId_ShouldReturn_Null_IfTheGivenIdDoesntExist()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftSignaturCreationUnitDE>(10).ToList();

            var sut = await CreateReadOnlyRepository(signatureCreateUnitsDE: entries);
            var actualEntry = await sut.GetSignaturCreationUnitDEAsync(Guid.Empty);

            actualEntry.Should().BeNull();
        }

        [Fact]
        public async Task InsertOrUpdateSignaturCreationUnitDEAsync_ShouldAddEntry_ToTheDatabase()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftSignaturCreationUnitDE>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftSignaturCreationUnitDE>();

            var sut = await CreateRepository(signatureCreateUnitsDE: entries);
            await sut.InsertOrUpdateSignaturCreationUnitDEAsync(entryToInsert);

            var insertedEntry = await sut.GetSignaturCreationUnitDEAsync(entryToInsert.ftSignaturCreationUnitDEId);
            insertedEntry.Should().BeEquivalentTo(entryToInsert);
        }

        [Fact]
        public async Task InsertOrUpdateSignaturCreationUnitDEAsync_ShouldUpdateEntry_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftSignaturCreationUnitDE>(10).ToList();
            var updatedValue = Guid.NewGuid().ToString();

            var sut = await CreateRepository(signatureCreateUnitsDE: entries);

            var entryToUpdate = await sut.GetSignaturCreationUnitDEAsync(entries[0].ftSignaturCreationUnitDEId);
            entryToUpdate.TseInfoJson = updatedValue;

            await sut.InsertOrUpdateSignaturCreationUnitDEAsync(entryToUpdate);

            var updatedEntry = await sut.GetSignaturCreationUnitDEAsync(entries[0].ftSignaturCreationUnitDEId);

            updatedEntry.TseInfoJson.Should().BeEquivalentTo(updatedValue);
        }

        [Fact]
        public async Task InsertOrUpdateSignaturCreationUnitDEAsync_ShouldUpdateTimeStampOfTheInsertedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftSignaturCreationUnitDE>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftSignaturCreationUnitDE>();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entryToInsert.TimeStamp = initialTimeStamp;

            var sut = await CreateRepository(signatureCreateUnitsDE: entries);
            await sut.InsertOrUpdateSignaturCreationUnitDEAsync(entryToInsert);

            var insertedEntry = await sut.GetSignaturCreationUnitDEAsync(entryToInsert.ftSignaturCreationUnitDEId);
            insertedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }

        [Fact]
        public async Task InsertOrUpdateSignaturCreationUnitDEAsync_ShouldUpdateTimeStampOfTheUpdatedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftSignaturCreationUnitDE>(10).ToList();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entries[0].TimeStamp = initialTimeStamp;
            var updatedValue = Guid.NewGuid().ToString();

            var sut = await CreateRepository(signatureCreateUnitsDE: entries);

            var entryToUpdate = await sut.GetSignaturCreationUnitDEAsync(entries[0].ftSignaturCreationUnitDEId);
            entryToUpdate.TseInfoJson = updatedValue;
            // There is no entry that we can update that would take affect that is we we just try to reapply a existing entry

            await sut.InsertOrUpdateSignaturCreationUnitDEAsync(entries[0]);

            var updatedEntry = await sut.GetSignaturCreationUnitDEAsync(entries[0].ftSignaturCreationUnitDEId);
            updatedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }
    }
}
