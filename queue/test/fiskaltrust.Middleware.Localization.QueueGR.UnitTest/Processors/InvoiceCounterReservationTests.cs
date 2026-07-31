using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.Localization.QueueGR.Models;
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
        var grSSCDMock = SetupSscdMock(success: true, mark: 999000111L);

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        var request = BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001);

        var result = await processor.PointOfSaleReceipt0x0001Async(request);

        result.receiptResponse.ftState.IsState(State.Success).Should().BeTrue();
        // The reserved segment must survive on the persisted identification: the POS
        // receives the series/aa through it, and the queue-start migration seeds from
        // exactly this segment.
        result.receiptResponse.ftReceiptIdentification.Should().Be("ft1#CB-A-42");
        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.Is<ftQueueGR>(q =>
            q.InvoiceNumerator == 42 &&
            q.InvoiceSeries == "CB-A" &&
            q.LastInvoiceMark == 999000111L)),
            Times.Once);
    }

    [Fact]
    public async Task CommitFailureAfterFiledInvoice_RetryReusesSameAa_Phase2Target()
    {
        // Pins the accepted-risk window the reserve-then-commit design creates: the
        // invoice was filed (Success + mark) but persisting the counter fails. The aa
        // is consumed at AADE while the counter stays behind, so retries re-reserve the
        // same aa — which AADE will reject as 233 until the planned self-heal ships.
        // That self-heal must treat this direction as "number consumed, advance".
        var queue = TestHelpers.CreateQueue();
        // Like the Azure repository, every read returns a fresh instance reflecting the
        // last successfully persisted state (numerator 41 — the first write fails).
        var configRepoMock = new Mock<IConfigurationRepository>();
        configRepoMock.Setup(x => x.GetQueueGRAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => new ftQueueGR
            {
                ftQueueGRId = queue.ftQueueId,
                CashBoxIdentification = "CB-A",
                InvoiceSeries = "CB-A",
                InvoiceNumerator = 41,
            });
        var writeAttempts = 0;
        ftQueueGR? persisted = null;
        configRepoMock.Setup(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>()))
            .Returns((ftQueueGR q) =>
            {
                if (++writeAttempts == 1)
                {
                    throw new InvalidOperationException("storage down");
                }
                persisted = q;
                return Task.CompletedTask;
            });
        var capturedAa = new List<long>();
        var grSSCDMock = SetupAutoEchoSscdMock(capturedAa);

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        var firstAttempt = () => processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));
        await firstAttempt.Should().ThrowAsync<InvalidOperationException>();

        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));

        // Both attempts reserved the same aa — in production the second submission
        // would collide at AADE because the first one was already filed.
        capturedAa.Should().Equal(42L, 42L);
        persisted!.InvoiceNumerator.Should().Be(42);
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
        var grSSCDMock = SetupSscdMock(success: false, mark: null);

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
    public async Task SuccessWithoutMark_IsAnScuNoOp_DoesNotAdvanceCounter()
    {
        // The SCU can answer Success without filing an invoice: delivery-note
        // cancellations and Pay0x3005 payment methods go to different AADE endpoints,
        // and misconfigurations short-circuit before anything is sent. AADE's
        // invoiceMark is the proof of filing — without it the reservation must not be
        // committed and the identification must stay exactly as it was before.
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = "CB-A",
            InvoiceNumerator = 41,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var grSSCDMock = SetupSscdMock(success: true, mark: null);

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        var request = BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001);

        var result = await processor.PointOfSaleReceipt0x0001Async(request);

        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>()), Times.Never);
        result.receiptResponse.ftReceiptIdentification.Should().Be("ft1#");
    }

    [Fact]
    public async Task SuccessWithMarkCaptionButForeignSignatureType_DoesNotAdvanceCounter()
    {
        // The SCU's non-invoice flows type all their response items as
        // GenericMyDataInfo. Even if one of them were captioned "invoiceMark", it must
        // not count as a filed invoice — only a SignatureTypeGR.Mark signature does.
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
            .ReturnsAsync((ProcessRequest req, List<(ReceiptRequest, ReceiptResponse)> _) =>
            {
                req.ReceiptResponse.ftState = req.ReceiptResponse.ftState.WithState(State.Success);
                req.ReceiptResponse.AddSignatureItem(new SignatureItem
                {
                    Caption = "invoiceMark",
                    Data = "123456",
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = (SignatureType) 0x4752_2000_0000_0019, // GenericMyDataInfo
                });
                return new ProcessResponse { ReceiptResponse = req.ReceiptResponse };
            });

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        var result = await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));

        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>()), Times.Never);
        result.receiptResponse.ftReceiptIdentification.Should().Be("ft1#");
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
        var grSSCDMock = SetupSscdMock(success: true, mark: 100L);

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
    public async Task Handwritten_TakesCallerNumberingInbound_AndNeverWritesCounter()
    {
        // Handwritten documents are caller-numbered: the queue stamps the merchant's
        // (series, aa) inbound and never makes a reservation — the counter is read
        // (for the own-series guard) but never written.
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = "CB-A",
            InvoiceNumerator = 41,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var capturedIdentifications = new List<string>();
        var grSSCDMock = new Mock<IGRSSCD>();
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<List<(ReceiptRequest, ReceiptResponse)>>()))
            .ReturnsAsync((ProcessRequest req, List<(ReceiptRequest, ReceiptResponse)> _) =>
            {
                capturedIdentifications.Add(req.ReceiptResponse.ftReceiptIdentification!);
                MarkAsSuccess(req.ReceiptResponse, mark: 777L);
                return new ProcessResponse { ReceiptResponse = req.ReceiptResponse };
            });

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        var result = await processor.PointOfSaleReceipt0x0001Async(BuildHandwrittenRequest(queue, "HW", 9999));

        capturedIdentifications.Should().Equal("ft1#HW-9999");
        result.receiptResponse.ftReceiptIdentification.Should().Be("ft1#HW-9999");
        queueGR.InvoiceNumerator.Should().Be(41);
        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>()), Times.Never);
    }

    [Fact]
    public async Task Handwritten_Failure_RestoresIdentification_AndNeverWritesCounter()
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
        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>()), Times.Never);
    }

    [Fact]
    public async Task Handwritten_WithQueueOwnSeries_IsRejectedBeforeTheScu()
    {
        // Numbering in the queue's own series is assigned exclusively by the counter.
        // A handwritten payload using that series could file numbers the counter does
        // not know about, and a later automatic reservation would collide at AADE —
        // reject it before anything is submitted.
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = "CB-A",
            InvoiceNumerator = 41,
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var grSSCDMock = new Mock<IGRSSCD>(MockBehavior.Strict); // must never be called

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        var result = await processor.PointOfSaleReceipt0x0001Async(BuildHandwrittenRequest(queue, "CB-A", 9999));

        result.receiptResponse.ftState.IsState(State.Error).Should().BeTrue();
        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>()), Times.Never);
    }

    [Fact]
    public void MarkSignatureContract_IsPinnedAgainstScuGr()
    {
        // Mirrors the pin in scu-gr's AADEFactoryTests: the commit gate matches
        // signatures captioned "invoiceMark" with this exact type value, which
        // duplicates scu-gr's SignatureTypeGR.Mark. If either side drifts, the counter
        // silently stops advancing and every invoice re-submits the same aa.
        ((long) SignatureTypeGR.Mark).Should().Be(0x4752_2000_0000_0014);
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
        var grSSCDMock = SetupSscdMock(success: false, mark: null);

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
                    MarkAsSuccess(req.ReceiptResponse, mark: 100L + callCount);
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
    public async Task ScuNoOpBetweenAutoReceipts_DoesNotShiftAutoSequence()
    {
        // A receipt the SCU answers with Success but no mark (an SCU-internal NoOp,
        // e.g. a payment-method transmission) must not consume an aa. The next filed
        // receipt picks up at the value following the *last committed* one.
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
                    // SCU-internal NoOp: Success, but no invoice was filed — no mark.
                    req.ReceiptResponse.ftState = req.ReceiptResponse.ftState.WithState(State.Success);
                }
                else
                {
                    MarkAsSuccess(req.ReceiptResponse, mark: 100L + callCount);
                }
                return new ProcessResponse { ReceiptResponse = req.ReceiptResponse };
            });

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));  // filed, aa=1, commits
        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));  // SCU NoOp, no commit
        await processor.PointOfSaleReceipt0x0001Async(BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001));  // filed, aa=2 (NOT 3), commits

        // Reservations attempted: 1 (committed), 2 (not consumed by the NoOp), 2 (committed).
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
                MarkAsSuccess(req.ReceiptResponse, markCounter++);
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

    private static void MarkAsSuccess(ReceiptResponse response, long? mark)
    {
        // The SCU never touches ftReceiptIdentification — the queue is its single
        // writer. A filed invoice is recognizable purely by the AADE invoiceMark, which
        // the SendInvoices success path types as SignatureTypeGR.Mark.
        response.ftState = response.ftState.WithState(State.Success);
        if (mark.HasValue)
        {
            response.AddSignatureItem(new SignatureItem
            {
                Caption = "invoiceMark",
                Data = mark.Value.ToString(),
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypeGR.Mark.As<SignatureType>(),
            });
        }
    }

    private static Mock<IGRSSCD> SetupSscdMock(bool success, long? mark)
    {
        var mock = new Mock<IGRSSCD>();
        mock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<List<(ReceiptRequest, ReceiptResponse)>>()))
            .ReturnsAsync((ProcessRequest req, List<(ReceiptRequest, ReceiptResponse)> _) =>
            {
                var resp = req.ReceiptResponse;
                if (success)
                {
                    MarkAsSuccess(resp, mark);
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
