using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.Middleware.Localization.QueueIT.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Interface;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.Helpers;

public class ReceiptReferencesTests
{
    private readonly ftQueueFixture _fixture = new();

    private class ftQueueFixture
    {
        public storage.V0.ftQueue Queue { get; } = TestHelpers.CreateQueue();
        public storage.V0.ftQueueItem QueueItem { get; }
        public ftQueueFixture() { QueueItem = TestHelpers.CreateQueueItem(Queue); }
    }

    private (ReceiptRequest request, ReceiptResponse response) CreateReferencedReceipt(bool withRTSignatures = true, bool failed = false)
    {
        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001);
        var response = TestHelpers.CreateResponse(_fixture.Queue, TestHelpers.CreateQueueItem(_fixture.Queue), request);
        if (withRTSignatures)
        {
            response.ftSignatures.AddRange(TestHelpers.CreateRTSignatures(zNumber: 344, documentNumber: 1239, new DateTime(2024, 10, 14, 8, 55, 45)));
        }
        if (failed)
        {
            response.SetReceiptResponseError("failed");
        }
        return (request, response);
    }

    [Fact]
    public void WithoutReference_NothingIsAdded()
    {
        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001, (ulong) ReceiptCaseFlags.Refund);
        var response = TestHelpers.CreateResponse(_fixture.Queue, _fixture.QueueItem, request);

        ReceiptReferences.TryAddReferenceSignatures(request, response).Should().BeTrue();

        response.ftSignatures.Should().BeEmpty();
        response.State().Should().Be(TestHelpers.BaseState);
    }

    [Fact]
    public void WithoutReference_WhenOneIsRequired_Fails()
    {
        var request = TestHelpers.CreateRequest(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010);
        var response = TestHelpers.CreateResponse(_fixture.Queue, _fixture.QueueItem, request);

        ReceiptReferences.TryAddReferenceSignatures(request, response, required: true).Should().BeFalse();

        response.State().Should().Be(0x4954_2000_EEEE_EEEE);
    }

    [Fact]
    public void WithReference_ButNothingResolved_Fails()
    {
        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001, (ulong) ReceiptCaseFlags.Refund, "missing");
        var response = TestHelpers.CreateResponse(_fixture.Queue, _fixture.QueueItem, request);

        ReceiptReferences.TryAddReferenceSignatures(request, response).Should().BeFalse();

        response.State().Should().Be(0x4954_2000_EEEE_EEEE);
        response.ftSignatures.Should().ContainSingle(x => x.Data == "There is no item available with the given cbPreviousReceiptReference 'missing'.");
    }

    [Fact]
    public void WithResolvedReference_AddsTheRTIdentificationAsReferenceSignatures()
    {
        var referenced = CreateReferencedReceipt();
        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001, (ulong) ReceiptCaseFlags.Refund, referenced.request.cbReceiptReference);
        var response = TestHelpers.CreateResponse(_fixture.Queue, _fixture.QueueItem, request);
        TestHelpers.SetPreviousReceipt(response, referenced);

        ReceiptReferences.TryAddReferenceSignatures(request, response).Should().BeTrue();

        using var scope = new AssertionScope();
        response.ftSignatures.Should().HaveCount(3);
        response.GetSignatureItem(SignatureTypeIT.RTReferenceZNumber)!.Data.Should().Be("0344");
        response.GetSignatureItem(SignatureTypeIT.RTReferenceDocumentNumber)!.Data.Should().Be("1239");
        response.GetSignatureItem(SignatureTypeIT.RTReferenceDocumentMoment)!.Data.Should().Be("2024-10-14 08:55:45");
        response.ftSignatures.Should().OnlyContain(x => x.ftSignatureFormat == SignatureFormat.Text);
        response.State().Should().Be(TestHelpers.BaseState);
    }

    [Fact]
    public void WithResolvedReference_WithoutRTIdentification_LeavesTheResponseUnchanged()
    {
        var referenced = CreateReferencedReceipt(withRTSignatures: false);
        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001, (ulong) ReceiptCaseFlags.Refund, referenced.request.cbReceiptReference);
        var response = TestHelpers.CreateResponse(_fixture.Queue, _fixture.QueueItem, request);
        TestHelpers.SetPreviousReceipt(response, referenced);

        ReceiptReferences.TryAddReferenceSignatures(request, response).Should().BeTrue();

        response.ftSignatures.Should().BeEmpty();
        response.State().Should().Be(TestHelpers.BaseState);
    }

    [Fact]
    public void WithSeveralResolvedReferences_UsesTheFirstSuccessfulOne()
    {
        var failed = CreateReferencedReceipt(failed: true);
        var succeeded = CreateReferencedReceipt();
        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001, (ulong) ReceiptCaseFlags.Void, succeeded.request.cbReceiptReference);
        var response = TestHelpers.CreateResponse(_fixture.Queue, _fixture.QueueItem, request);
        TestHelpers.SetPreviousReceipt(response, failed, succeeded);

        ReceiptReferences.TryAddReferenceSignatures(request, response).Should().BeTrue();

        response.GetSignatureItem(SignatureTypeIT.RTReferenceDocumentNumber)!.Data.Should().Be("1239");
    }
}
