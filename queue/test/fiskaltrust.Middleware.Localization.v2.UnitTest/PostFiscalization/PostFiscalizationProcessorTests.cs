using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.PostFiscalization;
using fiskaltrust.Middleware.PostFiscalization.Contracts;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.PostFiscalization;

public class PostFiscalizationProcessorTests
{
    private static readonly Guid _cashBoxId = Guid.NewGuid();
    private static readonly Guid _queueId = Guid.NewGuid();
    private static readonly State _successState = (State) 0x4752_2000_0000_0000;

    private sealed class MarketStateData : MiddlewareStateData
    {
        [JsonPropertyName("MARKET")]
        public string? Market { get; set; }
    }

    private static PostFiscalizationProcessor Processor(FakePostFiscalizationService? eInvoicing, FakePostFiscalizationService? eReporting)
        => new(NullLogger<PostFiscalizationProcessor>.Instance, eInvoicing, eReporting);

    private static ReceiptRequest Request() => new()
    {
        ftCashBoxID = _cashBoxId,
        cbReceiptReference = "R-1",
        cbTerminalID = "T1",
        ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_1001,
        cbChargeItems = [],
        cbPayItems = [],
    };

    private static ftQueueItem QueueItem() => new() { ftQueueItemId = Guid.NewGuid(), ftQueueId = _queueId, ftQueueRow = 7 };

    private static SignatureItem Signature(string caption, string data) => new()
    {
        Caption = caption,
        Data = data,
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = (SignatureType) 0x4752_2000_0000_0010,
    };

    private static ReceiptResponse Fiscalized(ftQueueItem queueItem, object? stateData = null) => new()
    {
        ftQueueID = queueItem.ftQueueId,
        ftQueueItemID = queueItem.ftQueueItemId,
        ftQueueRow = queueItem.ftQueueRow,
        ftCashBoxID = _cashBoxId,
        cbReceiptReference = "R-1",
        cbTerminalID = "T1",
        ftCashBoxIdentification = "CB-1",
        ftReceiptIdentification = "ft1A#",
        ftReceiptMoment = DateTime.UtcNow,
        ftState = _successState,
        ftSignatures = [Signature("invoiceMark", "400001924190871")],
        ftStateData = stateData,
    };

    /// <summary>Simulates the JSON round trip a real service response takes.</summary>
    private static ReceiptResponse RoundTrip(ReceiptResponse response) => JsonSerializer.Deserialize<ReceiptResponse>(JsonSerializer.Serialize(response))!;

    private static Func<ProcessRequest, Task<ProcessResponse>> AddingSignature(string caption, string data) => request =>
    {
        var returned = RoundTrip(request.ReceiptResponse);
        returned.ftSignatures.Add(Signature(caption, data));
        return Task.FromResult(new ProcessResponse { ReceiptResponse = returned });
    };

    private static PostFiscalizationStateData Outcome(ReceiptResponse response)
    {
        var stateData = MiddlewareStateData.FromReceiptResponse(response);
        stateData.Should().NotBeNull();
        stateData!.PostFiscalization.Should().NotBeNull();
        return stateData.PostFiscalization!;
    }

    [Fact]
    public async Task Disabled_IsANoOp()
    {
        var processor = PostFiscalizationProcessor.Disabled(NullLogger<PostFiscalizationProcessor>.Instance);
        var queueItem = QueueItem();
        var response = Fiscalized(queueItem);
        var journals = new List<ftActionJournal>();

        var preflight = await processor.ValidateAsync(Request());
        var finalized = await processor.FinalizeAsync(Request(), response, queueItem, preflight, journals);

        processor.IsEnabled.Should().BeFalse();
        preflight.Should().BeSameAs(PostFiscalizationPreflight.Disabled);
        preflight.Accepted.Should().BeTrue();
        finalized.Should().BeSameAs(response);
        finalized.ftStateData.Should().BeNull();
        finalized.ftSignatures.Should().ContainSingle();
        journals.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_CallsEInvoicingThenEReporting_WithTheReceiptRequest()
    {
        var order = new List<string>();
        var eInvoicing = new FakePostFiscalizationService { OnValidate = _ => { order.Add("einvoicing"); return Task.FromResult(new ValidateResponse { Applies = true }); } };
        var eReporting = new FakePostFiscalizationService { OnValidate = _ => { order.Add("ereporting"); return Task.FromResult(new ValidateResponse { Applies = false }); } };
        var request = Request();

        var preflight = await Processor(eInvoicing, eReporting).ValidateAsync(request);

        order.Should().Equal("einvoicing", "ereporting");
        eInvoicing.ValidateCalls.Should().ContainSingle().Which.ReceiptRequest.Should().BeSameAs(request);
        preflight.Accepted.Should().BeTrue();
        preflight.EInvoicing.Should().Be(PostFiscalizationServiceOutcome.Applies);
        preflight.EReporting.Should().Be(PostFiscalizationServiceOutcome.NotApplicable);
    }

    [Theory]
    [InlineData(0x4752_2000_0000_1000UL, true)]  // InvoiceUnknown
    [InlineData(0x4752_2000_0000_1001UL, true)]  // B2C
    [InlineData(0x4752_2000_0000_1002UL, true)]  // B2B
    [InlineData(0x4752_2000_0000_1003UL, true)]  // B2G
    [InlineData(0x4954_0000_0000_1001UL, true)]  // IT on the legacy stack: same type nibble, no version nibble
    [InlineData(0x4752_2000_0000_0001UL, false)] // PointOfSaleReceipt
    [InlineData(0x4752_2000_0000_2011UL, false)] // DailyClosing
    [InlineData(0x4752_2000_0000_4001UL, false)] // InitialOperationReceipt
    [InlineData(0x4445_0000_0000_0001UL, false)] // DE v1 receipt case: no type nibble
    public void IsInvoiceDocument_RecognizesTheInvoiceTypeNibble(ulong receiptCase, bool expected)
    {
        PostFiscalizationProcessor.IsInvoiceDocument(new ReceiptRequest { ftReceiptCase = (ReceiptCase) receiptCase }).Should().Be(expected);
    }

    [Fact]
    public async Task ValidateAsync_SkipsEInvoicingForNonInvoiceDocuments_ButStillAsksEReporting()
    {
        var eInvoicing = FakePostFiscalizationService.Rejecting("would reject if asked");
        var eReporting = FakePostFiscalizationService.Applying();
        var request = Request();
        request.ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0001; // PointOfSaleReceipt

        var preflight = await Processor(eInvoicing, eReporting).ValidateAsync(request);

        preflight.Accepted.Should().BeTrue();
        preflight.EInvoicing.Should().Be(PostFiscalizationServiceOutcome.NotApplicable);
        preflight.EReporting.Should().Be(PostFiscalizationServiceOutcome.Applies);
        eInvoicing.ValidateCalls.Should().BeEmpty("eInvoicing is only called for invoice document types");
        eReporting.ValidateCalls.Should().ContainSingle();
    }

    [Fact]
    public async Task FinalizeAsync_NeverCallsEInvoicingForNonInvoiceDocuments()
    {
        var eInvoicing = FakePostFiscalizationService.Applying();
        var processor = Processor(eInvoicing, null);
        var queueItem = QueueItem();
        var request = Request();
        request.ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_2011; // DailyClosing

        var preflight = await processor.ValidateAsync(request);
        var finalized = await processor.FinalizeAsync(request, Fiscalized(queueItem), queueItem, preflight, []);

        eInvoicing.ValidateCalls.Should().BeEmpty();
        eInvoicing.ProcessCalls.Should().BeEmpty();
        Outcome(finalized).EInvoicing.Should().Be(PostFiscalizationStateData.NotApplicable);
    }

    [Fact]
    public async Task ValidateAsync_UsesACustomReceiptFilterWhenGiven()
    {
        var eInvoicing = FakePostFiscalizationService.Applying();
        var processor = new PostFiscalizationProcessor(NullLogger<PostFiscalizationProcessor>.Instance, eInvoicing, null, eInvoicingReceiptFilter: request => request.cbReceiptReference == "allowed");
        var allowed = Request();
        allowed.cbReceiptReference = "allowed";
        allowed.ftReceiptCase = (ReceiptCase) 0x4445_0000_0000_0001; // a v1 case an allow list may admit

        (await processor.ValidateAsync(allowed)).EInvoicing.Should().Be(PostFiscalizationServiceOutcome.Applies);
        (await processor.ValidateAsync(Request())).EInvoicing.Should().Be(PostFiscalizationServiceOutcome.NotApplicable);
        eInvoicing.ValidateCalls.Should().ContainSingle();
    }

    [Fact]
    public async Task ValidateAsync_ReportsDisabledForAMissingService()
    {
        var preflight = await Processor(null, FakePostFiscalizationService.Applying()).ValidateAsync(Request());

        preflight.Accepted.Should().BeTrue();
        preflight.EInvoicing.Should().Be(PostFiscalizationServiceOutcome.Disabled);
        preflight.EReporting.Should().Be(PostFiscalizationServiceOutcome.Applies);
    }

    [Fact]
    public async Task ValidateAsync_RejectsWithTheServicesErrors_AndSkipsLaterServices()
    {
        var eInvoicing = FakePostFiscalizationService.Rejecting("customer is missing", " ", "VAT id invalid");
        var eReporting = FakePostFiscalizationService.Applying();

        var preflight = await Processor(eInvoicing, eReporting).ValidateAsync(Request());

        preflight.Accepted.Should().BeFalse();
        preflight.EInvoicing.Should().Be(PostFiscalizationServiceOutcome.Rejected);
        preflight.EReporting.Should().Be(PostFiscalizationServiceOutcome.Skipped);
        eReporting.ValidateCalls.Should().BeEmpty();
        var rejection = preflight.Rejection!;
        rejection.Service.Should().Be(PostFiscalizationService.EInvoicing);
        rejection.Outcome.Should().Be(PostFiscalizationServiceOutcome.Rejected);
        rejection.Caption.Should().Be("einvoicing-rejected");
        rejection.Reason.Should().Be("customer is missing; VAT id invalid");
        rejection.Detail.Should().Contain("VAT id invalid");
    }

    [Fact]
    public async Task ValidateAsync_EReportingRejection_NamesEReporting()
    {
        var preflight = await Processor(FakePostFiscalizationService.Applying(), FakePostFiscalizationService.Rejecting("not reportable")).ValidateAsync(Request());

        preflight.Accepted.Should().BeFalse();
        preflight.EInvoicing.Should().Be(PostFiscalizationServiceOutcome.Applies);
        preflight.EReporting.Should().Be(PostFiscalizationServiceOutcome.Rejected);
        preflight.Rejection!.Caption.Should().Be("ereporting-rejected");
        preflight.Rejection.Reason.Should().Be("not reportable");
    }

    [Fact]
    public async Task ValidateAsync_TreatsATransportFailureLikeARejection_NamingTheCause()
    {
        var eInvoicing = FakePostFiscalizationService.FailingValidate(new PostFiscalizationServiceException("timeout after 2 attempt(s) of 15000 ms each", "https://einvoicing.example.com/v2/validate"));

        var preflight = await Processor(eInvoicing, null).ValidateAsync(Request());

        preflight.Accepted.Should().BeFalse();
        preflight.EInvoicing.Should().Be(PostFiscalizationServiceOutcome.Failed);
        preflight.EReporting.Should().Be(PostFiscalizationServiceOutcome.Disabled);
        var rejection = preflight.Rejection!;
        rejection.Outcome.Should().Be(PostFiscalizationServiceOutcome.Failed);
        rejection.Caption.Should().Be("einvoicing-rejected");
        rejection.Reason.Should().Be("eInvoicing validate call failed: timeout after 2 attempt(s) of 15000 ms each");
        rejection.Detail.Should().Be("https://einvoicing.example.com/v2/validate");
    }

    [Fact]
    public async Task ValidateAsync_TreatsAnUnexpectedExceptionAsAFailure()
    {
        var eReporting = FakePostFiscalizationService.FailingValidate(new InvalidOperationException("bug"));

        var preflight = await Processor(null, eReporting).ValidateAsync(Request());

        preflight.Accepted.Should().BeFalse();
        preflight.Rejection!.Caption.Should().Be("ereporting-rejected");
        preflight.Rejection.Reason.Should().Be("eReporting validate call failed: unexpected error: bug");
        preflight.Rejection.Detail.Should().Contain(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task ValidateAsync_TreatsANullAnswerAsAFailure()
    {
        var eInvoicing = new FakePostFiscalizationService { OnValidate = _ => Task.FromResult<ValidateResponse>(null!) };

        var preflight = await Processor(eInvoicing, null).ValidateAsync(Request());

        preflight.Accepted.Should().BeFalse();
        preflight.Rejection!.Reason.Should().Contain("returned no validation result");
    }

    [Fact]
    public async Task FinalizeAsync_SkipsServicesThatDoNotApply_AndRecordsTheOutcome()
    {
        var eInvoicing = FakePostFiscalizationService.NotApplying();
        var eReporting = FakePostFiscalizationService.NotApplying();
        var processor = Processor(eInvoicing, eReporting);
        var queueItem = QueueItem();
        var journals = new List<ftActionJournal>();

        var preflight = await processor.ValidateAsync(Request());
        var finalized = await processor.FinalizeAsync(Request(), Fiscalized(queueItem), queueItem, preflight, journals);

        eInvoicing.ProcessCalls.Should().BeEmpty();
        eReporting.ProcessCalls.Should().BeEmpty();
        finalized.ftState.Should().Be(_successState);
        finalized.ftSignatures.Should().ContainSingle(signature => signature.Caption == "invoiceMark");
        var outcome = Outcome(finalized);
        outcome.FiscalizationSucceeded.Should().BeTrue();
        outcome.EInvoicing.Should().Be(PostFiscalizationStateData.NotApplicable);
        outcome.EReporting.Should().Be(PostFiscalizationStateData.NotApplicable);
        journals.Should().BeEmpty();
    }

    [Fact]
    public async Task FinalizeAsync_UsesTheReturnedResponse_AndRecordsOk()
    {
        var eInvoicing = new FakePostFiscalizationService { OnProcess = AddingSignature("einvoice-id", "urn:peppol:1") };
        var processor = Processor(eInvoicing, null);
        var queueItem = QueueItem();
        var request = Request();
        var response = Fiscalized(queueItem);
        var journals = new List<ftActionJournal>();

        var preflight = await processor.ValidateAsync(request);
        var finalized = await processor.FinalizeAsync(request, response, queueItem, preflight, journals);

        var processRequest = eInvoicing.ProcessCalls.Should().ContainSingle().Subject;
        processRequest.ReceiptRequest.Should().BeSameAs(request);
        processRequest.ReceiptResponse.Should().BeSameAs(response);
        finalized.Should().NotBeSameAs(response);
        finalized.ftState.Should().Be(_successState);
        finalized.ftSignatures.Select(signature => signature.Caption).Should().Equal("invoiceMark", "einvoice-id");
        finalized.ftQueueItemID.Should().Be(queueItem.ftQueueItemId);
        var outcome = Outcome(finalized);
        outcome.FiscalizationSucceeded.Should().BeTrue();
        outcome.EInvoicing.Should().Be(PostFiscalizationStateData.Ok);
        outcome.EReporting.Should().Be(PostFiscalizationStateData.Disabled);
        journals.Should().BeEmpty();
    }

    [Fact]
    public async Task FinalizeAsync_ChainsServices_SoEReportingSeesWhatEInvoicingAdded()
    {
        var eInvoicing = new FakePostFiscalizationService { OnProcess = AddingSignature("einvoice-id", "urn:peppol:1") };
        var eReporting = new FakePostFiscalizationService { OnProcess = AddingSignature("ereport-id", "rep-1") };
        var processor = Processor(eInvoicing, eReporting);
        var queueItem = QueueItem();

        var preflight = await processor.ValidateAsync(Request());
        var finalized = await processor.FinalizeAsync(Request(), Fiscalized(queueItem), queueItem, preflight, []);

        eReporting.ProcessCalls.Single().ReceiptResponse.ftSignatures.Select(signature => signature.Caption).Should().Equal("invoiceMark", "einvoice-id");
        finalized.ftSignatures.Select(signature => signature.Caption).Should().Equal("invoiceMark", "einvoice-id", "ereport-id");
        var outcome = Outcome(finalized);
        outcome.EInvoicing.Should().Be(PostFiscalizationStateData.Ok);
        outcome.EReporting.Should().Be(PostFiscalizationStateData.Ok);
    }

    [Fact]
    public async Task FinalizeAsync_OnFailure_MarksTheFiscalizedReceiptAsFailed_AndStillCallsTheNextService()
    {
        var eInvoicing = FakePostFiscalizationService.FailingProcess(new PostFiscalizationServiceException("HTTP 502 Bad Gateway after 2 attempts", "<html>gateway</html>"));
        var eReporting = new FakePostFiscalizationService { OnProcess = AddingSignature("ereport-id", "rep-1") };
        var processor = Processor(eInvoicing, eReporting);
        var queueItem = QueueItem();
        var request = Request();
        var journals = new List<ftActionJournal>();

        var preflight = await processor.ValidateAsync(request);
        var finalized = await processor.FinalizeAsync(request, Fiscalized(queueItem), queueItem, preflight, journals);

        finalized.ftState.IsState(State.Error).Should().BeTrue();
        finalized.ftState.Should().Be(_successState.WithState(State.Error));
        var failure = finalized.ftSignatures.Should().ContainSingle(signature => signature.Caption == "einvoicing-failed").Subject;
        failure.Data.Should().Be("eInvoicing process call failed after fiscalization: HTTP 502 Bad Gateway after 2 attempts");
        failure.ftSignatureFormat.Should().Be(SignatureFormat.Text);
        failure.ftSignatureType.Should().Be(request.ftReceiptCase.Reset().As<SignatureType>().WithCategory(SignatureTypeCategory.Failure));
        finalized.ftSignatures.Select(signature => signature.Caption).Should().Equal("invoiceMark", "ereport-id", "einvoicing-failed");

        var handedToEReporting = eReporting.ProcessCalls.Single().ReceiptResponse;
        handedToEReporting.ftState.IsState(State.Error).Should().BeFalse("eReporting gets the response as fiscalization produced it; the eInvoicing failure is marked only after all services returned");
        handedToEReporting.ftSignatures.Select(signature => signature.Caption).Should().Equal("invoiceMark");

        var journal = journals.Should().ContainSingle().Subject;
        journal.Type.Should().Be("einvoicing-failed");
        journal.ftQueueItemId.Should().Be(queueItem.ftQueueItemId);
        journal.ftQueueId.Should().Be(_queueId);
        journal.Message.Should().Contain("HTTP 502 Bad Gateway");
        journal.DataJson.Should().Contain("<html>gateway</html>");

        var outcome = Outcome(finalized);
        outcome.FiscalizationSucceeded.Should().BeTrue();
        outcome.EInvoicing.Should().Be(PostFiscalizationStateData.Failed);
        outcome.EReporting.Should().Be(PostFiscalizationStateData.Ok);
    }

    [Fact]
    public async Task FinalizeAsync_BothServicesCanFail_TheSecondOneViaTheErrorStateChannel()
    {
        var eInvoicing = FakePostFiscalizationService.FailingProcess(new PostFiscalizationServiceException("unreachable after 2 attempt(s): Connection refused"));
        var eReporting = new FakePostFiscalizationService
        {
            OnProcess = request =>
            {
                var returned = RoundTrip(request.ReceiptResponse);
                returned.MarkAsFailed();
                returned.ftSignatures.Add(Signature("ereporting-error", "authority rejected the report"));
                return Task.FromResult(new ProcessResponse { ReceiptResponse = returned });
            },
        };
        var processor = Processor(eInvoicing, eReporting);
        var queueItem = QueueItem();
        var journals = new List<ftActionJournal>();

        var preflight = await processor.ValidateAsync(Request());
        var finalized = await processor.FinalizeAsync(Request(), Fiscalized(queueItem), queueItem, preflight, journals);

        finalized.ftState.IsState(State.Error).Should().BeTrue();
        finalized.ftSignatures.Select(signature => signature.Caption).Should().Equal("invoiceMark", "einvoicing-failed", "ereporting-failed");
        finalized.ftSignatures.Single(signature => signature.Caption == "ereporting-failed").Data.Should().Contain("reported an error state");
        journals.Select(journal => journal.Type).Should().Equal("einvoicing-failed", "ereporting-failed");
        journals.Single(journal => journal.Type == "ereporting-failed").DataJson.Should().Contain("authority rejected the report");
        var outcome = Outcome(finalized);
        outcome.FiscalizationSucceeded.Should().BeTrue();
        outcome.EInvoicing.Should().Be(PostFiscalizationStateData.Failed);
        outcome.EReporting.Should().Be(PostFiscalizationStateData.Failed);
    }

    [Fact]
    public async Task FinalizeAsync_ALaterServiceCannotUndoAnEarlierFailure()
    {
        var eInvoicing = FakePostFiscalizationService.FailingProcess(new PostFiscalizationServiceException("unreachable after 2 attempt(s): Connection refused"));
        var eReporting = new FakePostFiscalizationService
        {
            OnProcess = request => Task.FromResult(new ProcessResponse
            {
                ReceiptResponse = new ReceiptResponse
                {
                    ftQueueItemID = request.ReceiptResponse.ftQueueItemID,
                    ftQueueID = request.ReceiptResponse.ftQueueID,
                    ftQueueRow = request.ReceiptResponse.ftQueueRow,
                    ftCashBoxID = request.ReceiptResponse.ftCashBoxID,
                    cbReceiptReference = request.ReceiptResponse.cbReceiptReference,
                    cbTerminalID = request.ReceiptResponse.cbTerminalID,
                    ftCashBoxIdentification = request.ReceiptResponse.ftCashBoxIdentification,
                    ftReceiptIdentification = request.ReceiptResponse.ftReceiptIdentification,
                    ftReceiptMoment = request.ReceiptResponse.ftReceiptMoment,
                    ftState = _successState,
                    ftSignatures = [Signature("ereport-id", "rep-1")],
                },
            }),
        };
        var processor = Processor(eInvoicing, eReporting);
        var queueItem = QueueItem();
        var journals = new List<ftActionJournal>();

        var preflight = await processor.ValidateAsync(Request());
        var finalized = await processor.FinalizeAsync(Request(), Fiscalized(queueItem), queueItem, preflight, journals);

        finalized.ftState.IsState(State.Error).Should().BeTrue();
        finalized.ftSignatures.Should().Contain(signature => signature.Caption == "einvoicing-failed");
        finalized.ftSignatures.Should().Contain(signature => signature.Caption == "ereport-id");
        journals.Select(journal => journal.Type).Should().Contain("einvoicing-failed").And.Contain("ereporting-dropped-signatures");
        Outcome(finalized).EInvoicing.Should().Be(PostFiscalizationStateData.Failed);
    }

    [Fact]
    public async Task FinalizeAsync_TreatsAnErrorStateFromTheServiceAsAFailure()
    {
        var eInvoicing = new FakePostFiscalizationService
        {
            OnProcess = request =>
            {
                var returned = RoundTrip(request.ReceiptResponse);
                returned.MarkAsFailed();
                returned.ftSignatures.Add(Signature("einvoicing-error", "PEPPOL participant not registered"));
                return Task.FromResult(new ProcessResponse { ReceiptResponse = returned });
            },
        };
        var processor = Processor(eInvoicing, null);
        var queueItem = QueueItem();
        var response = Fiscalized(queueItem);
        var journals = new List<ftActionJournal>();

        var preflight = await processor.ValidateAsync(Request());
        var finalized = await processor.FinalizeAsync(Request(), response, queueItem, preflight, journals);

        finalized.Should().BeSameAs(response, "the pre-call response is used when the service signals a failure");
        finalized.ftState.IsState(State.Error).Should().BeTrue();
        finalized.ftSignatures.Select(signature => signature.Caption).Should().Equal("invoiceMark", "einvoicing-failed");
        finalized.ftSignatures.Single(signature => signature.Caption == "einvoicing-failed").Data.Should().Contain("reported an error state");
        journals.Single().DataJson.Should().Contain("PEPPOL participant not registered");
        Outcome(finalized).EInvoicing.Should().Be(PostFiscalizationStateData.Failed);
    }

    [Fact]
    public async Task FinalizeAsync_TreatsANullAnswerAsAFailure()
    {
        var eReporting = new FakePostFiscalizationService { OnProcess = _ => Task.FromResult<ProcessResponse>(null!) };
        var processor = Processor(null, eReporting);
        var queueItem = QueueItem();
        var journals = new List<ftActionJournal>();

        var preflight = await processor.ValidateAsync(Request());
        var finalized = await processor.FinalizeAsync(Request(), Fiscalized(queueItem), queueItem, preflight, journals);

        finalized.ftState.IsState(State.Error).Should().BeTrue();
        finalized.ftSignatures.Should().Contain(signature => signature.Caption == "ereporting-failed" && signature.Data.Contains("returned no receipt response"));
        journals.Single().Type.Should().Be("ereporting-failed");
    }

    [Fact]
    public async Task FinalizeAsync_TreatsAnUnexpectedExceptionAsAFailure()
    {
        var eInvoicing = FakePostFiscalizationService.FailingProcess(new NullReferenceException("bug"));
        var processor = Processor(eInvoicing, null);
        var queueItem = QueueItem();
        var journals = new List<ftActionJournal>();

        var preflight = await processor.ValidateAsync(Request());
        var finalized = await processor.FinalizeAsync(Request(), Fiscalized(queueItem), queueItem, preflight, journals);

        finalized.ftState.IsState(State.Error).Should().BeTrue();
        finalized.ftSignatures.Single(signature => signature.Caption == "einvoicing-failed").Data.Should().Contain("unexpected error: bug");
        journals.Single().DataJson.Should().Contain(nameof(NullReferenceException));
    }

    [Fact]
    public async Task FinalizeAsync_RestoresIdentifyingFields_AndJournalsTheContractViolation()
    {
        var eInvoicing = new FakePostFiscalizationService
        {
            OnProcess = request =>
            {
                var returned = RoundTrip(request.ReceiptResponse);
                returned.ftQueueItemID = Guid.NewGuid();
                returned.ftReceiptIdentification = "changed";
                returned.ftSignatures.Add(Signature("einvoice-id", "urn:1"));
                return Task.FromResult(new ProcessResponse { ReceiptResponse = returned });
            },
        };
        var processor = Processor(eInvoicing, null);
        var queueItem = QueueItem();
        var response = Fiscalized(queueItem);
        var journals = new List<ftActionJournal>();

        var preflight = await processor.ValidateAsync(Request());
        var finalized = await processor.FinalizeAsync(Request(), response, queueItem, preflight, journals);

        finalized.ftQueueItemID.Should().Be(queueItem.ftQueueItemId);
        finalized.ftReceiptIdentification.Should().Be("ft1A#");
        finalized.ftReceiptMoment.Should().Be(response.ftReceiptMoment);
        finalized.ftState.IsState(State.Error).Should().BeFalse("a contract violation is diagnosable, not fatal");
        finalized.ftSignatures.Should().Contain(signature => signature.Caption == "einvoice-id");
        var journal = journals.Should().ContainSingle().Subject;
        journal.Type.Should().Be("einvoicing-contract-violation");
        journal.Message.Should().Contain("ftQueueItemID").And.Contain("ftReceiptIdentification");
        Outcome(finalized).EInvoicing.Should().Be(PostFiscalizationStateData.Ok);
    }

    [Fact]
    public async Task FinalizeAsync_JournalsDroppedSignatures_ButKeepsTheReturnedResponse()
    {
        var eReporting = new FakePostFiscalizationService
        {
            OnProcess = request =>
            {
                var returned = RoundTrip(request.ReceiptResponse);
                returned.ftSignatures = [Signature("ereport-id", "rep-1")];
                return Task.FromResult(new ProcessResponse { ReceiptResponse = returned });
            },
        };
        var processor = Processor(null, eReporting);
        var queueItem = QueueItem();
        var journals = new List<ftActionJournal>();

        var preflight = await processor.ValidateAsync(Request());
        var finalized = await processor.FinalizeAsync(Request(), Fiscalized(queueItem), queueItem, preflight, journals);

        finalized.ftSignatures.Select(signature => signature.Caption).Should().Equal("ereport-id");
        finalized.ftState.IsState(State.Error).Should().BeFalse();
        var journal = journals.Should().ContainSingle().Subject;
        journal.Type.Should().Be("ereporting-dropped-signatures");
        journal.Message.Should().Contain("invoiceMark");
        journal.DataJson.Should().Contain("400001924190871");
    }

    [Fact]
    public async Task FinalizeAsync_MergesTheOutcomeIntoTypedStateData()
    {
        var previous = new Receipt { Request = Request(), Response = new ReceiptResponse { ftReceiptIdentification = "ft0#" } };
        var processor = Processor(FakePostFiscalizationService.Applying(), null);
        var queueItem = QueueItem();
        var response = Fiscalized(queueItem, new MiddlewareStateData { PreviousReceiptReference = [previous] });

        var preflight = await processor.ValidateAsync(Request());
        var finalized = await processor.FinalizeAsync(Request(), response, queueItem, preflight, []);

        var stateData = finalized.ftStateData.Should().BeOfType<MiddlewareStateData>().Subject;
        stateData.PreviousReceiptReference.Should().ContainSingle().Which.Response.ftReceiptIdentification.Should().Be("ft0#");
        stateData.PostFiscalization!.EInvoicing.Should().Be(PostFiscalizationStateData.Ok);
    }

    [Fact]
    public async Task FinalizeAsync_MergesTheOutcomeIntoMarketSpecificStateData_KeepingTheSubclass()
    {
        var processor = Processor(FakePostFiscalizationService.Applying(), null);
        var queueItem = QueueItem();
        var response = Fiscalized(queueItem, new MarketStateData { Market = "ES" });

        var preflight = await processor.ValidateAsync(Request());
        var finalized = await processor.FinalizeAsync(Request(), response, queueItem, preflight, []);

        var stateData = finalized.ftStateData.Should().BeOfType<MarketStateData>().Subject;
        stateData.Market.Should().Be("ES");
        stateData.PostFiscalization!.FiscalizationSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task FinalizeAsync_MergesTheOutcomeIntoJsonElementStateData_FromTheWire()
    {
        var eInvoicing = new FakePostFiscalizationService { OnProcess = AddingSignature("einvoice-id", "urn:1") };
        var processor = Processor(eInvoicing, null);
        var queueItem = QueueItem();
        var response = Fiscalized(queueItem, JsonSerializer.Deserialize<JsonElement>("""{ "ES": { "LastReceipt": null, "GovernmentAPI": { "Version": "1" } }, "ftPreviousReceiptReference": [] }"""));

        var preflight = await processor.ValidateAsync(Request());
        var finalized = await processor.FinalizeAsync(Request(), response, queueItem, preflight, []);

        var stateData = finalized.ftStateData.Should().BeOfType<MiddlewareStateData>().Subject;
        stateData.ExtraData.Should().ContainKey("ES");
        stateData.PostFiscalization!.EInvoicing.Should().Be(PostFiscalizationStateData.Ok);
        var persisted = JsonSerializer.Serialize(finalized);
        using var document = JsonDocument.Parse(persisted);
        var persistedStateData = document.RootElement.GetProperty("ftStateData");
        persistedStateData.GetProperty("ES").GetProperty("GovernmentAPI").GetProperty("Version").GetString().Should().Be("1");
        persistedStateData.GetProperty("PostFiscalization").GetProperty("FiscalizationSucceeded").GetBoolean().Should().BeTrue();
        persistedStateData.GetProperty("PostFiscalization").GetProperty("EInvoicing").GetString().Should().Be("ok");
        persistedStateData.GetProperty("PostFiscalization").GetProperty("EReporting").GetString().Should().Be("disabled");
    }

    [Fact]
    public async Task FinalizeAsync_WrapsUninterpretableStateData_SoTheFiscalizationMarkerSurvivesAFailure()
    {
        var eInvoicing = FakePostFiscalizationService.FailingProcess(new PostFiscalizationServiceException("HTTP 500 Internal Server Error after 2 attempts"));
        var processor = Processor(eInvoicing, null);
        var queueItem = QueueItem();
        var response = Fiscalized(queueItem, JsonSerializer.Deserialize<JsonElement>("\"just a string\""));
        var journals = new List<ftActionJournal>();

        var preflight = await processor.ValidateAsync(Request());
        var finalized = await processor.FinalizeAsync(Request(), response, queueItem, preflight, journals);

        finalized.ftState.IsState(State.Error).Should().BeTrue();
        var stateData = finalized.ftStateData.Should().BeOfType<MiddlewareStateData>().Subject;
        stateData.PostFiscalization!.FiscalizationSucceeded.Should().BeTrue();
        stateData.PostFiscalization.EInvoicing.Should().Be(PostFiscalizationStateData.Failed);
        stateData.ExtraData[PostFiscalizationProcessor.OriginalStateDataKey].GetString().Should().Be("just a string");
        journals.Select(journal => journal.Type).Should().Equal("einvoicing-failed", "postfiscalization-statedata");

        var persisted = RoundTrip(finalized);
        persisted.IsFiscalized().Should().BeTrue("a finalize-failed receipt must stay referenceable even when its state data could not be merged");
    }

    [Fact]
    public async Task FinalizeAsync_RestoresStateDataAServiceReplacedWithANonObject()
    {
        var previous = new Receipt { Request = Request(), Response = new ReceiptResponse { ftReceiptIdentification = "ft0#" } };
        var eInvoicing = new FakePostFiscalizationService
        {
            OnProcess = request =>
            {
                var returned = RoundTrip(request.ReceiptResponse);
                returned.ftStateData = JsonSerializer.Deserialize<JsonElement>("[1, 2, 3]");
                returned.ftSignatures.Add(Signature("einvoice-id", "urn:1"));
                return Task.FromResult(new ProcessResponse { ReceiptResponse = returned });
            },
        };
        var processor = Processor(eInvoicing, null);
        var queueItem = QueueItem();
        var originalStateData = new MiddlewareStateData { PreviousReceiptReference = [previous] };
        var journals = new List<ftActionJournal>();

        var preflight = await processor.ValidateAsync(Request());
        var finalized = await processor.FinalizeAsync(Request(), Fiscalized(queueItem, originalStateData), queueItem, preflight, journals);

        finalized.ftState.IsState(State.Error).Should().BeFalse();
        finalized.ftSignatures.Should().Contain(signature => signature.Caption == "einvoice-id");
        finalized.ftStateData.Should().BeSameAs(originalStateData);
        originalStateData.PreviousReceiptReference.Should().ContainSingle();
        originalStateData.PostFiscalization!.EInvoicing.Should().Be(PostFiscalizationStateData.Ok);
        var journal = journals.Should().ContainSingle().Subject;
        journal.Type.Should().Be("einvoicing-contract-violation");
        journal.Message.Should().Contain("not a JSON object");
    }

    [Fact]
    public async Task FinalizeAsync_RestoresStateDataAServiceDropped()
    {
        var eReporting = new FakePostFiscalizationService
        {
            OnProcess = request =>
            {
                var returned = RoundTrip(request.ReceiptResponse);
                returned.ftStateData = null;
                return Task.FromResult(new ProcessResponse { ReceiptResponse = returned });
            },
        };
        var processor = Processor(null, eReporting);
        var queueItem = QueueItem();
        var journals = new List<ftActionJournal>();

        var preflight = await processor.ValidateAsync(Request());
        var finalized = await processor.FinalizeAsync(Request(), Fiscalized(queueItem, new MarketStateData { Market = "PT" }), queueItem, preflight, journals);

        var stateData = finalized.ftStateData.Should().BeOfType<MarketStateData>().Subject;
        stateData.Market.Should().Be("PT");
        stateData.PostFiscalization!.EReporting.Should().Be(PostFiscalizationStateData.Ok);
        journals.Should().ContainSingle().Which.Type.Should().Be("ereporting-contract-violation");
    }

    [Fact]
    public async Task ActivityTags_ReflectTheOutcomes()
    {
        var eInvoicing = new FakePostFiscalizationService { OnProcess = AddingSignature("einvoice-id", "urn:1") };
        var processor = Processor(eInvoicing, FakePostFiscalizationService.NotApplying());
        var queueItem = QueueItem();
        using var activity = new Activity("sign").Start();

        var preflight = await processor.ValidateAsync(Request());
        activity.GetTagItem(PostFiscalizationProcessor.EInvoicingActivityTag).Should().Be("applies");
        activity.GetTagItem(PostFiscalizationProcessor.EReportingActivityTag).Should().Be("not-applicable");

        await processor.FinalizeAsync(Request(), Fiscalized(queueItem), queueItem, preflight, []);
        activity.GetTagItem(PostFiscalizationProcessor.EInvoicingActivityTag).Should().Be("ok");
        activity.GetTagItem(PostFiscalizationProcessor.EReportingActivityTag).Should().Be("not-applicable");
    }

    [Fact]
    public async Task ActivityTags_DistinguishRejectionsFromOutages()
    {
        using (var activity = new Activity("sign").Start())
        {
            await Processor(FakePostFiscalizationService.Rejecting("nope"), null).ValidateAsync(Request());
            activity.GetTagItem(PostFiscalizationProcessor.EInvoicingActivityTag).Should().Be("rejected");
            activity.GetTagItem(PostFiscalizationProcessor.EReportingActivityTag).Should().Be("disabled");
        }

        using (var activity = new Activity("sign").Start())
        {
            await Processor(FakePostFiscalizationService.FailingValidate(new PostFiscalizationServiceException("timeout")), FakePostFiscalizationService.Applying()).ValidateAsync(Request());
            activity.GetTagItem(PostFiscalizationProcessor.EInvoicingActivityTag).Should().Be("failed");
            activity.GetTagItem(PostFiscalizationProcessor.EReportingActivityTag).Should().Be("skipped");
        }
    }
}
