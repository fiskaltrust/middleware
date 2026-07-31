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
    public async Task UpgradedQueue_SeedsAtMaxOwnSeriesAa_NotAtNewest()
    {
        // A historical aa-only mydataoverride could file a caller-chosen aa into the
        // queue's own series. If such a submission is newer than the last automatic one
        // and carries a LOWER aa, a newest-based seed would restart below already-filed
        // values and every following reservation would collide at AADE (233, which
        // nothing self-heals yet). The seed must therefore be the maximum own-series
        // aa, not the newest.
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

        queueGR.InvoiceNumerator.Should().Be(17);
    }

    [Fact]
    public async Task Gate_RetriesFaultedMigration_OnNextReceipt()
    {
        // A transient storage error at startup must only fail the receipts processed
        // during the outage — the next receipt retries the migration instead of
        // rethrowing a permanently cached failure until process restart.
        var calls = 0;
        var gate = new InvoiceCounterMigrationGate(() =>
            Interlocked.Increment(ref calls) == 1
                ? Task.FromException(new InvalidOperationException("storage down"))
                : Task.CompletedTask);

        var firstReceipt = () => gate.EnsureMigratedAsync();
        await firstReceipt.Should().ThrowAsync<InvalidOperationException>();

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

    private static IMiddlewareQueueItemRepository QueueItemRepo(params ftQueueItem[] queueItems)
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(x => x.GetByQueueRowAsync(It.IsAny<long>()))
            .ReturnsAsync((long row) => queueItems.FirstOrDefault(x => x.ftQueueRow == row));
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
