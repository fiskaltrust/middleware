using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Localization.QueueDE.Repositories;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Moq;
using Xunit;
using System.Threading.Tasks;
using FluentAssertions;
using fiskaltrust.ifPOS.v0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE.UnitTest.Repository
{
    public class DailyClosingRepositoryTest
    {
        [Fact]
        public async Task DailyClosingRepositoryTest_IncludeActionjWithNoQueueItemId_ValidDailyClosingsAsync()
        {
            var (ajs, queueItems) = GetDailyClosingData();
            var actionjounalRepo = new Mock<IReadOnlyActionJournalRepository>();
            actionjounalRepo.Setup(x => x.GetAsync()).Returns(Task.FromResult(ajs));
            var queueItemRepository = new Mock<IMiddlewareQueueItemRepository>();
            queueItemRepository.Setup(x => x.GetByTimeStampRangeAsync(It.IsAny<long>(), It.IsAny<long>())).Returns(queueItems.ToAsyncEnumerable());

            var dailyClosingRepository = new DailyClosingRepository(actionjounalRepo.Object, queueItemRepository.Object);
            var closings = await dailyClosingRepository.GetAsync();

            closings.Should().NotBeNull();
            closings[0].ZNumber.Should().Be(1);
            closings[0].QueueRow.Should().Be(1);
            closings[1].ZNumber.Should().Be(2);
            closings[1].QueueRow.Should().Be(2);
            closings[2].ZNumber.Should().Be(3);
            closings[2].QueueRow.Should().Be(4);
            closings[3].ZNumber.Should().Be(4);
            closings[3].QueueRow.Should().Be(5);
            closings[4].QueueRow.Should().Be(6);
            closings[4].ZNumber.Should().Be(5);

        }
        
        private (IEnumerable<ftActionJournal>, IEnumerable<ftQueueItem>) GetDailyClosingData()
        {
            var queueId = Guid.NewGuid();
            var queueItemId2 = Guid.NewGuid();

            var queueItem1_timestamp = DateTime.Now.Ticks;
            var actionJournal1_timestamp = DateTime.Now.Ticks;
            Task.Delay(1).Wait();

            var queueItem2_timestamp = DateTime.Now.Ticks;
            var actionJournal2_mom = DateTime.Now.Ticks;
            Task.Delay(1).Wait();

            var queueItem3_timestamp = DateTime.Now.Ticks;
            Task.Delay(1).Wait();

            var queueItem4_timestamp = DateTime.Now.Ticks;
            var actionJournal4_timestamp = DateTime.Now.Ticks;
            Task.Delay(1).Wait();

            var queueItem5_timestamp = DateTime.Now.Ticks;
            var actionJournal5_timestamp = DateTime.Now.Ticks;
            Task.Delay(1).Wait();

            var queueItem6_timestamp = DateTime.Now.Ticks;
            var actionJournal6_timestamp = DateTime.Now.Ticks;

            var actionJournals = new List<ftActionJournal>() {
                new() {
                    ftQueueId = queueId,
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueItemId = queueId,
                    TimeStamp = actionJournal1_timestamp,
                    Type = "4445000100000007",
                    DataJson = "{\"closingNumber\": 1, \"ftReceiptNumerator\": 1}"
                },
                new() {
                    ftQueueId = queueId,
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueItemId = queueId,
                    TimeStamp = actionJournal2_mom,
                    Type = "4445000800000007",
                    DataJson = "{\"closingNumber\": 2, \"ftReceiptNumerator\": 2}"
                },
                new() {
                    ftQueueId = queueId,
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueItemId = queueId,
                    TimeStamp = actionJournal4_timestamp,
                    Type = "4445000800000007",
                    DataJson = "{\"closingNumber\": 3, \"ftReceiptNumerator\": 4}"
                },
                new() {
                    ftQueueId = queueId,
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueItemId = queueId,
                    TimeStamp = actionJournal5_timestamp,
                    Type = "4445000800000007",
                    DataJson = "{\"closingNumber\": 4, \"ftReceiptNumerator\": 5}"
                },
                new() {
                    ftQueueId = queueId,
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueItemId = queueId,
                    TimeStamp = actionJournal6_timestamp,
                    Type = "4445000800000007",
                    DataJson = "{\"ftReceiptNumerator\": 6}"
                },
            };

            var queueItems = new List<ftQueueItem>()
            {
                new() {
                    ftQueueItemId = Guid.NewGuid(),
                    ftQueueRow = 1,
                    ftQueueId = queueId,
                    TimeStamp = queueItem1_timestamp,
                    response = JsonConvert.SerializeObject(new ReceiptResponse{ ftReceiptIdentification = "ft1#" })
                },
                new() {
                    ftQueueItemId = queueItemId2,
                    ftQueueRow = 2,
                    ftQueueId = queueId,
                    TimeStamp = queueItem2_timestamp,
                    response = JsonConvert.SerializeObject(new ReceiptResponse{ ftReceiptIdentification = "ft2#" })
                },
                new() {
                    ftQueueItemId = Guid.NewGuid(),
                    ftQueueRow = 3,
                    ftQueueId = queueId,
                    TimeStamp = queueItem3_timestamp,
                    response = JsonConvert.SerializeObject(new ReceiptResponse{ ftReceiptIdentification = "ft3#" })
                },
                new() {
                    ftQueueItemId = Guid.NewGuid(),
                    ftQueueRow = 4,
                    ftQueueId = queueId,
                    TimeStamp = queueItem4_timestamp,
                    response = JsonConvert.SerializeObject(new ReceiptResponse{ ftReceiptIdentification = "ft4#" })
                },
                new() {
                    ftQueueItemId = Guid.NewGuid(),
                    ftQueueRow = 5,
                    ftQueueId = queueId,
                    TimeStamp = queueItem5_timestamp,
                    response = JsonConvert.SerializeObject(new ReceiptResponse{ ftReceiptIdentification = "ft5#" })
                },
                new() {
                    ftQueueItemId = Guid.NewGuid(),
                    ftQueueRow = 6,
                    ftQueueId = queueId,
                    TimeStamp = queueItem6_timestamp,
                    response = JsonConvert.SerializeObject(new ReceiptResponse{ ftReceiptIdentification = "ft6#", ftStateData="{\"DailyClosingNumber\":5}" })
                },
            };

            return (actionJournals, queueItems);
        }
    }
}
