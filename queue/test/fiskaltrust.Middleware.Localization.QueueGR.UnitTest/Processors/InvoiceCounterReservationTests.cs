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
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

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
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        var request = BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001);

        var result = await processor.PointOfSaleReceipt0x0001Async(request);

        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>()), Times.Never);
        // Failed receipts must persist exactly the identification they had before the
        // reservation existed — the unconfirmed "{series}-{aa}" segment is removed.
        result.receiptResponse.ftReceiptIdentification.Should().Be("ft1#");
    }

    [Fact]
    public async Task SscdException_RestoresIdentification_AndDoesNotAdvanceCounter()
    {
        // If the SCU call throws (e.g. myDATA unreachable), SignProcessor persists the
        // response as failed. That failed receipt must carry the exact pre-reservation
        // identification — same behaviour as before this feature — and the counter must
        // not advance.
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = "CB-A",
            InvoiceNumerator = 41,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var grSSCDMock = new Mock<IGRSSCD>();
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<List<(ReceiptRequest, ReceiptResponse)>>()))
            .ThrowsAsync(new HttpRequestException("myDATA unreachable"));

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        var request = BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001);

        var act = () => processor.PointOfSaleReceipt0x0001Async(request);

        await act.Should().ThrowAsync<HttpRequestException>();
        request.ReceiptResponse.ftReceiptIdentification.Should().Be("ft1#");
        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>()), Times.Never);
    }

    [Fact]
    public async Task UnexpectedForeignSegmentOnSuccess_DoesNotAdvanceCounter_AndWritesActionJournal()
    {
        // Defensive: with handwritten numbering taken inbound and series/aa overrides
        // via mydataoverride rejected, a successful response always carries the
        // reserved segment. If it ever doesn't (a numbering bug), the counter must not
        // advance and the anomaly must be surfaced via an action journal — but the
        // receipt itself must not fail, since AADE already filed the document.
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = "CB-A",
            InvoiceNumerator = 41,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var grSSCDMock = SetupSscdMock(success: true, series: "FOREIGN", aa: 7777, mark: 999000111L);

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        var request = BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001);

        var result = await processor.PointOfSaleReceipt0x0001Async(request);

        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>()), Times.Never);
        result.receiptResponse.ftState.IsState(State.Success).Should().BeTrue();
        result.actionJournals.Should().ContainSingle()
            .Which.Message.Should().Contain("does not carry the reserved segment 'CB-A-42'");
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
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

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
        // Regression test: the reservation must be driven purely by InvoiceNumerator.
        // NoOp receipts (daily closings, zero receipts) advance ftReceiptNumerator but
        // never touch the counter, so a fresh queue submits aa = 1 no matter how many
        // NoOps preceded the first myDATA submission.
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
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        var request = BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001);

        await processor.PointOfSaleReceipt0x0001Async(request);

        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.Is<ftQueueGR>(q =>
            q.InvoiceSeries == "CB-A" &&
            q.InvoiceNumerator == 1)),
            Times.Once);
    }

    [Fact]
    public async Task Handwritten_TakesCallerNumberingInbound_AndNeverTouchesStorage()
    {
        // Handwritten documents are caller-numbered: the queue stamps the merchant's
        // (series, aa) inbound and never makes a reservation. The strict repository
        // mock proves the counter is neither read nor written.
        var queue = TestHelpers.CreateQueue();
        var configRepoMock = new Mock<IConfigurationRepository>(MockBehavior.Strict);
        var capturedIdentifications = new List<string>();
        var grSSCDMock = new Mock<IGRSSCD>();
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<List<(ReceiptRequest, ReceiptResponse)>>()))
            .ReturnsAsync((ProcessRequest req, List<(ReceiptRequest, ReceiptResponse)> _) =>
            {
                capturedIdentifications.Add(req.ReceiptResponse.ftReceiptIdentification!);
                MarkAsSuccessKeepingSuffix(req.ReceiptResponse, mark: 777L);
                return new ProcessResponse { ReceiptResponse = req.ReceiptResponse };
            });

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        var result = await processor.PointOfSaleReceipt0x0001Async(BuildHandwrittenRequest(queue, "HW", 9999));

        capturedIdentifications.Should().Equal("ft1#HW-9999");
        result.receiptResponse.ftReceiptIdentification.Should().Be("ft1#HW-9999");
        configRepoMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handwritten_Failure_RestoresIdentification_AndNeverTouchesStorage()
    {
        var queue = TestHelpers.CreateQueue();
        var configRepoMock = new Mock<IConfigurationRepository>(MockBehavior.Strict);
        var grSSCDMock = new Mock<IGRSSCD>();
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<List<(ReceiptRequest, ReceiptResponse)>>()))
            .ReturnsAsync((ProcessRequest req, List<(ReceiptRequest, ReceiptResponse)> _) =>
            {
                req.ReceiptResponse.ftState = req.ReceiptResponse.ftState.WithState(State.Error);
                return new ProcessResponse { ReceiptResponse = req.ReceiptResponse };
            });

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        var result = await processor.PointOfSaleReceipt0x0001Async(BuildHandwrittenRequest(queue, "HW", 9999));

        result.receiptResponse.ftReceiptIdentification.Should().Be("ft1#");
        configRepoMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handwritten_IncompletePayload_FallsBackToReservation()
    {
        // Handwritten flag without a usable (Series, AA) payload: the queue cannot take
        // the numbering inbound and reserves as usual. The SCU stays the single
        // validator — it rejects the request with its precise error, the reservation is
        // never confirmed, and the counter does not advance.
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
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        await processor.PointOfSaleReceipt0x0001Async(BuildHandwrittenRequest(queue, series: null, aa: null));

        configRepoMock.Verify(x => x.GetQueueGRAsync(It.IsAny<Guid>()), Times.Once);
        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>()), Times.Never);
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
    public async Task UninitializedInvoiceSeries_Throws_InsteadOfNumberingWithLegacyScheme()
    {
        // The counter is initialized once at queue start (InvoiceCounterMigration is
        // awaited before any receipt is processed) and at activation for fresh queues.
        // If the series is still unset when a submission arrives, something is genuinely
        // broken — the reservation must blow up instead of falling back to any legacy
        // numbering scheme.
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = null!, // startup migration did not run
            InvoiceNumerator = 0,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var grSSCDMock = new Mock<IGRSSCD>(MockBehavior.Strict); // must never be called

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        var act = () => processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));

        await act.Should().ThrowAsync<InvalidOperationException>();
        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>()), Times.Never);
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
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

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
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));  // succeeds, aa=1
        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));  // fails, attempts aa=2
        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));  // retry succeeds, aa=2 again

        capturedAa.Should().Equal(1L, 2L, 2L);
        queueGR.InvoiceNumerator.Should().Be(2);
    }

    [Fact]
    public async Task OverrideBetweenAutoReceipts_DoesNotShiftAutoSequence()
    {
        // Defensive sequence-resilience: even if a successful response ever came back
        // with a foreign (series, aa) — which no supported path produces anymore — the
        // auto counter must not advance, and the next auto receipt picks up at the
        // value following the *last auto-committed* one.
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
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

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

    private static ProcessCommandRequest BuildRequest(ftQueue queue, ReceiptCase receiptCase)
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
            ftQueueRow = 1,
            ftReceiptIdentification = "ft1#",
            ftReceiptMoment = DateTime.UtcNow,
        };
        return new ProcessCommandRequest(queue, receiptRequest, receiptResponse);
    }

    private static ProcessCommandRequest BuildHandwrittenRequest(ftQueue queue, string? series, long? aa)
    {
        var request = BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001);
        request.ReceiptRequest.ftReceiptCase = request.ReceiptRequest.ftReceiptCase.WithFlag(ReceiptCaseFlags.HandWritten);
        request.ReceiptRequest.ftReceiptCaseData = new { GR = new { Series = series, AA = aa } };
        return request;
    }
}
