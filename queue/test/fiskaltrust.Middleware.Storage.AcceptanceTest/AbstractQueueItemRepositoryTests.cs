using AutoFixture;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
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

            var actualEntries = await ((IMiddlewareRepository<ftQueueItem>)sut).GetByTimeStampRangeAsync(firstSearchedEntryTimeStamp, lastSearchedEntryTimeStamp).ToListAsync();

            actualEntries.Should().BeEquivalentTo(allEntries.Skip(1).Take(5));
            actualEntries.Should().BeInAscendingOrder(x => x.TimeStamp);
        }

        [Fact]
        public async Task GetByTimeStampAsync_ShouldReturnAllEntries_FromAGivenTimeStamp_ShouldReturnOnlyTheseEntries()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).OrderBy(x => x.TimeStamp).ToList();

            var sut = await CreateRepository(expectedEntries);

            var allEntries = (await sut.GetAsync()).OrderBy(x => x.TimeStamp).ToList();
            var firstSearchedEntryTimeStamp = allEntries[1].TimeStamp;

            var actualEntries = await ((IMiddlewareRepository<ftQueueItem>)sut).GetEntriesOnOrAfterTimeStampAsync(firstSearchedEntryTimeStamp).ToListAsync();

            actualEntries.Should().BeEquivalentTo(allEntries.Skip(1));
            actualEntries.First().TimeStamp.Should().Be(firstSearchedEntryTimeStamp);
            actualEntries.Should().BeInAscendingOrder(x => x.TimeStamp);
        }

        [Fact]
        public async Task GetByTimeStampAsync_ShouldReturnAllEntries_FromAGivenTimeStamp_WithTake_ShouldReturnOnlyTheSpecifiedAmountOfEntries()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).OrderBy(x => x.TimeStamp).ToList();

            var sut = await CreateRepository(expectedEntries);

            var allEntries = (await sut.GetAsync()).OrderBy(x => x.TimeStamp).ToList();
            var firstSearchedEntryTimeStamp = allEntries[1].TimeStamp;

            var actualEntries = await ((IMiddlewareRepository<ftQueueItem>)sut).GetEntriesOnOrAfterTimeStampAsync(firstSearchedEntryTimeStamp, 2).ToListAsync();

            actualEntries.Should().BeEquivalentTo(allEntries.Skip(1).Take(2));
            actualEntries.Should().BeInAscendingOrder(x => x.TimeStamp);
        }

        [Fact]
        public async Task GetByTimeStampAsync_ShouldReturnAllEntries_FromAGivenTimeStamp_AndAllowToInsertEntryInTheMeantime()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).OrderBy(x => x.TimeStamp).ToList();

            var sutIterate = await CreateRepository(expectedEntries);
            var sutInsert = await CreateRepository(Array.Empty<ftQueueItem>());

            await foreach (var entry in ((IMiddlewareRepository<ftQueueItem>)sutIterate).GetEntriesOnOrAfterTimeStampAsync(0, 2))
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
        public async Task InsertOrUpdateAsync_ShouldAddEntryWithHugeRequestAndResponse_ToTheDatabase()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftQueueItem>();

            var request = JsonConvert.DeserializeObject<ReceiptRequest>(entryToInsert.request);
            request.cbReceiptReference = string.Join(string.Empty, StorageTestFixtureProvider.GetFixture().CreateMany<char>(40_000));
            entryToInsert.request = JsonConvert.SerializeObject(request);

            var response = JsonConvert.DeserializeObject<ReceiptResponse>(entryToInsert.response);
            response.cbReceiptReference = string.Join(string.Empty, StorageTestFixtureProvider.GetFixture().CreateMany<char>(40_000));
            entryToInsert.response = JsonConvert.SerializeObject(response);

            var sut = await CreateRepository(entries);
            await sut.InsertOrUpdateAsync(entryToInsert);

            var insertedEntry = await sut.GetAsync(entryToInsert.ftQueueItemId);
            insertedEntry.Should().BeEquivalentTo(entryToInsert);
        }

        private class Test
        {
            public DateTime DateTime { get; set; }
        }

        [Theory]
        [InlineData("2024-06-19T15:30:21Z", System.Globalization.DateTimeStyles.AdjustToUniversal)]
        public async Task InsertOrUpdateAsync_ShouldAddEntryWithNonUtcDateTime_ToTheDatabase(string dateTime, System.Globalization.DateTimeStyles flags)
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<ftQueueItem>();
            entryToInsert.ftQueueMoment = DateTime.Parse(dateTime, null, flags);
            entryToInsert.cbReceiptMoment = DateTime.Parse(dateTime, null, flags);
            entryToInsert.ftDoneMoment = DateTime.Parse(dateTime, null, flags);
            entryToInsert.ftWorkMoment = DateTime.Parse(dateTime, null, flags);

            var sut = await CreateRepository(entries);
            await sut.InsertOrUpdateAsync(entryToInsert);

            var insertedEntry = await sut.GetAsync(entryToInsert.ftQueueItemId);
            insertedEntry.Should().BeEquivalentTo(entryToInsert);
        }

        [Fact]
        public virtual async Task InsertOrUpdateAsync_ShouldUpdateEntry_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(10).ToList();

            var sut = await CreateRepository(entries);
            var count = (await sut.GetAsync()).Count();
            var entryToUpdate = await sut.GetAsync(entries[0].ftQueueItemId);
            entryToUpdate.ftQueueRow = long.MaxValue;

            await sut.InsertOrUpdateAsync(entryToUpdate);

            var updatedEntry = await sut.GetAsync(entries[0].ftQueueItemId);

            updatedEntry.ftQueueRow.Should().Be(long.MaxValue);
            (await sut.GetAsync()).Count().Should().Be(count);
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
        public async Task GetGroupedReceiptReference_NoTimespan_QueryValidGroupAndItems()
        {
            var noReference = StorageTestFixtureProvider.GetFixture().CreateMany<ftQueueItem>(1).ToList();
            var receiptReference = "reference9";
            var queueItemFixture = StorageTestFixtureProvider.GetFixture();
            queueItemFixture.Customize<ftQueueItem>(c => c.With(r => r.cbReceiptReference, receiptReference));
            var expectedEntries = queueItemFixture.CreateMany<ftQueueItem>(10).OrderBy(x => x.ftQueueRow).ToList();
            expectedEntries.Add(noReference.First());

            var receiptFixture = StorageTestFixtureProvider.GetFixture();
            receiptFixture.Customize<ReceiptRequest>(c => c.With(r => r.ftReceiptCase, 4919338172267102209));

            var receiptPreviousFixture = StorageTestFixtureProvider.GetFixture();
            receiptPreviousFixture.Customize<ReceiptRequest>(c => c.With(r => r.ftReceiptCase, 4919338172267102209).With(r => r.cbPreviousReceiptReference, receiptReference));

            for (var i = 0; i < expectedEntries.Count; i++)
            {
                if (i < 4)
                {
                    expectedEntries[i].request = JsonConvert.SerializeObject(receiptPreviousFixture.Create<ReceiptRequest>());
                }
                else
                {
                    expectedEntries[i].request = JsonConvert.SerializeObject(receiptFixture.Create<ReceiptRequest>());
                }

            }
            var sut = await CreateRepository(expectedEntries);

            var groupedReferences = await sut.GetGroupedReceiptReferenceAsync(null, null).ToListAsync();
            groupedReferences.Count().Should().Be(2);
            groupedReferences.Contains(noReference[0].cbReceiptReference).Should().BeTrue();
            groupedReferences.Contains(receiptReference).Should().BeTrue();

        }

        [Fact]
        public async Task GetGroupedReceiptReference_FromTimespan_QueryValidGroupAndItems()
        {

            var receiptFixture = StorageTestFixtureProvider.GetFixture();
            receiptFixture.Customize<ReceiptRequest>(c => c.With(r => r.ftReceiptCase, 4919338172267102209));

            var queueItemFixture = StorageTestFixtureProvider.GetFixture();
            queueItemFixture.Customize<ftQueueItem>(c => c.With(r => r.TimeStamp, DateTime.UtcNow.Ticks).With(r => r.request, JsonConvert.SerializeObject(receiptFixture.Create<ReceiptRequest>())));
            var expectedEntries = queueItemFixture.CreateMany<ftQueueItem>(2).ToList();
            var sut = await CreateRepository(expectedEntries);

            await Task.Delay(1);
            var fromIncl = DateTime.UtcNow.Ticks;
            var queueItem = queueItemFixture.Create<ftQueueItem>();
            await sut.InsertOrUpdateAsync(queueItem);
            var groupedReferences = await sut.GetGroupedReceiptReferenceAsync(fromIncl, null).ToListAsync();
            groupedReferences.Count().Should().Be(1);
        }

        [Fact]
        public async Task GetGroupedReceiptReference_ToTimespan_QueryValidGroupAndItems()
        {
            var receiptFixture = StorageTestFixtureProvider.GetFixture();
            receiptFixture.Customize<ReceiptRequest>(c => c.With(r => r.ftReceiptCase, 4919338172267102209));

            var queueItemFixture = StorageTestFixtureProvider.GetFixture();
            queueItemFixture.Customize<ftQueueItem>(c => c.With(r => r.TimeStamp, DateTime.UtcNow.Ticks).With(r => r.request, JsonConvert.SerializeObject(receiptFixture.Create<ReceiptRequest>())));
            var expectedEntries = queueItemFixture.CreateMany<ftQueueItem>(2).ToList();
            var sut = await CreateRepository(expectedEntries);
            var toIncl = DateTime.UtcNow.Ticks;
            await Task.Delay(1);
            var queueItem = queueItemFixture.Create<ftQueueItem>();
            await sut.InsertOrUpdateAsync(queueItem);
            var groupedReferences = await sut.GetGroupedReceiptReferenceAsync(null, toIncl).ToListAsync();
            groupedReferences.Count().Should().Be(2);
        }

        [Fact]
        public async Task GetGroupedReceiptReference_ToAndFromTimespan_QueryValidGroupAndItems()
        {
            var receiptFixture = StorageTestFixtureProvider.GetFixture();
            receiptFixture.Customize<ReceiptRequest>(c => c.With(r => r.ftReceiptCase, 4919338172267102209));

            var queueItemFixture = StorageTestFixtureProvider.GetFixture();
            queueItemFixture.Customize<ftQueueItem>(c => c.With(r => r.TimeStamp, DateTime.UtcNow.Ticks).With(r => r.request, JsonConvert.SerializeObject(receiptFixture.Create<ReceiptRequest>())));

            var expectedEntries = queueItemFixture.CreateMany<ftQueueItem>(2).ToList();
            var sut = await CreateRepository(expectedEntries);
            await Task.Delay(1);
            var fromIncl = DateTime.UtcNow.Ticks;
            await Task.Delay(1);
            var queueItem = queueItemFixture.Create<ftQueueItem>();
            queueItem.cbReceiptReference = "reference9fromTo";
            await sut.InsertOrUpdateAsync(queueItem);
            await Task.Delay(1);
            var toIncl = DateTime.UtcNow.Ticks;
            await Task.Delay(1);
            queueItem = queueItemFixture.Create<ftQueueItem>();
            await sut.InsertOrUpdateAsync(queueItem);

            var groupedReferences = await sut.GetGroupedReceiptReferenceAsync(fromIncl, toIncl).ToListAsync();
            groupedReferences.Count().Should().Be(1);
        }


        [Fact]
        public virtual async Task GetQueueItemsForReceiptReferenceAsync_PosAndNonePosReceipts_ValidQueueItems()
        {
            var receiptReference = "receiptReference9";

            var receiptFixture = StorageTestFixtureProvider.GetFixture();
            receiptFixture.Customize<ReceiptRequest>(c => c.With(r => r.ftReceiptCase, 4919338172267102209));

            var queueItemFixture = StorageTestFixtureProvider.GetFixture();
            queueItemFixture.Customize<ftQueueItem>(c => c.With(r => r.TimeStamp, DateTime.UtcNow.Ticks).
            With(r => r.request, JsonConvert.SerializeObject(receiptFixture.Create<ReceiptRequest>())).
            With(r => r.cbReceiptReference, receiptReference));

            var receiptFixtureClosing = StorageTestFixtureProvider.GetFixture();
            receiptFixtureClosing.Customize<ReceiptRequest>(c => c.With(r => r.ftReceiptCase, 4919338172401319943));

            var queueItemFixtureClosing = StorageTestFixtureProvider.GetFixture();
            queueItemFixtureClosing.Customize<ftQueueItem>(c => c.With(r => r.TimeStamp, DateTime.UtcNow.Ticks).
            With(r => r.request, JsonConvert.SerializeObject(receiptFixtureClosing.Create<ReceiptRequest>())).
            With(r => r.cbReceiptReference, receiptReference));

            var expectedEntriesPos = queueItemFixture.CreateMany<ftQueueItem>(2).ToList();
            expectedEntriesPos.Add(queueItemFixtureClosing.Create<ftQueueItem>());
            var difReceiptRef = queueItemFixture.Create<ftQueueItem>();
            difReceiptRef.cbReceiptReference = "NotIncluded";
            expectedEntriesPos.Add(difReceiptRef);

            var sut = await CreateRepository(expectedEntriesPos);

            var receiptReferences = await sut.GetQueueItemsForReceiptReferenceAsync(receiptReference).ToListAsync();
            receiptReferences.Count().Should().Be(2);
            foreach (var receipt in receiptReferences)
            {
                receipt.cbReceiptReference.Should().Be(receiptReference);
                JsonConvert.DeserializeObject<ReceiptRequest>(receipt.request).ftReceiptCase.Should().Be(4919338172267102209);
            }
        }

        [Fact]
        public async Task GetClosestPreviousReceiptReferencesAsync_PosAndNonePosReceipts_ValidQueueItems()
        {
            var receiptReference = "receiptReference9";

            var receiptFixtureClosing = StorageTestFixtureProvider.GetFixture();
            receiptFixtureClosing.Customize<ReceiptRequest>(c => c.With(r => r.ftReceiptCase, 4919338172401319943));

            var queueItemFixtureClosing = StorageTestFixtureProvider.GetFixture();
            queueItemFixtureClosing.Customize<ftQueueItem>(c => c.With(r => r.TimeStamp, DateTime.UtcNow.Ticks).
            With(r => r.request, JsonConvert.SerializeObject(receiptFixtureClosing.Create<ReceiptRequest>())).
            With(r => r.cbReceiptReference, receiptReference));

            var expectedEntries = queueItemFixtureClosing.CreateMany<ftQueueItem>(2).ToList();
            var sut = await CreateRepository(expectedEntries);
            await Task.Delay(1);

            var receiptFixture = StorageTestFixtureProvider.GetFixture();
            receiptFixture.Customize<ReceiptRequest>(c => c.With(r => r.ftReceiptCase, 4919338172267102209));

            var queueItemFixture = StorageTestFixtureProvider.GetFixture();
            queueItemFixture.Customize<ftQueueItem>(c => c.With(r => r.TimeStamp, DateTime.UtcNow.Ticks).
            With(r => r.request, JsonConvert.SerializeObject(receiptFixture.Create<ReceiptRequest>())).
            With(r => r.cbReceiptReference, receiptReference));

            var queueRow = 0;
            var firstPos = queueItemFixture.Create<ftQueueItem>();
            firstPos.ftQueueRow = queueRow;
            await sut.InsertOrUpdateAsync(firstPos);

            var prevReceiptFixture = StorageTestFixtureProvider.GetFixture();
            prevReceiptFixture.Customize<ReceiptRequest>(c => c.With(r => r.ftReceiptCase, 4919338172267102209).
            With(r => r.cbPreviousReceiptReference, receiptReference));

            await Task.Delay(1);
            var secondPos = queueItemFixture.Create<ftQueueItem>();
            secondPos.ftQueueRow = queueRow++;
            secondPos.cbReceiptReference = firstPos.cbReceiptReference;
            secondPos.request = JsonConvert.SerializeObject(receiptFixture.Create<ReceiptRequest>());
            await sut.InsertOrUpdateAsync(secondPos);

            await Task.Delay(1);
            var prevRefPos = queueItemFixture.Create<ftQueueItem>();
            prevRefPos.ftQueueRow = queueRow++;
            prevRefPos.cbReceiptReference = "ReceiptReference" + Guid.NewGuid().ToString();
            prevRefPos.request = JsonConvert.SerializeObject(prevReceiptFixture.Create<ReceiptRequest>());
            await sut.InsertOrUpdateAsync(prevRefPos);

            var closestQueriedPos = await sut.GetClosestPreviousReceiptReferencesAsync(prevRefPos);

            closestQueriedPos.ftQueueItemId.Should().Be(secondPos.ftQueueItemId);

        }
    }
}
