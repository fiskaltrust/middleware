using AutoFixture;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Base.Extensions;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;


namespace fiskaltrust.Middleware.Storage.AcceptanceTest
{
    public abstract class AbstractQueueItemRepositoryTests : IDisposable
    {
        public abstract Task<IMiddlewareQueueItemRepository> CreateRepository(IEnumerable<ftQueueItem> entries);

        public virtual Task<IMiddlewareQueueItemRepository> CreateRepository(string path) => throw new NotImplementedException();

        public abstract Task<IReadOnlyQueueItemRepository> CreateReadOnlyRepository(IEnumerable<ftQueueItem> entries);

        public virtual void DisposeDatabase() { return; }

        public void Dispose() => DisposeDatabase();

        [Fact]
        public async Task GetAsync_ShouldReturnAllEntriesThatExistInRepository()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10);

            var sut = await CreateReadOnlyRepository(expectedEntries);

            var actualEntries = await sut.GetAsync();

            actualEntries.Should().BeEquivalentTo(expectedEntries);
        }

        [Fact]
        public async Task GetByTimeStampAsync_ShouldReturnAllEntries_WithinAGivenTimeStamp_ShouldReturnOnlyTheseEntries()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).OrderByDescending(x => x.TimeStamp).ToList();

            var sut = await CreateRepository(expectedEntries);

            var allEntries = (await sut.GetAsync()).OrderByDescending(x => x.TimeStamp).ToList();
            var firstSearchedEntryTimeStamp = allEntries[5].TimeStamp;
            var lastSearchedEntryTimeStamp = allEntries[1].TimeStamp;

            var actualEntries = await ((IMiddlewareRepository<ftQueueItem>) sut).GetByTimeStampRangeAsync(firstSearchedEntryTimeStamp, lastSearchedEntryTimeStamp).ToListAsync();

            actualEntries.Should().BeEquivalentTo(allEntries.Skip(1).Take(5));
        }

        [Fact]
        public async Task GetByTimeStampAsync_ShouldReturnAllEntries_FromAGivenTimeStamp_ShouldReturnOnlyTheseEntries()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).OrderBy(x => x.TimeStamp).ToList();

            var sut = await CreateRepository(expectedEntries);

            var allEntries = (await sut.GetAsync()).OrderBy(x => x.TimeStamp).ToList();
            var firstSearchedEntryTimeStamp = allEntries[1].TimeStamp;

            var actualEntries = await ((IMiddlewareRepository<ftQueueItem>) sut).GetEntriesOnOrAfterTimeStampAsync(firstSearchedEntryTimeStamp).ToListAsync();

            actualEntries.Should().BeEquivalentTo(allEntries.Skip(1));
        }

        [Fact]
        public async Task GetByTimeStampAsync_ShouldReturnAllEntries_FromAGivenTimeStamp_WithTake_ShouldReturnOnlyTheSpecifiedAmountOfEntries()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).OrderBy(x => x.TimeStamp).ToList();

            var sut = await CreateRepository(expectedEntries);

            var allEntries = (await sut.GetAsync()).OrderBy(x => x.TimeStamp).ToList();
            var firstSearchedEntryTimeStamp = allEntries[1].TimeStamp;

            var actualEntries = await ((IMiddlewareRepository<ftQueueItem>) sut).GetEntriesOnOrAfterTimeStampAsync(firstSearchedEntryTimeStamp, 2).ToListAsync();

            actualEntries.Should().BeEquivalentTo(allEntries.Skip(1).Take(2));
        }

        [Fact]
        public async Task GetByTimeStampAsync_ShouldReturnAllEntries_FromAGivenTimeStamp_AndAllowToInsertEntryInTheMeantime()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).OrderBy(x => x.TimeStamp).ToList();

            var sutIterate = await CreateRepository(expectedEntries);
            var sutInsert = await CreateRepository(Array.Empty<ftQueueItem>());

            await foreach (var entry in ((IMiddlewareRepository<ftQueueItem>) sutIterate).GetEntriesOnOrAfterTimeStampAsync(0, 2))
            {
                await sutInsert.InsertOrUpdateAsync(StorageTestFixtureProvider.GetFixture().Create<ftQueueItem>());
            }
        }

        [Fact]
        public async Task GetByReceiptReferenceAsync_ShouldReturnAllEntriesWithMatchingReference()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).OrderBy(x => x.TimeStamp).ToList();
            var expectedReceiptReference = Guid.NewGuid().ToString();

            expectedEntries[0].cbReceiptReference = expectedReceiptReference;
            expectedEntries[1].cbReceiptReference = expectedReceiptReference;
            expectedEntries[2].cbReceiptReference = expectedReceiptReference;

            var sut = await CreateRepository(expectedEntries);
            var allEntries = (await sut.GetAsync()).OrderBy(x => x.TimeStamp).ToList();

            var entries = await sut.GetByReceiptReferenceAsync(expectedReceiptReference).ToListAsync();

            entries.Should().BeEquivalentTo(allEntries.Take(3));
        }

        [Fact]
        public async Task GetByReceiptReferenceAsync_ShouldReturnAllEntriesWithMatchingReference_ButOnlyWithMatchingTerminalId()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).OrderBy(x => x.TimeStamp).ToList();
            var expectedReceiptReference = Guid.NewGuid().ToString();
            var expectedTerminalId = Guid.NewGuid().ToString();

            expectedEntries[0].cbReceiptReference = expectedReceiptReference;
            expectedEntries[1].cbReceiptReference = expectedReceiptReference;
            expectedEntries[1].cbTerminalID = expectedTerminalId;
            expectedEntries[2].cbReceiptReference = expectedReceiptReference;

            var sut = await CreateRepository(expectedEntries);
            var allEntries = (await sut.GetAsync()).OrderBy(x => x.TimeStamp).ToList();

            var entries = await sut.GetByReceiptReferenceAsync(expectedReceiptReference, expectedTerminalId).ToListAsync();

            entries.Should().BeEquivalentTo(new List<ftQueueItem> { expectedEntries[1] });
        }

        [Fact]
        public async Task GetByReceiptReferenceAsync_ShouldReturnAllEntriesWithMatchingReference_AndConnectionShouldBeAbleToHandleInsert()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).OrderBy(x => x.TimeStamp).ToList();
            var expectedReceiptReference = Guid.NewGuid().ToString();

            expectedEntries[0].cbReceiptReference = expectedReceiptReference;
            expectedEntries[1].cbReceiptReference = expectedReceiptReference;
            expectedEntries[2].cbReceiptReference = expectedReceiptReference;

            var sut = await CreateRepository(expectedEntries);
            var allEntries = (await sut.GetAsync()).OrderBy(x => x.TimeStamp).ToList();

            var entries = await sut.GetByReceiptReferenceAsync(expectedReceiptReference).ToListAsync();

            entries.Should().BeEquivalentTo(allEntries.Take(3));

            await sut.InsertOrUpdateAsync(StorageTestFixtureProvider.GetFixture().Create<ftQueueItem>());
        }

        [Fact]
        public async Task GetAsync_WithId_ShouldReturn_TheElementWithTheGivenId_IfTheIdExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).ToList();
            var expectedEntry = entries[4];

            var sut = await CreateReadOnlyRepository(entries);
            var actualEntry = await sut.GetAsync(expectedEntry.ftQueueItemId);

            actualEntry.Should().BeEquivalentTo(expectedEntry);
        }

        [Fact]
        public async Task GetAsync_WithId_ShouldReturn_Null_IfTheGivenIdDoesntExist()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).ToList();

            var sut = await CreateReadOnlyRepository(entries);
            var actualEntry = await sut.GetAsync(Guid.Empty);

            actualEntry.Should().BeNull();
        }

        [Fact]
        public async Task InsertOrUpdateAsync_ShouldAddEntry_ToTheDatabase()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftQueueItem>();

            var sut = await CreateRepository(entries);
            await sut.InsertOrUpdateAsync(entryToInsert);

            var insertedEntry = await sut.GetAsync(entryToInsert.ftQueueItemId);
            insertedEntry.Should().BeEquivalentTo(entryToInsert);
        }

        [Fact]
        public async Task InsertOrUpdateAsync_ShouldUpdateEntry_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).ToList();

            var sut = await CreateRepository(entries);

            var entryToUpdate = await sut.GetAsync(entries[0].ftQueueItemId);
            entryToUpdate.ftQueueRow = long.MaxValue;

            await sut.InsertOrUpdateAsync(entryToUpdate);

            var updatedEntry = await sut.GetAsync(entries[0].ftQueueItemId);

            updatedEntry.ftQueueRow.Should().Be(long.MaxValue);
        }

        [Fact]
        public async Task InsertOrUpdateAsync_ShouldUpdateTimeStampOfTheInsertedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftQueueItem>();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entryToInsert.TimeStamp = initialTimeStamp;

            var sut = await CreateRepository(entries);
            await sut.InsertOrUpdateAsync(entryToInsert);

            var insertedEntry = await sut.GetAsync(entryToInsert.ftQueueItemId);
            insertedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }

        [Fact]
        public async Task InsertOrUpdateAsync_ShouldUpdateTimeStampOfTheUpdatedEntry()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).ToList();
            var initialTimeStamp = DateTime.UtcNow.AddHours(-1).Ticks;
            entries[0].TimeStamp = initialTimeStamp;
            var sut = await CreateRepository(entries);

            var entryToUpdate = await sut.GetAsync(entries[0].ftQueueItemId);
            entryToUpdate.ftQueueRow = long.MaxValue;

            await sut.InsertOrUpdateAsync(entries[0]);

            var updatedEntry = await sut.GetAsync(entries[0].ftQueueItemId);
            updatedEntry.TimeStamp.Should().BeGreaterThan(initialTimeStamp);
        }

        [Fact]
        public async Task GetPreviousReceiptReferencesAsync_TerminalIDEmpty_ShouldReturnExpectedQueueItems()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).OrderBy(x => x.ftQueueRow).ToList();
            var expectedReceiptReference = Guid.NewGuid().ToString();

            var receiptRequest = new ReceiptRequest()
            {
                cbPreviousReceiptReference = expectedReceiptReference,
            };

            var receiptRequestJson = JsonConvert.SerializeObject(receiptRequest);

            var receiptRequestFixture = StorageTestFixtureProvider.GetFixture();
            receiptRequestFixture.Customize<ReceiptRequest>(c => c.With(r => r.ftReceiptCase, 4919338172267102209));

            expectedEntries[9].request = receiptRequestJson;
            expectedEntries[9].cbTerminalID = string.Empty;
            expectedEntries[0].cbReceiptReference = expectedReceiptReference;
            expectedEntries[0].request = JsonConvert.SerializeObject(receiptRequestFixture.Create<ReceiptRequest>());
            expectedEntries[1].cbReceiptReference = expectedReceiptReference;
            expectedEntries[1].request = JsonConvert.SerializeObject(receiptRequestFixture.Create<ReceiptRequest>());
            expectedEntries[2].cbReceiptReference = expectedReceiptReference;
            expectedEntries[2].request = JsonConvert.SerializeObject(receiptRequestFixture.Create<ReceiptRequest>());

            var sut = await CreateRepository(expectedEntries);
            var allEntries = (await sut.GetAsync()).OrderBy(x => x.ftQueueRow).ToList();

            var entries = await sut.GetPreviousReceiptReferencesAsync(expectedEntries[9]).OrderBy(x => x.ftQueueRow).ToListAsync();

            entries.Should().BeEquivalentTo(allEntries.Take(3));
        }

        [Fact]
        public async Task GetPreviousReceiptReferencesAsync_AllPosReceiptCases_ShouldReturnSomeQueueItems()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(25).OrderBy(x => x.ftQueueRow).ToList();
            var expectedReceiptReference = Guid.NewGuid().ToString();

            var receiptRequest = new ReceiptRequest()
            {
                cbPreviousReceiptReference = expectedReceiptReference,
            };

            var receiptRequestJson = JsonConvert.SerializeObject(receiptRequest);

            foreach (var (entry, index) in expectedEntries.Select((e, i) => (e, i)))
            {
                entry.cbReceiptReference = expectedReceiptReference;
                var request = StorageTestFixtureProvider.GetFixture().Create<ReceiptRequest>();
                request.ftReceiptCase = 4919338172267102208 + index;
                entry.request = JsonConvert.SerializeObject(request);
            }

            expectedEntries[24].request = receiptRequestJson;

            var sut = await CreateRepository(expectedEntries);
            var allEntries = (await sut.GetAsync()).OrderBy(x => x.ftQueueRow).ToList();

            var entries = await sut.GetPreviousReceiptReferencesAsync(expectedEntries[24]).OrderBy(x => x.ftQueueRow).ToListAsync();

            entries.Should().BeEquivalentTo(allEntries.Take(24).Where(e => JsonConvert.DeserializeObject<ReceiptRequest>(e.request).IncludeInReferences()));
        }

        [Fact]
        public async Task GetPreviousReceiptReferencesAsync_PreviousReceiptReferenceEmpty_ShouldReturnNoQueueItems()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).OrderBy(x => x.ftQueueRow).ToList();
            var expectedReceiptReference = Guid.NewGuid().ToString();

            var receiptRequest = new ReceiptRequest()
            {
                cbPreviousReceiptReference = string.Empty
            };

            var receiptRequestJson = JsonConvert.SerializeObject(receiptRequest);
            expectedEntries[9].request = receiptRequestJson;

            var sut = await CreateRepository(expectedEntries);
            var allEntries = (await sut.GetAsync()).OrderBy(x => x.TimeStamp).ToList();
            var entries = await sut.GetPreviousReceiptReferencesAsync(expectedEntries[9]).ToListAsync();
            entries.Count.Should().Be(0);
        }


        [Fact]
        public async Task GetPreviousReceiptReferencesAsync_ZeroReceiptReceiptRefOnlyPosReceipts_ShouldReturnNoQueueItems()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).OrderBy(x => x.ftQueueRow).ToList();
            var expectedReceiptReference = Guid.NewGuid().ToString();


            expectedEntries[0].request = JsonConvert.SerializeObject(new ReceiptRequest()
            {
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftReceiptCase = 4919338172267102210
            });
            expectedEntries[1].request = JsonConvert.SerializeObject(new ReceiptRequest()
            {
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftReceiptCase = 4919338172267102211
            });
            expectedEntries[2].request = JsonConvert.SerializeObject(new ReceiptRequest()
            {
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftReceiptCase = 4919338172267102213
            });
            expectedEntries[3].request = JsonConvert.SerializeObject(new ReceiptRequest()
            {
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftReceiptCase = 4919338172267102214
            });
            expectedEntries[4].request = JsonConvert.SerializeObject(new ReceiptRequest()
            {
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftReceiptCase = 4919338172267102210
            });
            expectedEntries[5].request = JsonConvert.SerializeObject(new ReceiptRequest()
            {
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftReceiptCase = 4919338172267102211
            });
            expectedEntries[6].request = JsonConvert.SerializeObject(new ReceiptRequest()
            {
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftReceiptCase = 4919338172267102213
            });
            expectedEntries[7].request = JsonConvert.SerializeObject(new ReceiptRequest()
            {
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftReceiptCase = 4919338172267102214
            });

            var sut = await CreateRepository(expectedEntries);
            var allEntries = (await sut.GetAsync()).OrderBy(x => x.ftQueueRow).ToList();

            var entries = await sut.GetPreviousReceiptReferencesAsync(expectedEntries[0]).OrderBy(x => x.ftQueueRow).ToListAsync();
            entries.Count.Should().Be(0);
            entries = await sut.GetPreviousReceiptReferencesAsync(expectedEntries[1]).OrderBy(x => x.ftQueueRow).ToListAsync();
            entries.Count.Should().Be(0);
            entries = await sut.GetPreviousReceiptReferencesAsync(expectedEntries[2]).OrderBy(x => x.ftQueueRow).ToListAsync();
            entries.Count.Should().Be(0);
            entries = await sut.GetPreviousReceiptReferencesAsync(expectedEntries[3]).OrderBy(x => x.ftQueueRow).ToListAsync();
            entries.Count.Should().Be(0);
            entries = await sut.GetPreviousReceiptReferencesAsync(expectedEntries[4]).OrderBy(x => x.ftQueueRow).ToListAsync();
            entries.Count.Should().Be(0);
            entries = await sut.GetPreviousReceiptReferencesAsync(expectedEntries[5]).OrderBy(x => x.ftQueueRow).ToListAsync();
            entries.Count.Should().Be(0);
            entries = await sut.GetPreviousReceiptReferencesAsync(expectedEntries[6]).OrderBy(x => x.ftQueueRow).ToListAsync();
            entries.Count.Should().Be(0);
            entries = await sut.GetPreviousReceiptReferencesAsync(expectedEntries[7]).OrderBy(x => x.ftQueueRow).ToListAsync();
            entries.Count.Should().Be(0);
        }

        [Fact]
        public async Task GetPreviousReceiptReferencesAsync_PrevReceiptRefZeroReceiptOnlyPosReceipts_ShouldReturnNoQueueItems()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).OrderBy(x => x.ftQueueRow).ToList();
            var expectedReceiptReference = Guid.NewGuid().ToString();

            var receiptRequest = new ReceiptRequest()
            {
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbPreviousReceiptReference = Guid.NewGuid().ToString(),
                ftReceiptCase = 4919338172267102209
            };

            var receiptRequestJson = JsonConvert.SerializeObject(receiptRequest);

            expectedEntries[9].request = receiptRequestJson;
            expectedEntries[9].cbTerminalID = string.Empty;
            expectedEntries[0].request = JsonConvert.SerializeObject(new ReceiptRequest()
            {
                cbReceiptReference = receiptRequest.cbReceiptReference,
                ftReceiptCase = 4919338172267102210
            });
            expectedEntries[1].request = JsonConvert.SerializeObject(new ReceiptRequest()
            {
                cbReceiptReference = receiptRequest.cbReceiptReference,
                ftReceiptCase = 4919338172267102211
            });
            expectedEntries[2].request = JsonConvert.SerializeObject(new ReceiptRequest()
            {
                cbReceiptReference = receiptRequest.cbReceiptReference,
                ftReceiptCase = 4919338172267102213
            });
            expectedEntries[3].request = JsonConvert.SerializeObject(new ReceiptRequest()
            {
                cbReceiptReference = receiptRequest.cbReceiptReference,
                ftReceiptCase = 4919338172267102214
            });
            expectedEntries[4].request = JsonConvert.SerializeObject(new ReceiptRequest()
            {
                cbReceiptReference = receiptRequest.cbPreviousReceiptReference,
                ftReceiptCase = 4919338172267102210
            });
            expectedEntries[5].request = JsonConvert.SerializeObject(new ReceiptRequest()
            {
                cbReceiptReference = receiptRequest.cbPreviousReceiptReference,
                ftReceiptCase = 4919338172267102211
            });
            expectedEntries[6].request = JsonConvert.SerializeObject(new ReceiptRequest()
            {
                cbReceiptReference = receiptRequest.cbPreviousReceiptReference,
                ftReceiptCase = 4919338172267102213
            });
            expectedEntries[7].request = JsonConvert.SerializeObject(new ReceiptRequest()
            {
                cbReceiptReference = receiptRequest.cbPreviousReceiptReference,
                ftReceiptCase = 4919338172267102214
            });

            var sut = await CreateRepository(expectedEntries);
            var allEntries = (await sut.GetAsync()).OrderBy(x => x.ftQueueRow).ToList();

            var entries = await sut.GetPreviousReceiptReferencesAsync(expectedEntries[9]).OrderBy(x => x.ftQueueRow).ToListAsync();
            entries.Count.Should().Be(0);
        }
    }
}
