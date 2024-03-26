using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fiskaltrust.Middleware.Localization.QueueDE.Repositories;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Moq;
using Xunit;
using System.Threading.Tasks;
using FluentAssertions;

namespace fiskaltrust.Middleware.Localization.QueueDE.UnitTest.Repository
{
    public class DailyClosingRepositoryTest
    {
        [Fact]
        public async Task DailyClosingRepositoryTest_IncludeActionjWithNoQueueItemId_ValidDailyClosingsAsync()
        {
            var(ajs, queueItems) = GetDailyClosingData();
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

        }

        private (IEnumerable<ftActionJournal>, IEnumerable<ftQueueItem>) GetDailyClosingData()
        {
            var queueId = Guid.NewGuid();
            var queueItemId2 = Guid.NewGuid();

            var queueMoment1 = DateTime.Now;
            Task.Delay(1).Wait();
            var actionJournal1_mom = DateTime.Now;
            Task.Delay(1).Wait();
            var queueItemt1_tstp = DateTime.Now.Ticks;
            Task.Delay(1).Wait();
            var queueMoment2 = DateTime.Now;
            Task.Delay(1).Wait();
            var actionJournal2_mom = DateTime.Now;
            Task.Delay(1).Wait();
            var queueItemt2_tstp = DateTime.Now.Ticks;
            Task.Delay(1).Wait();
            var queueMoment2_1 = DateTime.Now;
            Task.Delay(1).Wait();
            var queueItemt2_1_tstp = DateTime.Now.Ticks;
            Task.Delay(1).Wait();
            var queueMoment3 = DateTime.Now;
            var actionJournal3_mom = DateTime.Now;
            Task.Delay(1).Wait();
            var queueItemt3_tstp = DateTime.Now.Ticks;
            Task.Delay(1).Wait();
            var queueMoment4 = DateTime.Now;
            Task.Delay(1).Wait();
            var actionJournal4_mom = DateTime.Now;
            var queueItemt4_tstp = DateTime.Now.Ticks;

            var actionJournals = new List<ftActionJournal>() {
                new ftActionJournal
                {
                    ftQueueId = queueId,
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueItemId = queueId,
                    Moment = actionJournal1_mom,
                    Type = "4445000100000007",
                    DataJson = "{\"closingNumber\": 1}"
                },
                new ftActionJournal
                {
                    ftQueueId = queueId,
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueItemId = queueId,
                    Moment = actionJournal2_mom,
                    Type = "4445000800000007",
                    DataJson = "{\"closingNumber\": 2}"
                },
                new ftActionJournal
                {
                    ftQueueId = queueId,
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueItemId = queueId,
                    Moment = actionJournal3_mom,
                    Type = "4445000800000007",
                    DataJson = "{\"closingNumber\": 3}"
                },
                new ftActionJournal
                {
                    ftQueueId = queueId,
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueItemId = queueId,
                    Moment = actionJournal4_mom,
                    Type = "4445000800000007",
                    DataJson = "{\"closingNumber\": 4}"
                },
            };

            var queueItems = new List<ftQueueItem>()
            {
                new ftQueueItem
                {
                    ftQueueItemId = Guid.NewGuid(),
                    ftQueueMoment = queueMoment1,
                    ftQueueRow = 1,
                    ftQueueId = queueId,
                    TimeStamp = queueItemt1_tstp
                },
                new ftQueueItem
                {
                    ftQueueItemId = queueItemId2,
                    ftQueueMoment = queueMoment2,
                    ftQueueRow = 2,
                    ftQueueId = queueId,
                    TimeStamp = queueItemt2_tstp
                },
                new ftQueueItem
                {
                    ftQueueItemId = Guid.NewGuid(),
                    ftQueueMoment = queueMoment2_1,
                    ftQueueRow = 3,
                    ftQueueId = queueId,
                    TimeStamp = queueItemt2_1_tstp
                },
                new ftQueueItem
                {
                    ftQueueItemId = Guid.NewGuid(),
                    ftQueueMoment = queueMoment3,
                    ftQueueRow = 4,
                    ftQueueId = queueId,
                    TimeStamp = queueItemt3_tstp
                },
                new ftQueueItem
                {
                    ftQueueItemId = Guid.NewGuid(),
                    ftQueueMoment = queueMoment4,
                    ftQueueRow = 5,
                    ftQueueId = queueId,
                    TimeStamp = queueItemt4_tstp
                },
            };

            return (actionJournals, queueItems);
        }
    }
}
