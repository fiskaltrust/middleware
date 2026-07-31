using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueGR.Processors;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.Processors;

public class InvoiceCounterMigrationTests
{
    [Fact]
    public async Task UpgradedQueue_SeedsAtLastSubmittedAa()
    {
        // Queues activated before the invoice counter shipped carry the last submitted
        // aa in the "{series}-{aa}" segment the pre-counter MyDataSCU appended to
        // ftReceiptIdentification on every AADE success. The migration must seed the
        // counter so the next reservation is exactly last-submitted-aa + 1 — zero
        // receipts, closings and failed attempts advanced ftReceiptNumerator in the
        // meantime, and none of that may shift the sequence.
        var queue = TestHelpers.CreateQueue();
        queue.ftReceiptNumerator = 40; // inflated by NoOps — must be irrelevant
        queue.ftQueuedRow = 8;
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = null!,
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepo(queueGR, queue);
        var queueItemRepository = QueueItemRepo(
            HistoryItem(5, success: true, "ft11#CB-A-17"),   // last real AADE submission
            HistoryItem(6, success: true, "ft12#"),          // daily closing (NoOp, no segment)
            HistoryItem(7, success: true, "ft13#"),          // zero receipt (NoOp, no segment)
            HistoryItem(8, success: false, "ft14#"));        // failed submission

        await InvoiceCounterMigration.EnsureMigratedAsync(configRepoMock.Object, queueItemRepository, queue.ftQueueId, Mock.Of<ILogger>());

        queueGR.InvoiceSeries.Should().Be("CB-A");
        queueGR.InvoiceNumerator.Should().Be(17); // next reserved aa = 18 = last + 1
        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(queueGR), Times.Once);
    }

    [Fact]
    public async Task UpgradedQueue_SeedsAtNewestOwnSeriesAa_EvenWhenHigherValuesExistFurtherBack()
    {
        // The seed is deliberately the NEWEST own-series aa, not the history maximum:
        // the walk stops at the first hit, so queue start doesn't scan the complete
        // history (on Azure Table Storage every row read is an unpartitioned filter
        // query — the full walk caused a read storm on large queues). Accepted risk: a
        // historical aa-only mydataoverride that filed out-of-order values further back
        // (here: the automatic 17 is older than the override's 5) makes this seed too
        // low, and the next reservation fails loudly at AADE as a duplicate (233) until
        // the counter is corrected manually — see GetLastSubmittedAaAsync.
        var queue = TestHelpers.CreateQueue();
        queue.ftQueuedRow = 3;
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = null!,
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepo(queueGR, queue);
        var queueItemRepository = QueueItemRepo(
            HistoryItem(1, success: true, "ft1#CB-A-17"),   // last automatic submission
            HistoryItem(2, success: true, "ft2#CB-A-5"),    // historical override-low, newer
            HistoryItem(3, success: true, "ft3#"));         // closing (NoOp)

        await InvoiceCounterMigration.EnsureMigratedAsync(configRepoMock.Object, queueItemRepository, queue.ftQueueId, Mock.Of<ILogger>());

        queueGR.InvoiceNumerator.Should().Be(5);
    }

    [Fact]
    public async Task UpgradedQueue_ReadsHistoryInRowRangeChunks_AndStopsAtTheFirstChunkWithASubmission()
    {
        // The read-storm guard itself: the history is read in row-range chunks (one
        // storage query per chunk, newest range first) and the walk ends with the chunk
        // containing the newest own-series submission — older ranges are never queried.
        var queue = TestHelpers.CreateQueue();
        queue.ftQueuedRow = 1200; // three chunks of 500: [701..1200], [201..700], [1..200]
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = null!,
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepo(queueGR, queue);
        var queriedRanges = new List<(long From, long To)>();
        var items = new[]
        {
            HistoryItem(100, success: true, "ft1#CB-A-90"),   // older submission — must stay unread
            HistoryItem(900, success: true, "ft2#CB-A-870"),  // newest own-series submission
            HistoryItem(1100, success: true, "ft3#"),         // closing (NoOp)
        };
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(x => x.GetByQueueRowRangeAsync(It.IsAny<long>(), It.IsAny<long>()))
            .Returns((long from, long to) =>
            {
                queriedRanges.Add((from, to));
                return items.Where(x => x.ftQueueRow >= from && x.ftQueueRow <= to).ToAsyncEnumerable();
            });

        await InvoiceCounterMigration.EnsureMigratedAsync(configRepoMock.Object, repo.Object, queue.ftQueueId, Mock.Of<ILogger>());

        queueGR.InvoiceNumerator.Should().Be(870);
        queriedRanges.Should().Equal((701L, 1200L)); // a single roundtrip — older chunks stay unread
    }

    [Fact]
    public async Task Gate_RetriesFaultedMigration_OnNextReceipt()
    {
        // A transient storage error at startup must only fail the receipts processed
        // during the outage — the next receipt retries the migration instead of
        // rethrowing a permanently cached failure until process restart. The first
        // attempt is held open with a TaskCompletionSource so the test observes the
        // in-flight failure deterministically (a receipt arriving only after the fault
        // would already trigger the retry and never see the exception).
        var calls = 0;
        var firstAttemptStarted = new TaskCompletionSource();
        var firstAttemptRelease = new TaskCompletionSource();
        var gate = new InvoiceCounterMigrationGate(async () =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                firstAttemptStarted.SetResult();
                await firstAttemptRelease.Task;
                throw new InvalidOperationException("storage down");
            }
        });

        await firstAttemptStarted.Task;
        var receiptDuringOutage = gate.EnsureMigratedAsync(); // joins the running first attempt
        firstAttemptRelease.SetResult();                      // first attempt now faults

        var awaitingReceipt = () => receiptDuringOutage;
        await awaitingReceipt.Should().ThrowAsync<InvalidOperationException>();

        await gate.EnsureMigratedAsync(); // retried and succeeded

        calls.Should().Be(2);
    }

    [Fact]
    public async Task Gate_RunsMigrationOnlyOnce_WhenItSucceeds()
    {
        var calls = 0;
        var gate = new InvoiceCounterMigrationGate(() =>
        {
            Interlocked.Increment(ref calls);
            return Task.CompletedTask;
        });

        await gate.EnsureMigratedAsync();
        await gate.EnsureMigratedAsync();
        await gate.EnsureMigratedAsync();

        calls.Should().Be(1);
    }

    [Fact]
    public async Task UpgradedQueue_IgnoresForeignSeriesSubmissions()
    {
        // Handwritten / mydataoverride submissions carry a caller-supplied series in
        // their segment. They are caller-numbered and must not seed the auto counter.
        var queue = TestHelpers.CreateQueue();
        queue.ftQueuedRow = 2;
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = null!,
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepo(queueGR, queue);
        var queueItemRepository = QueueItemRepo(
            HistoryItem(1, success: true, "ft1#CB-A-17"),
            HistoryItem(2, success: true, "ft2#HANDWRITTEN-9999"));

        await InvoiceCounterMigration.EnsureMigratedAsync(configRepoMock.Object, queueItemRepository, queue.ftQueueId, Mock.Of<ILogger>());

        queueGR.InvoiceNumerator.Should().Be(17);
    }

    [Fact]
    public async Task UpgradedQueue_IgnoresFailedAttemptsWithSegment()
    {
        // Failed attempts must never count as submitted, even if their response carries
        // a segment — otherwise a retry loop would drift the seed upwards and
        // reintroduce gaps.
        var queue = TestHelpers.CreateQueue();
        queue.ftQueuedRow = 2;
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = null!,
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepo(queueGR, queue);
        var queueItemRepository = QueueItemRepo(
            HistoryItem(1, success: true, "ft1#CB-A-17"),
            HistoryItem(2, success: false, "ft2#CB-A-18"));

        await InvoiceCounterMigration.EnsureMigratedAsync(configRepoMock.Object, queueItemRepository, queue.ftQueueId, Mock.Of<ILogger>());

        queueGR.InvoiceNumerator.Should().Be(17);
    }

    [Fact]
    public async Task UpgradedQueue_NeverSubmitted_SeedsZero()
    {
        // An upgraded queue that only ever produced NoOps has no submission history —
        // it must start numbering at aa = 1 no matter how far the NoOps advanced
        // ftReceiptNumerator.
        var queue = TestHelpers.CreateQueue();
        queue.ftReceiptNumerator = 40;
        queue.ftQueuedRow = 3;
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = null!,
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepo(queueGR, queue);
        var queueItemRepository = QueueItemRepo(
            HistoryItem(1, success: true, "ft1#"),
            HistoryItem(2, success: true, "ft2#"),
            HistoryItem(3, success: true, "ft3#"));

        await InvoiceCounterMigration.EnsureMigratedAsync(configRepoMock.Object, queueItemRepository, queue.ftQueueId, Mock.Of<ILogger>());

        queueGR.InvoiceSeries.Should().Be("CB-A");
        queueGR.InvoiceNumerator.Should().Be(0); // first reserved aa = 1
        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(queueGR), Times.Once);
    }

    [Fact]
    public async Task EmptyQueue_SeedsZero()
    {
        var queue = TestHelpers.CreateQueue();
        queue.ftQueuedRow = 0;
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = null!,
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepo(queueGR, queue);

        await InvoiceCounterMigration.EnsureMigratedAsync(configRepoMock.Object, QueueItemRepo(), queue.ftQueueId, Mock.Of<ILogger>());

        queueGR.InvoiceSeries.Should().Be("CB-A");
        queueGR.InvoiceNumerator.Should().Be(0);
        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(queueGR), Times.Once);
    }

    [Fact]
    public async Task AlreadyInitialized_DoesNothing()
    {
        // The migration runs at every queue start — once the counter is initialized it
        // must be a cheap no-op and never rewrite or reseed anything.
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = "CB-A",
            InvoiceNumerator = 5,
        };
        var configRepoMock = SetupConfigRepo(queueGR, queue);

        await InvoiceCounterMigration.EnsureMigratedAsync(configRepoMock.Object, QueueItemRepo(), queue.ftQueueId, Mock.Of<ILogger>());

        queueGR.InvoiceNumerator.Should().Be(5);
        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>()), Times.Never);
        configRepoMock.Verify(x => x.GetQueueAsync(It.IsAny<Guid>()), Times.Never);
    }

    private static Mock<IConfigurationRepository> SetupConfigRepo(ftQueueGR queueGR, ftQueue queue)
    {
        var repo = new Mock<IConfigurationRepository>();
        repo.Setup(x => x.GetQueueGRAsync(It.IsAny<Guid>())).ReturnsAsync(queueGR);
        repo.Setup(x => x.GetQueueAsync(It.IsAny<Guid>())).ReturnsAsync(queue);
        repo.Setup(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>())).Returns(Task.CompletedTask);
        return repo;
    }

    /// <summary>
    /// Serves the row-range reads the migration walks the history with. Items are
    /// returned in ascending row order — the opposite of the walk direction — so every
    /// test exercises the client-side ordering the range contract requires.
    /// </summary>
    private static IMiddlewareQueueItemRepository QueueItemRepo(params ftQueueItem[] queueItems)
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(x => x.GetByQueueRowRangeAsync(It.IsAny<long>(), It.IsAny<long>()))
            .Returns((long from, long to) => queueItems
                .Where(x => x.ftQueueRow >= from && x.ftQueueRow <= to)
                .OrderBy(x => x.ftQueueRow)
                .ToAsyncEnumerable());
        return repo.Object;
    }

    /// <summary>
    /// A historical queue item as the pre-counter code persisted it: the response of an
    /// AADE-submitted receipt carries "{series}-{aa}" after the "#", NoOps and failed
    /// submissions don't.
    /// </summary>
    private static ftQueueItem HistoryItem(long row, bool success, string receiptIdentification)
    {
        var response = new ReceiptResponse
        {
            ftState = ((State) 0x4752_2000_0000_0000).WithState(success ? State.Success : State.Error),
            ftCashBoxIdentification = "CB-A",
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = row,
            ftReceiptIdentification = receiptIdentification,
            ftReceiptMoment = DateTime.UtcNow,
        };
        return new ftQueueItem
        {
            ftQueueItemId = response.ftQueueItemID,
            ftQueueRow = row,
            response = JsonSerializer.Serialize(response),
        };
    }
}
