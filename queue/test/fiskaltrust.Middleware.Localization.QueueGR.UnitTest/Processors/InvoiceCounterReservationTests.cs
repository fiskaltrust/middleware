using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.Localization.QueueGR.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;
using FluentAssertions;
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
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

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
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        var request = BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001);

        await processor.PointOfSaleReceipt0x0001Async(request);

        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.Is<ftQueueGR>(q =>
            q.InvoiceSeries == "CB-A" &&
            q.InvoiceNumerator == 1)),
            Times.Once);
    }

    [Fact]
    public async Task UpgradedQueue_SeedsInvoiceNumeratorFromFtReceiptNumerator()
    {
        // Queues activated before the InvoiceNumerator-based fix had been submitting
        // receipts with aa derived from ftReceiptNumerator. The migration seeds
        // InvoiceNumerator from ftReceiptNumerator so the first post-upgrade reserved
        // aa lands strictly above anything AADE could already have on file, accepting
        // a one-aa cosmetic gap in exchange for a guaranteed collision-free upgrade
        // even on queues where a pre-upgrade attempt crashed after AADE-success.
        var queue = TestHelpers.CreateQueue();
        queue.ftReceiptNumerator = 17;  // queue was submitting receipts pre-upgrade
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = null!,
            InvoiceNumerator = 0, // never written under the new code yet
        };
        var configRepoMock = SetupConfigRepoMock(queueGR);
        var grSSCDMock = SetupSscdMock(success: true, series: "CB-A", aa: 18, mark: 999L);

        var processor = new ReceiptCommandProcessorGR(
            grSSCDMock.Object,
            Mock.Of<IQueueStorageProvider>(),
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        var request = BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001);

        await processor.PointOfSaleReceipt0x0001Async(request);

        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.Is<ftQueueGR>(q =>
            q.InvoiceSeries == "CB-A" &&
            q.InvoiceNumerator == 18)),
            Times.Once);
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
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepoMock.Object)));

        var request = BuildRequest(queue, ReceiptCase.PointOfSaleReceipt0x0001);

        await processor.PointOfSaleReceipt0x0001Async(request);

        configRepoMock.Verify(x => x.InsertOrUpdateQueueGRAsync(It.Is<ftQueueGR>(q =>
            q.InvoiceSeries == "CB-A" &&
            q.InvoiceNumerator == 1)),
            Times.Once);
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
}
