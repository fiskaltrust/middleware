using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.Localization.QueueGR.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.Processors;

public class InvoiceCounterReservationTests
{
    [Fact]
    public async Task SuccessfulSubmission_AdvancesInvoiceNumerator()
    {
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = "CB-A",
            InvoiceNumerator = 41,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var grSSCDMock = SetupSscdMock(success: true, series: "CB-A", aa: 42, mark: 999000111L);

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)),
            TestHelpers.CreateQueueItemRepositoryStub());

        var request = BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001);

        var result = await processor.PointOfSaleReceipt0x0001Async(request);

        result.receiptResponse.ftState.IsState(State.Success).Should().BeTrue();
        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.Is<ftQueueGR>(q =>
            q.InvoiceNumerator == 42 &&
            q.InvoiceSeries == "CB-A" &&
            q.LastInvoiceMark == 999000111L)),
            Times.Once);
    }

    [Fact]
    public async Task ErrorResponse_DoesNotAdvanceCounter()
    {
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = "CB-A",
            InvoiceNumerator = 41,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var grSSCDMock = SetupSscdMock(success: false, series: "CB-A", aa: 42, mark: null);

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)),
            TestHelpers.CreateQueueItemRepositoryStub());

        var request = BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001);

        await processor.PointOfSaleReceipt0x0001Async(request);

        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>()), Times.Never);
    }

    [Fact]
    public async Task OverridePath_SuffixMismatch_DoesNotAdvanceCounter()
    {
        // Simulates handwritten or mydataoverride: the SCU comes back successful, but
        // the (series, aa) that ended up on the AADE doc isn't the one we reserved
        // (because the override replaced it). The country processor must NOT commit
        // our reservation in that case — those documents are caller-numbered.
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = "CB-A",
            InvoiceNumerator = 41,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var grSSCDMock = SetupSscdMock(success: true, series: "HANDWRITTEN", aa: 7777, mark: 999000111L);

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)),
            TestHelpers.CreateQueueItemRepositoryStub());

        var request = BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001);

        await processor.PointOfSaleReceipt0x0001Async(request);

        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>()), Times.Never);
    }

    [Fact]
    public async Task SuccessWithoutMarkSignature_StillCommits_MarkIsNull()
    {
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = "CB-A",
            InvoiceNumerator = 41,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var grSSCDMock = SetupSscdMock(success: true, series: "CB-A", aa: 42, mark: null);

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)),
            TestHelpers.CreateQueueItemRepositoryStub());

        var request = BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001);

        await processor.PointOfSaleReceipt0x0001Async(request);

        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.Is<ftQueueGR>(q =>
            q.InvoiceNumerator == 42 &&
            q.LastInvoiceMark == null)),
            Times.Once);
    }

    [Fact]
    public async Task NewQueueWithNoOpsBeforeFirstSubmission_StartsAtAa1()
    {
        // Regression test: a brand-new queue whose activation already seeded
        // InvoiceSeries must NOT be treated as a pre-upgrade queue, even if NoOp
        // receipts (daily closings etc.) have advanced ftReceiptNumerator before
        // the first myDATA submission. Without the upgrade gating, the migration
        // would seed InvoiceNumerator from ftReceiptNumerator and emit aa=N+1,
        // recreating the very gaps this PR is trying to fix.
        var queue = TestHelpers.CreateQueue();
        queue.ftReceiptNumerator = 3;  // 3 NoOps happened post-activation
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = "CB-A",  // activation seeded this — discriminator for new vs upgraded
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var grSSCDMock = SetupSscdMock(success: true, series: "CB-A", aa: 1, mark: 100L);

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)),
            TestHelpers.CreateQueueItemRepositoryStub());

        var request = BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001);

        await processor.PointOfSaleReceipt0x0001Async(request);

        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.Is<ftQueueGR>(q =>
            q.InvoiceSeries == "CB-A" &&
            q.InvoiceNumerator == 1)),
            Times.Once);
    }

    [Fact]
    public async Task UpgradedQueue_ContinuesAtLastSubmittedAaPlusOne()
    {
        // Queues activated before this code shipped carry the last submitted aa in the
        // "{series}-{aa}" segment the old MyDataSCU appended to ftReceiptIdentification
        // on every AADE success. The migration must continue exactly at
        // last-submitted-aa + 1: zero receipts, closings and failed attempts advanced
        // ftReceiptNumerator in the meantime, and none of that may shift the sequence.
        var queue = TestHelpers.CreateQueue();
        queue.ftReceiptNumerator = 40;  // inflated by NoOps — must be irrelevant
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = null!,
            InvoiceNumerator = 0, // never written under the new code yet
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var capturedAa = new List<long>();
        var grSSCDMock = SetupAutoEchoSscdMock(capturedAa);
        var queueItemRepository = TestHelpers.CreateQueueItemRepositoryStub(
            HistoryItem(row: 5, success: true, "ft11#CB-A-17"),   // last real AADE submission
            HistoryItem(row: 6, success: true, "ft12#"),          // daily closing (NoOp, no segment)
            HistoryItem(row: 7, success: true, "ft13#"),          // zero receipt (NoOp, no segment)
            HistoryItem(row: 8, success: false, "ft14#"));        // failed old-code submission

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)),
            queueItemRepository);

        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001, queueRow: 9));

        capturedAa.Should().Equal(18L);
        queueGR.InvoiceSeries.Should().Be("CB-A");
        queueGR.InvoiceNumerator.Should().Be(18);
        // Two writes: the persisted migration seed (17), then the successful commit (18).
        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(queueGR), Times.Exactly(2));
    }

    [Fact]
    public async Task UpgradedQueue_IgnoresForeignSeriesSubmissions()
    {
        // Handwritten / mydataoverride submissions carry a caller-supplied series in
        // their segment. They are caller-numbered and must not seed the auto counter.
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = null!,
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var capturedAa = new List<long>();
        var grSSCDMock = SetupAutoEchoSscdMock(capturedAa);
        var queueItemRepository = TestHelpers.CreateQueueItemRepositoryStub(
            HistoryItem(row: 1, success: true, "ft1#CB-A-17"),
            HistoryItem(row: 2, success: true, "ft2#HANDWRITTEN-9999"));

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)),
            queueItemRepository);

        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001, queueRow: 3));

        capturedAa.Should().Equal(18L);
        queueGR.InvoiceNumerator.Should().Be(18);
    }

    [Fact]
    public async Task UpgradedQueue_FailedNewCodeAttempt_DoesNotDriftSeed()
    {
        // A failed submission made by THIS code persists the reserved segment on its
        // response together with an error state. If the migration scan runs again (the
        // seed write itself failed or crashed), it must not mistake that failed attempt
        // for a submission — otherwise every retry would drift the seed one up and
        // reintroduce gaps.
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = null!,
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var capturedAa = new List<long>();
        var grSSCDMock = SetupAutoEchoSscdMock(capturedAa);
        var queueItemRepository = TestHelpers.CreateQueueItemRepositoryStub(
            HistoryItem(row: 1, success: true, "ft1#CB-A-17"),
            HistoryItem(row: 2, success: false, "ft2#CB-A-18"));  // failed new-code attempt

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)),
            queueItemRepository);

        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001, queueRow: 3));

        capturedAa.Should().Equal(18L);  // 18 is reused, not skipped
        queueGR.InvoiceNumerator.Should().Be(18);
    }

    [Fact]
    public async Task UpgradedQueue_NeverSubmitted_StartsAtAa1()
    {
        // An upgraded queue that only ever produced NoOps (closings, zero receipts)
        // has no submission history — it must start numbering at aa = 1 no matter how
        // far the NoOps advanced ftReceiptNumerator.
        var queue = TestHelpers.CreateQueue();
        queue.ftReceiptNumerator = 40;
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = null!,
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var capturedAa = new List<long>();
        var grSSCDMock = SetupAutoEchoSscdMock(capturedAa);
        var queueItemRepository = TestHelpers.CreateQueueItemRepositoryStub(
            HistoryItem(row: 1, success: true, "ft1#"),
            HistoryItem(row: 2, success: true, "ft2#"),
            HistoryItem(row: 3, success: true, "ft3#"));

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)),
            queueItemRepository);

        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001, queueRow: 4));

        capturedAa.Should().Equal(1L);
        queueGR.InvoiceNumerator.Should().Be(1);
    }

    [Fact]
    public async Task UpgradedQueue_MigrationSeedIsPersisted_EvenWhenFirstSubmissionFails()
    {
        // The seed is written immediately after the history scan, so the scan runs at
        // most once per queue and the migration outcome is observable even if the first
        // post-upgrade submission fails.
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = null!,
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var grSSCDMock = SetupSscdMock(success: false, series: "CB-A", aa: 18, mark: null);
        var queueItemRepository = TestHelpers.CreateQueueItemRepositoryStub(
            HistoryItem(row: 1, success: true, "ft1#CB-A-17"));

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)),
            queueItemRepository);

        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001, queueRow: 2));

        queueGR.InvoiceSeries.Should().Be("CB-A");
        queueGR.InvoiceNumerator.Should().Be(17);  // seed only — the failed attempt must not commit 18
        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(queueGR), Times.Once);
    }

    [Theory]
    [InlineData(ReceiptCase.ZeroReceipt0x2000)]
    [InlineData(ReceiptCase.OneReceipt0x2001)]
    [InlineData(ReceiptCase.ShiftClosing0x2010)]
    [InlineData(ReceiptCase.DailyClosing0x2011)]
    [InlineData(ReceiptCase.MonthlyClosing0x2012)]
    [InlineData(ReceiptCase.YearlyClosing0x2013)]
    public async Task DailyOperations_AreNoOps_AndNeverTouchTheInvoiceCounter(ReceiptCase receiptCase)
    {
        // GR routes zero receipts and all closings to GRFallBackOperations.NoOp: no SCU
        // call, no counter reservation, no "{series}-{aa}" segment on the identification.
        // DailyOperationsCommandProcessorGR has no storage dependency at all, so these
        // receipt cases can neither consume nor advance an aa.
        var queue = TestHelpers.CreateQueue();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(receiptCase),
            cbReceiptMoment = DateTime.UtcNow,
        };
        var receiptResponse = new ReceiptResponse
        {
            ftState = (State) 0x4752_2000_0000_0000,
            ftCashBoxIdentification = "CB-A",
            ftQueueID = queue.ftQueueId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptIdentification = "ft1#",
            ftReceiptMoment = DateTime.UtcNow,
        };

        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), Mock.Of<IMarketValidator>(), null!, null!, new DailyOperationsCommandProcessorGR(), null!, null!);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftReceiptIdentification.Should().Be("ft1#");
    }

    [Fact]
    public async Task EmptyInvoiceSeries_FallsBackToCashBoxIdentification()
    {
        // Queues that existed before the activation seeding ran (or before the upgrade)
        // may have InvoiceSeries unset. The reservation must still produce a stable
        // series rather than crashing or emitting an empty one.
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = null!, // not yet seeded
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var grSSCDMock = SetupSscdMock(success: true, series: "CB-A", aa: 1, mark: 12345L);

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)),
            TestHelpers.CreateQueueItemRepositoryStub());

        var request = BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001);

        await processor.PointOfSaleReceipt0x0001Async(request);

        queueGR.InvoiceSeries.Should().Be("CB-A");
        queueGR.InvoiceNumerator.Should().Be(1);
        // Two writes: the persisted migration seed (0), then the successful commit (1).
        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(queueGR), Times.Exactly(2));
    }

    private static Mock<IConfigurationRepository> SetupConfigRepoMock(ftQueueGR queueGR)
    {
        var repo = new Mock<IConfigurationRepository>();
        repo.Setup(x => x.GetQueueGRAsync(It.IsAny<Guid>())).ReturnsAsync(queueGR);
        repo.Setup(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>())).Returns(Task.CompletedTask);
        return repo;
    }

    [Fact]
    public async Task FiveSuccessfulSubmissions_ProduceContinuousAa1Through5()
    {
        // Continuity smoke: five POS receipts in a row on a fresh queue must produce
        // exactly aa = 1, 2, 3, 4, 5 on AADE with no skips.
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = "CB-A",
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var capturedAa = new List<long>();
        var grSSCDMock = SetupAutoEchoSscdMock(capturedAa);

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)),
            TestHelpers.CreateQueueItemRepositoryStub());

        for (var i = 0; i < 5; i++)
        {
            await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));
        }

        capturedAa.Should().Equal(1L, 2L, 3L, 4L, 5L);
        queueGR.InvoiceNumerator.Should().Be(5);
    }

    [Fact]
    public async Task RetryAfterErrorReusesSameAa()
    {
        // Failure semantics: if AADE rejects a submission (State.Error), the counter
        // must not advance and the retry must reuse the exact same aa.
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = "CB-A",
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var capturedAa = new List<long>();
        var callCount = 0;

        var grSSCDMock = new Mock<IGRSSCD>();
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<List<(ReceiptRequest, ReceiptResponse)>>()))
            .ReturnsAsync((ProcessRequest req, List<(ReceiptRequest, ReceiptResponse)> _) =>
            {
                callCount++;
                CaptureReservedAa(req.ReceiptResponse, capturedAa);
                if (callCount == 2)
                {
                    // Second call fails. Counter must not advance.
                    req.ReceiptResponse.ftState = req.ReceiptResponse.ftState.WithState(State.Error);
                }
                else
                {
                    MarkAsSuccessKeepingSuffix(req.ReceiptResponse, mark: 100L + callCount);
                }
                return new ProcessResponse { ReceiptResponse = req.ReceiptResponse };
            });

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)),
            TestHelpers.CreateQueueItemRepositoryStub());

        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));  // succeeds, aa=1
        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));  // fails, attempts aa=2
        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));  // retry succeeds, aa=2 again

        capturedAa.Should().Equal(1L, 2L, 2L);
        queueGR.InvoiceNumerator.Should().Be(2);
    }

    [Fact]
    public async Task OverrideBetweenAutoReceipts_DoesNotShiftAutoSequence()
    {
        // A receipt that submits under a caller-supplied (series, aa) — handwritten or
        // mydataoverride — must not advance the auto counter. The next auto receipt
        // therefore picks up at the value following the *last auto-committed* one,
        // not at the value following the override.
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = "CB-A",
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var capturedAa = new List<long>();
        var callCount = 0;

        var grSSCDMock = new Mock<IGRSSCD>();
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<List<(ReceiptRequest, ReceiptResponse)>>()))
            .ReturnsAsync((ProcessRequest req, List<(ReceiptRequest, ReceiptResponse)> _) =>
            {
                callCount++;
                CaptureReservedAa(req.ReceiptResponse, capturedAa);
                if (callCount == 2)
                {
                    // Override path: suffix gets rewritten to something else (simulates
                    // handwritten or mydataoverride replacing series/aa on the doc).
                    OverwriteSuffix(req.ReceiptResponse, "HANDWRITTEN", 9999, mark: 5_555_555L);
                }
                else
                {
                    MarkAsSuccessKeepingSuffix(req.ReceiptResponse, mark: 100L + callCount);
                }
                return new ProcessResponse { ReceiptResponse = req.ReceiptResponse };
            });

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)),
            TestHelpers.CreateQueueItemRepositoryStub());

        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));  // auto, aa=1, commits
        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));  // override, suffix mismatch, no commit
        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));  // auto, aa=2 (NOT 3), commits

        // Reservations attempted: 1 (committed), 2 (lost to override), 2 (committed).
        capturedAa.Should().Equal(1L, 2L, 2L);
        queueGR.InvoiceNumerator.Should().Be(2);
    }

    private static Mock<IGRSSCD> SetupAutoEchoSscdMock(List<long> capturedAa, long startMark = 100L)
    {
        var markCounter = startMark;
        var mock = new Mock<IGRSSCD>();
        mock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<List<(ReceiptRequest, ReceiptResponse)>>()))
            .ReturnsAsync((ProcessRequest req, List<(ReceiptRequest, ReceiptResponse)> _) =>
            {
                CaptureReservedAa(req.ReceiptResponse, capturedAa);
                MarkAsSuccessKeepingSuffix(req.ReceiptResponse, markCounter++);
                return new ProcessResponse { ReceiptResponse = req.ReceiptResponse };
            });
        return mock;
    }

    private static void CaptureReservedAa(ReceiptResponse response, List<long> sink)
    {
        var identification = response.ftReceiptIdentification ?? string.Empty;
        var hashIdx = identification.IndexOf('#');
        if (hashIdx < 0)
        {
            return;
        }
        var suffix = identification.Substring(hashIdx + 1);
        var dashIdx = suffix.LastIndexOf('-');
        if (dashIdx > 0 && long.TryParse(suffix.Substring(dashIdx + 1), out var aa))
        {
            sink.Add(aa);
        }
    }

    private static void MarkAsSuccessKeepingSuffix(ReceiptResponse response, long mark)
    {
        // Auto path: MyDataSCU's SetCountrySuffix rewrites with the doc's values, which
        // in the no-override case equal what the country processor pre-appended — so the
        // suffix stays unchanged.
        response.ftState = response.ftState.WithState(State.Success);
        response.AddSignatureItem(new SignatureItem
        {
            Caption = "invoiceMark",
            Data = mark.ToString(),
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = (SignatureType) 0,
        });
    }

    private static void OverwriteSuffix(ReceiptResponse response, string overrideSeries, long overrideAa, long mark)
    {
        // Override path: MyDataSCU rewrites the suffix with the override values from the
        // doc, producing a string that does not end with the country processor's
        // pre-appended reservation.
        response.ftState = response.ftState.WithState(State.Success);
        var identification = response.ftReceiptIdentification ?? string.Empty;
        var hashIdx = identification.IndexOf('#');
        var prefix = hashIdx >= 0 ? identification.Substring(0, hashIdx + 1) : identification + "#";
        response.ftReceiptIdentification = prefix + $"{overrideSeries}-{overrideAa}";
        response.AddSignatureItem(new SignatureItem
        {
            Caption = "invoiceMark",
            Data = mark.ToString(),
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = (SignatureType) 0,
        });
    }

    private static Mock<IGRSSCD> SetupSscdMock(bool success, string series, long aa, long? mark)
    {
        var mock = new Mock<IGRSSCD>();
        mock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<List<(ReceiptRequest, ReceiptResponse)>>()))
            .ReturnsAsync((ProcessRequest req, List<(ReceiptRequest, ReceiptResponse)> _) =>
            {
                var resp = req.ReceiptResponse;
                if (success)
                {
                    resp.ftState = resp.ftState.WithState(State.Success);
                    // MyDataSCU rewrites the country segment after "#" with the (series, aa)
                    // actually submitted to AADE; we mirror that behaviour here.
                    var identification = resp.ftReceiptIdentification ?? string.Empty;
                    var hashIdx = identification.IndexOf('#');
                    var prefix = hashIdx >= 0 ? identification.Substring(0, hashIdx + 1) : identification + "#";
                    resp.ftReceiptIdentification = prefix + $"{series}-{aa}";
                    if (mark.HasValue)
                    {
                        resp.AddSignatureItem(new SignatureItem
                        {
                            Caption = "invoiceMark",
                            Data = mark.Value.ToString(),
                            ftSignatureFormat = SignatureFormat.Text,
                            ftSignatureType = (SignatureType) 0,
                        });
                    }
                }
                else
                {
                    resp.ftState = resp.ftState.WithState(State.Error);
                }
                return new ProcessResponse { ReceiptResponse = resp };
            });
        return mock;
    }

    private static ProcessCommandRequest BuildRequest(ftQueue queue, ReceiptCase receiptCase, long queueRow = 1)
    {
        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(receiptCase),
            cbReceiptMoment = DateTime.UtcNow,
        };
        var receiptResponse = new ReceiptResponse
        {
            ftState = (State) 0x4752_2000_0000_0000,
            ftCashBoxIdentification = "CB-A",
            ftQueueID = queue.ftQueueId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = queueRow,
            ftReceiptIdentification = "ft1#",
            ftReceiptMoment = DateTime.UtcNow,
        };
        return new ProcessCommandRequest(queue, receiptRequest, receiptResponse);
    }

    /// <summary>
    /// A historical queue item as the pre-upgrade code persisted it: the response of an
    /// AADE-submitted receipt carries "{series}-{aa}" after the "#", NoOps and failed
    /// submissions don't (failed new-code attempts do, but with an error state).
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
