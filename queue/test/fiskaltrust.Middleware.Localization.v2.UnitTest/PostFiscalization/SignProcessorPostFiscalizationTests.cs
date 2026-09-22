using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.Localization.v2.PostFiscalization;
using fiskaltrust.Middleware.Localization.v2.PostFiscalization.Contracts;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.PostFiscalization;

/// <summary>The two RFC 712 seams in the shared <see cref="SignProcessor"/>, exercised with scripted services.</summary>
public class SignProcessorPostFiscalizationTests
{
    private sealed class Harness
    {
        public Guid QueueId { get; } = Guid.NewGuid();
        public Guid CashBoxId { get; } = Guid.NewGuid();
        public Mock<IMiddlewareQueueItemRepository> QueueItemRepository { get; } = new();
        public List<ftQueueItem> PersistedQueueItems { get; } = [];
        public List<ftActionJournal> ActionJournals { get; } = [];
        public List<ftReceiptJournal> ReceiptJournals { get; } = [];
        public int CountryProcessorCalls { get; private set; }
        public Func<ReceiptRequest, ReceiptResponse, ftQueue, ftQueueItem, Task<(ReceiptResponse, List<ftActionJournal>)>> CountryProcessor { get; set; }
            = (_, response, _, _) => Task.FromResult((response, new List<ftActionJournal>()));

        public SignProcessor Create(FakePostFiscalizationService? eInvoicing, FakePostFiscalizationService? eReporting, bool sandbox = false)
        {
            var storageProvider = new Mock<IStorageProvider>();
            var configurationRepository = new Mock<IConfigurationRepository>();
            configurationRepository.Setup(x => x.GetQueueAsync(QueueId)).ReturnsAsync(new ftQueue
            {
                ftQueueId = QueueId,
                ftCashBoxId = CashBoxId,
                Timeout = 300,
                StartMoment = DateTime.MinValue,
                StopMoment = null,
                CountryCode = "EU",
            });
            var actionJournalRepository = new Mock<IMiddlewareActionJournalRepository>();
            actionJournalRepository.Setup(x => x.InsertAsync(It.IsAny<ftActionJournal>())).Callback<ftActionJournal>(ActionJournals.Add).Returns(Task.CompletedTask);
            var receiptJournalRepository = new Mock<IMiddlewareReceiptJournalRepository>();
            receiptJournalRepository.Setup(x => x.InsertAsync(It.IsAny<ftReceiptJournal>())).Callback<ftReceiptJournal>(ReceiptJournals.Add).Returns(Task.CompletedTask);
            QueueItemRepository.Setup(x => x.InsertOrUpdateAsync(It.IsAny<ftQueueItem>())).Callback<ftQueueItem>(PersistedQueueItems.Add).Returns(Task.CompletedTask);

            storageProvider.Setup(x => x.CreateConfigurationRepository()).Returns(new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configurationRepository.Object)));
            storageProvider.Setup(x => x.CreateMiddlewareQueueItemRepository()).Returns(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(QueueItemRepository.Object)));
            storageProvider.Setup(x => x.CreateMiddlewareActionJournalRepository()).Returns(new AsyncLazy<IMiddlewareActionJournalRepository>(() => Task.FromResult(actionJournalRepository.Object)));
            storageProvider.Setup(x => x.CreateMiddlewareReceiptJournalRepository()).Returns(new AsyncLazy<IMiddlewareReceiptJournalRepository>(() => Task.FromResult(receiptJournalRepository.Object)));

            var configuration = new MiddlewareConfiguration { QueueId = QueueId, CashBoxId = CashBoxId, IsSandbox = sandbox, ReceiptRequestMode = 0 };
            return new SignProcessor(
                Mock.Of<ILogger<SignProcessor>>(),
                new QueueStorageProvider(QueueId, storageProvider.Object),
                (request, response, queue, queueItem) =>
                {
                    CountryProcessorCalls++;
                    return CountryProcessor(request, response, queue, queueItem);
                },
                new AsyncLazy<string>(() => Task.FromResult("CB-1")),
                configuration,
                new PostFiscalizationProcessor(Mock.Of<ILogger<PostFiscalizationProcessor>>(), eInvoicing, eReporting));
        }

        public ReceiptRequest Request(ReceiptCase receiptCase = (ReceiptCase) 0x4752_2000_0000_1001) => new()
        {
            ftCashBoxID = CashBoxId,
            cbReceiptReference = "R-1",
            cbTerminalID = "T1",
            cbReceiptMoment = DateTime.UtcNow,
            ftReceiptCase = receiptCase,
            cbChargeItems = [],
            cbPayItems = [],
        };
    }

    private static SignatureItem Signature(string caption, string data) => new()
    {
        Caption = caption,
        Data = data,
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = (SignatureType) 0x4752_2000_0000_0010,
    };

    private static Func<ProcessRequest, Task<ProcessResponse>> AddingSignature(string caption, string data) => request =>
    {
        var returned = JsonSerializer.Deserialize<ReceiptResponse>(JsonSerializer.Serialize(request.ReceiptResponse))!;
        returned.ftSignatures.Add(Signature(caption, data));
        return Task.FromResult(new ProcessResponse { ReceiptResponse = returned });
    };

    [Fact]
    public async Task PreflightRejection_RefusesTheReceipt_WithoutQueueItemFiscalizationOrReceiptJournal()
    {
        var harness = new Harness();
        var eInvoicing = FakePostFiscalizationService.Rejecting("cbCustomer is required for B2B invoices");
        var eReporting = FakePostFiscalizationService.Applying();
        var processor = harness.Create(eInvoicing, eReporting);
        var request = harness.Request();

        var response = await processor.ProcessAsync(request);

        response.Should().NotBeNull();
        response!.ftState.IsState(State.Error).Should().BeTrue();
        response.ftQueueItemID.Should().Be(Guid.Empty);
        response.ftQueueID.Should().Be(harness.QueueId);
        response.ftCashBoxID.Should().Be(harness.CashBoxId);
        response.cbReceiptReference.Should().Be("R-1");
        response.ftCashBoxIdentification.Should().Be("CB-1");
        response.ftReceiptIdentification.Should().BeEmpty();
        var signature = response.ftSignatures.Should().ContainSingle().Subject;
        signature.Caption.Should().Be("einvoicing-rejected");
        signature.Data.Should().Be("cbCustomer is required for B2B invoices");
        signature.ftSignatureType.Should().Be(request.ftReceiptCase.Reset().As<SignatureType>().WithCategory(SignatureTypeCategory.Failure));

        harness.PersistedQueueItems.Should().BeEmpty("no queue item is created for a receipt refused before fiscalization");
        harness.CountryProcessorCalls.Should().Be(0);
        harness.ReceiptJournals.Should().BeEmpty();
        eReporting.ValidateCalls.Should().BeEmpty();
        eInvoicing.ProcessCalls.Should().BeEmpty();
        var journal = harness.ActionJournals.Should().ContainSingle().Subject;
        journal.Type.Should().Be("einvoicing-rejected");
        journal.ftQueueId.Should().Be(harness.QueueId);
        journal.ftQueueItemId.Should().Be(Guid.Empty);
        journal.Message.Should().Contain("R-1").And.Contain("cbCustomer is required");
        journal.DataJson.Should().Contain("\"phase\":\"validate\"").And.Contain("\"outcome\":\"rejected\"");
    }

    [Fact]
    public async Task PreflightTransportFailure_RefusesTheReceipt_NamingTheCause()
    {
        var harness = new Harness();
        var eReporting = FakePostFiscalizationService.FailingValidate(new PostFiscalizationServiceException("timeout after 2 attempt(s) of 15000 ms each"));
        var processor = harness.Create(FakePostFiscalizationService.Applying(), eReporting);

        var response = await processor.ProcessAsync(harness.Request());

        response!.ftState.IsState(State.Error).Should().BeTrue();
        var signature = response.ftSignatures.Should().ContainSingle().Subject;
        signature.Caption.Should().Be("ereporting-rejected");
        signature.Data.Should().Be("eReporting validate call failed: timeout after 2 attempt(s) of 15000 ms each");
        harness.PersistedQueueItems.Should().BeEmpty();
        harness.ReceiptJournals.Should().BeEmpty();
        harness.ActionJournals.Should().ContainSingle().Which.DataJson.Should().Contain("\"outcome\":\"failed\"");
    }

    [Fact]
    public async Task FinalizeSuccess_MergesServiceSignatures_PersistsThem_AndCreatesTheReceiptJournal()
    {
        var harness = new Harness();
        var eInvoicing = new FakePostFiscalizationService { OnProcess = AddingSignature("einvoice-id", "urn:peppol:1") };
        var eReporting = new FakePostFiscalizationService { OnProcess = AddingSignature("ereport-id", "rep-1") };
        harness.CountryProcessor = (_, response, _, _) =>
        {
            response.ftSignatures.Add(Signature("invoiceMark", "400001924190871"));
            return Task.FromResult((response, new List<ftActionJournal>()));
        };
        var processor = harness.Create(eInvoicing, eReporting);

        var response = await processor.ProcessAsync(harness.Request());

        response!.ftState.IsState(State.Error).Should().BeFalse();
        response.ftSignatures.Select(signature => signature.Caption).Should().Equal("invoiceMark", "einvoice-id", "ereport-id");
        var processRequest = eInvoicing.ProcessCalls.Should().ContainSingle().Subject;
        processRequest.ReceiptResponse.ftQueueItemID.Should().NotBe(Guid.Empty);
        processRequest.ReceiptResponse.ftReceiptIdentification.Should().StartWith("ft");
        processRequest.ReceiptResponse.ftSignatures.Should().ContainSingle(signature => signature.Caption == "invoiceMark", "the service sees everything the queue and SCU produced");
        var outcome = MiddlewareStateData.FromReceiptResponse(response)!.PostFiscalization!;
        outcome.FiscalizationSucceeded.Should().BeTrue();
        outcome.EInvoicing.Should().Be(PostFiscalizationStateData.Ok);
        outcome.EReporting.Should().Be(PostFiscalizationStateData.Ok);

        harness.ReceiptJournals.Should().ContainSingle();
        var persisted = harness.PersistedQueueItems.Last();
        persisted.response.Should().Contain("einvoice-id").And.Contain("ereport-id").And.Contain("\"PostFiscalization\"");
        harness.ActionJournals.Should().BeEmpty();
    }

    [Fact]
    public async Task FinalizeFailure_ReturnsAndPersistsTheErrorResponse_ButStillCreatesTheReceiptJournal()
    {
        var harness = new Harness();
        var eInvoicing = FakePostFiscalizationService.FailingProcess(new PostFiscalizationServiceException("HTTP 503 Service Unavailable after 2 attempts", "maintenance"));
        var processor = harness.Create(eInvoicing, null);

        var response = await processor.ProcessAsync(harness.Request());

        response!.ftState.IsState(State.Error).Should().BeTrue();
        response.ftQueueItemID.Should().NotBe(Guid.Empty);
        var failure = response.ftSignatures.Should().ContainSingle(signature => signature.Caption == "einvoicing-failed").Subject;
        failure.Data.Should().Contain("HTTP 503 Service Unavailable after 2 attempts");

        harness.CountryProcessorCalls.Should().Be(1);
        harness.ReceiptJournals.Should().ContainSingle("the receipt is fiscalized, so it is journaled although it carries an error state");
        var persisted = harness.PersistedQueueItems.Last();
        persisted.ftQueueItemId.Should().Be(response.ftQueueItemID);
        persisted.response.Should().Contain("einvoicing-failed").And.Contain("\"FiscalizationSucceeded\":true").And.Contain("\"EInvoicing\":\"failed\"");
        JsonSerializer.Deserialize<ReceiptResponse>(persisted.response)!.IsFiscalized().Should().BeTrue();

        var journal = harness.ActionJournals.Should().ContainSingle().Subject;
        journal.Type.Should().Be("einvoicing-failed");
        journal.ftQueueItemId.Should().Be(response.ftQueueItemID);
        journal.DataJson.Should().Contain("maintenance");
    }

    [Fact]
    public async Task FiscalizationFailure_SkipsFinalize_AndCreatesNoReceiptJournal()
    {
        var harness = new Harness();
        var eInvoicing = FakePostFiscalizationService.Applying();
        harness.CountryProcessor = (_, response, _, _) =>
        {
            response.SetReceiptResponseError("SCU unreachable");
            return Task.FromResult((response, new List<ftActionJournal>()));
        };
        var processor = harness.Create(eInvoicing, null);

        var response = await processor.ProcessAsync(harness.Request());

        response!.ftState.IsState(State.Error).Should().BeTrue();
        eInvoicing.ValidateCalls.Should().ContainSingle();
        eInvoicing.ProcessCalls.Should().BeEmpty();
        response.ftSignatures.Should().ContainSingle().Which.Caption.Should().Be("FAILURE");
        MiddlewareStateData.FromReceiptResponse(response).Should().BeNull("no PostFiscalization section is written when fiscalization failed");
        harness.ReceiptJournals.Should().BeEmpty();
        harness.ActionJournals.Should().ContainSingle().Which.Message.Should().Contain("0xEEEE_EEEE");
        JsonSerializer.Deserialize<ReceiptResponse>(harness.PersistedQueueItems.Last().response)!.IsFiscalized().Should().BeFalse();
    }

    [Fact]
    public async Task UncaughtCountryProcessorException_SkipsFinalize()
    {
        var harness = new Harness();
        var eInvoicing = FakePostFiscalizationService.Applying();
        harness.CountryProcessor = (_, _, _, _) => throw new InvalidOperationException("boom");
        var processor = harness.Create(eInvoicing, null);

        var response = await processor.ProcessAsync(harness.Request());

        response!.ftState.IsState(State.Error).Should().BeTrue();
        eInvoicing.ProcessCalls.Should().BeEmpty();
        harness.ReceiptJournals.Should().BeEmpty();
    }

    [Fact]
    public async Task Sandbox_AppendsTheSandboxSignatureAfterTheServiceSignatures()
    {
        var harness = new Harness();
        var eInvoicing = new FakePostFiscalizationService { OnProcess = AddingSignature("einvoice-id", "urn:peppol:1") };
        var processor = harness.Create(eInvoicing, null, sandbox: true);

        var response = await processor.ProcessAsync(harness.Request());

        response!.ftSignatures.Select(signature => signature.Caption).Should().Equal("einvoice-id", "S A N D B O X");
        eInvoicing.ProcessCalls.Single().ReceiptResponse.ftSignatures.Should().NotContain(signature => signature.Caption == "S A N D B O X");
    }

    [Fact]
    public async Task ReceiptRequested_ReturnsThePersistedResponse_WithoutCallingTheServices()
    {
        var harness = new Harness();
        var eInvoicing = FakePostFiscalizationService.Applying();
        var request = harness.Request(((ReceiptCase) 0x4752_2000_0000_1001).WithFlag(ReceiptCaseFlags.ReceiptRequested));
        var persistedResponse = new ReceiptResponse
        {
            ftQueueItemID = Guid.NewGuid(),
            cbReceiptReference = "R-1",
            ftState = ((State) 0x4752_2000_0000_0000).WithState(State.Error),
            ftSignatures = [Signature("einvoicing-failed", "eInvoicing process call failed after fiscalization: timeout")],
            ftStateData = new MiddlewareStateData { PostFiscalization = new PostFiscalizationStateData { FiscalizationSucceeded = true, EInvoicing = PostFiscalizationStateData.Failed } },
        };
        var persistedRequest = harness.Request();
        harness.QueueItemRepository.Setup(x => x.GetByReceiptReferenceAsync("R-1", "T1")).Returns(new[]
        {
            new ftQueueItem
            {
                ftQueueItemId = persistedResponse.ftQueueItemID,
                cbReceiptReference = "R-1",
                cbTerminalID = "T1",
                request = JsonSerializer.Serialize(persistedRequest),
                response = JsonSerializer.Serialize(persistedResponse),
                responseHash = "hash",
                ftDoneMoment = DateTime.UtcNow,
            },
        }.ToAsyncEnumerable());
        var processor = harness.Create(eInvoicing, null);

        var response = await processor.ProcessAsync(request);

        response!.ftQueueItemID.Should().Be(persistedResponse.ftQueueItemID);
        response.ftState.IsState(State.Error).Should().BeTrue();
        response.ftSignatures.Should().ContainSingle().Which.Caption.Should().Be("einvoicing-failed");
        response.IsFiscalized().Should().BeTrue("the POS can tell a fiscalized-but-failed receipt from an unfiscalized one");
        eInvoicing.ValidateCalls.Should().BeEmpty();
        eInvoicing.ProcessCalls.Should().BeEmpty();
        harness.CountryProcessorCalls.Should().Be(0);
        harness.PersistedQueueItems.Should().BeEmpty();
    }

    [Fact]
    public async Task NotApplicableServices_LeaveTheReceiptUntouched_ButRecordTheOutcome()
    {
        var harness = new Harness();
        var processor = harness.Create(FakePostFiscalizationService.NotApplying(), FakePostFiscalizationService.NotApplying());

        var response = await processor.ProcessAsync(harness.Request((ReceiptCase) 0x4752_2000_0000_2011));

        response!.ftState.IsState(State.Error).Should().BeFalse();
        response.ftSignatures.Should().BeEmpty();
        var outcome = MiddlewareStateData.FromReceiptResponse(response)!.PostFiscalization!;
        outcome.EInvoicing.Should().Be(PostFiscalizationStateData.NotApplicable);
        outcome.EReporting.Should().Be(PostFiscalizationStateData.NotApplicable);
        harness.ReceiptJournals.Should().ContainSingle();
    }
}
