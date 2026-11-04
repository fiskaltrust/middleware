using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.Common.Extensions;
using fiskaltrust.Middleware.Journal.DE.Common.Repositories;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Helpers
{
    public class DailyClosingSplitterTests
    {
        [Fact]
        public async Task SplitByDailyClosingReceiptAsync_Should_ReturnEmptyResult_WhenNoItemsAreSent()
        {
            var receiptJournalRepo = new Mock<IReadOnlyReceiptJournalRepository>();
            var queueItemRepo = new Mock<IReadOnlyQueueItemRepository>();
            var receiptReferenceRepo = new Mock<IReadOnlyReceiptReferenceRepository>();

            var dailyclosingRepo = new Mock<IDailyClosingRepository>();
            dailyclosingRepo.Setup(x => x.GetAsync()).ReturnsAsync(new List<DailyClosingReceipt>());

            var sut = new DailyClosingSplitter(receiptJournalRepo.Object, queueItemRepo.Object, receiptReferenceRepo.Object, dailyclosingRepo.Object,  new Mock<ILogger>().Object, null);
            var result = await sut.SplitByDailyClosingReceiptAsync(0, 1, 5, false).ToListAsync();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SplitByDailyClosingReceiptAsync_Should_ReturnEmptyResult_WhenNoDailyClosingsAreSent()
        {
            var receiptJournalRepo = new Mock<IReadOnlyReceiptJournalRepository>();
            receiptJournalRepo.Setup(x => x.GetAsync()).ReturnsAsync(TestData.ReceiptJournals);

            var queueItemRepo = new Mock<IReadOnlyQueueItemRepository>();
            queueItemRepo.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => TestData.QueueItemsWithoutDailyClosings.Single(x => x.ftQueueItemId == id));

            var receiptReferenceRepo = new Mock<IReadOnlyReceiptReferenceRepository>();

            var dailyclosingRepo = new Mock<IDailyClosingRepository>();
            dailyclosingRepo.Setup(x => x.GetAsync()).ReturnsAsync(new List<DailyClosingReceipt>());

            var sut = new DailyClosingSplitter(receiptJournalRepo.Object, queueItemRepo.Object, receiptReferenceRepo.Object, dailyclosingRepo.Object, new Mock<ILogger>().Object, null);
            var result = await sut.SplitByDailyClosingReceiptAsync(0, 1, 5, false).ToListAsync();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SplitByDailyClosingReceiptAsync_Should_FailLoudly_WhenAReceiptsQueueItemIsMissing()
        {
            // A receipt whose queue item was not downloaded must fail the export - silently dropping it would
            // ship an output that is missing a receipt. Upstream guards should make this unreachable; reaching
            // it means the downloaded set itself is inconsistent.
            var receiptJournalRepo = new Mock<IReadOnlyReceiptJournalRepository>();
            receiptJournalRepo.Setup(x => x.GetAsync()).ReturnsAsync(TestData.ReceiptJournals);

            var missingQueueItemId = TestData.ReceiptJournals[1].ftQueueItemId;
            var queueItemRepo = new Mock<IReadOnlyQueueItemRepository>();
            queueItemRepo.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => TestData.QueueItemsWithoutDailyClosings.SingleOrDefault(x => x.ftQueueItemId == id && id != missingQueueItemId));

            var dailyclosingRepo = new Mock<IDailyClosingRepository>();
            dailyclosingRepo.Setup(x => x.GetAsync()).ReturnsAsync(new List<DailyClosingReceipt>());

            var sut = new DailyClosingSplitter(receiptJournalRepo.Object, queueItemRepo.Object, new Mock<IReadOnlyReceiptReferenceRepository>().Object, dailyclosingRepo.Object, new Mock<ILogger>().Object, null);
            Func<Task> split = async () => await sut.SplitByDailyClosingReceiptAsync(0, 1, 5, false).ToListAsync();

            (await split.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*missing receipts*");
        }

        [Fact]
        public async Task SplitByDailyClosingReceiptAsync_Should_GroupItems_ByDailyClosing()
        {
            var receiptJournalRepo = new Mock<IReadOnlyReceiptJournalRepository>();
            receiptJournalRepo.Setup(x => x.GetAsync()).ReturnsAsync(TestData.ReceiptJournals);

            var queueItemRepo = new Mock<IReadOnlyQueueItemRepository>();
            queueItemRepo.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => TestData.QueueItemsWithDailyClosings.Single(x => x.ftQueueItemId == id));

            var receiptReferenceRepo = new Mock<IReadOnlyReceiptReferenceRepository>();
            var receiptReferenceData = new HashSet<ReceiptCaseData>();
            receiptReferenceRepo.Setup(x => x.GetReceiptReferenceAsync(It.IsAny<long?>(), It.IsAny<long?>(), It.IsAny<List<DailyClosingReceipt>>())).ReturnsAsync(() => TestData.ReceiptReferenceData);

            var dailyclosings = new List<DailyClosingReceipt>
            {
                new DailyClosingReceipt { QueueRow = 4, ZNumber = 1, ZTime  = new DateTime(2020, 1, 1) },
                new DailyClosingReceipt { QueueRow = 7, ZNumber = 1, ZTime  = new DateTime(2020, 1, 2)}
            };
            var dailyclosingRepo = new Mock<IDailyClosingRepository>();
            dailyclosingRepo.Setup(x => x.GetAsync()).ReturnsAsync(dailyclosings);

            var sut = new DailyClosingSplitter(receiptJournalRepo.Object, queueItemRepo.Object, receiptReferenceRepo.Object, dailyclosingRepo.Object, new Mock<ILogger>().Object,  null);
            var result = await sut.SplitByDailyClosingReceiptAsync(0, 0, 5, false).ToListAsync();

            result.Should().HaveCount(2);
            result[0].Should().BeEquivalentTo((
            new List<ReceiptEntry> {
                new ReceiptEntry
                {
                    ReceiptJournal = TestData.ReceiptJournals[0],
                    QueueItem = SetReqRespNull(TestData.QueueItemsWithDailyClosings[0]),
                    ReceiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(TestData.QueueItemsWithDailyClosings[0].request),
                    ReceiptResponse = JsonConvert.DeserializeObject<ReceiptResponse>(TestData.QueueItemsWithDailyClosings[0].response)
                },
                new ReceiptEntry
                {
                    ReceiptJournal = TestData.ReceiptJournals[1],
                    QueueItem = SetReqRespNull(TestData.QueueItemsWithDailyClosings[1]),
                    ReceiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(TestData.QueueItemsWithDailyClosings[1].request),
                    ReceiptResponse = JsonConvert.DeserializeObject<ReceiptResponse>(TestData.QueueItemsWithDailyClosings[1].response)
                }
            }, new HashSet<ReceiptReferenceData>(), 0));
            ;

            result[1].Should().BeEquivalentTo((
            new List<ReceiptEntry> {
                new ReceiptEntry
                {
                    ReceiptJournal = TestData.ReceiptJournals[2],
                    QueueItem = SetReqRespNull(TestData.QueueItemsWithDailyClosings[2]),
                    ReceiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(TestData.QueueItemsWithDailyClosings[2].request),
                    ReceiptResponse = JsonConvert.DeserializeObject<ReceiptResponse>(TestData.QueueItemsWithDailyClosings[2].response)
                },
                new ReceiptEntry
                {
                    ReceiptJournal = TestData.ReceiptJournals[3],
                    QueueItem = SetReqRespNull(TestData.QueueItemsWithDailyClosings[3]),
                    ReceiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(TestData.QueueItemsWithDailyClosings[3].request),
                    ReceiptResponse = JsonConvert.DeserializeObject<ReceiptResponse>(TestData.QueueItemsWithDailyClosings[3].response)
                }
            }, new HashSet<ReceiptReferenceData>(), 1));
        }
        private static ftQueueItem SetReqRespNull(ftQueueItem item)
        {
            item.request = null;
            item.response = null;
            return item;
        }

        [Fact]
        public void LookUpReferences_tryDifferenValuesValidResult()
        {
            var receiptJournalRepo = new Mock<IReadOnlyReceiptJournalRepository>();

            var queueItemRepo = new Mock<IReadOnlyQueueItemRepository>();
            queueItemRepo.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => TestData.QueueItemsWithoutDailyClosings.Single(x => x.ftQueueItemId == id));

            var receiptReferenceRepo = new Mock<IReadOnlyReceiptReferenceRepository>();

            var dailyclosingRepo = new Mock<IDailyClosingRepository>();
            dailyclosingRepo.Setup(x => x.GetAsync()).ReturnsAsync(new List<DailyClosingReceipt>());

            var sut = new DailyClosingSplitter(receiptJournalRepo.Object, queueItemRepo.Object, receiptReferenceRepo.Object, dailyclosingRepo.Object, new Mock<ILogger>().Object, null);
            new ReceiptRequest() { ftReceiptCase = 0x4445000000000002 }.IncludeInReferences().Should().BeFalse();
            new ReceiptRequest() { ftReceiptCase = 0x4445000000000001 }.IncludeInReferences().Should().BeTrue();
        }
    }
}
