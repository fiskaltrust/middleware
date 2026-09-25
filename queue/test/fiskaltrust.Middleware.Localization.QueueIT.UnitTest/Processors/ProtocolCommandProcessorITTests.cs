using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.Middleware.Localization.QueueIT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.Processors;
using fiskaltrust.Middleware.Localization.QueueIT.SCU;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.Processors;

public class ProtocolCommandProcessorITTests
{
    private readonly ftQueue _queue = TestHelpers.CreateQueue();
    private readonly ftQueueItem _queueItem;

    public ProtocolCommandProcessorITTests()
    {
        _queueItem = TestHelpers.CreateQueueItem(_queue);
    }

    private static ReceiptProcessor CreateReceiptProcessor(IITSSCDProvider sscd) => new(Mock.Of<ILogger<ReceiptProcessor>>(), Mock.Of<IMarketValidator>(), null!, null!, null!, null!, new ProtocolCommandProcessorIT(sscd));

    [Fact]
    public async Task CopyReceipt_WithoutReference_Fails()
    {
        var sscd = new Mock<IITSSCDProvider>(MockBehavior.Strict);
        var request = TestHelpers.CreateRequest(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await new ProtocolCommandProcessorIT(sscd.Object).CopyReceiptPrintExistingReceipt0x3010Async(new ProcessCommandRequest(_queue, request, response));

        result.receiptResponse.State().Should().Be(0x4954_2000_EEEE_EEEE);
        result.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE" && x.Data.Contains("requires the cbPreviousReceiptReference"));
    }

    [Fact]
    public async Task CopyReceipt_WithAReferenceTheQueueCouldNotResolve_Fails()
    {
        var sscd = new Mock<IITSSCDProvider>(MockBehavior.Strict);
        var request = TestHelpers.CreateRequest(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010, cbPreviousReceiptReference: "unknown");
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await new ProtocolCommandProcessorIT(sscd.Object).CopyReceiptPrintExistingReceipt0x3010Async(new ProcessCommandRequest(_queue, request, response));

        result.receiptResponse.State().Should().Be(0x4954_2000_EEEE_EEEE);
        result.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE" && x.Data == "There is no item available with the given cbPreviousReceiptReference 'unknown'.");
    }

    [Fact]
    public async Task CopyReceipt_WithReference_HandsTheReferencedRTDocumentToTheScu()
    {
        ProcessRequest? scuRequest = null;
        var sscd = TestHelpers.CreateSscd(request => scuRequest = request);
        var referencedRequest = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001);
        var referencedResponse = TestHelpers.CreateResponse(_queue, TestHelpers.CreateQueueItem(_queue), referencedRequest);
        referencedResponse.ftSignatures.AddRange(TestHelpers.CreateRTSignatures(zNumber: 344, documentNumber: 1239, new DateTime(2024, 10, 14, 8, 55, 45)));

        var request = TestHelpers.CreateRequest(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010, cbPreviousReceiptReference: referencedRequest.cbReceiptReference);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);
        TestHelpers.SetPreviousReceipt(response, (referencedRequest, referencedResponse));

        var result = await new ProtocolCommandProcessorIT(sscd.Object).CopyReceiptPrintExistingReceipt0x3010Async(new ProcessCommandRequest(_queue, request, response));

        using var scope = new AssertionScope();
        result.receiptResponse.State().Should().Be(TestHelpers.BaseState);
        scuRequest!.ReceiptResponse.GetSignatureItem(SignatureTypeIT.RTReferenceZNumber)!.Data.Should().Be("0344");
        scuRequest.ReceiptResponse.GetSignatureItem(SignatureTypeIT.RTReferenceDocumentNumber)!.Data.Should().Be("1239");
        scuRequest.ReceiptResponse.GetSignatureItem(SignatureTypeIT.RTReferenceDocumentMoment)!.Data.Should().Be("2024-10-14 08:55:45");
        result.receiptResponse.GetSignatureItem(SignatureTypeIT.RTReferenceDocumentNumber)!.Data.Should().Be("1239");
    }

    [Fact]
    public async Task ProtocolUnspecified_WithTheNonFiscalPrintFlag_IsPrintedByTheScu()
    {
        ProcessRequest? scuRequest = null;
        var sscd = TestHelpers.CreateSscd(request => scuRequest = request);
        var request = TestHelpers.CreateRequest(ReceiptCase.ProtocolUnspecified0x3000, (ulong) ReceiptCaseFlagsIT.NonFiscalPrint);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateReceiptProcessor(sscd.Object).ProcessAsync(request, response, _queue, _queueItem);

        scuRequest.Should().NotBeNull();
        result.receiptResponse.State().Should().Be(TestHelpers.BaseState);
    }

    [Fact]
    public async Task ProtocolUnspecified_WithoutTheNonFiscalPrintFlag_IsOnlyStored()
    {
        var sscd = new Mock<IITSSCDProvider>(MockBehavior.Strict);
        var request = TestHelpers.CreateRequest(ReceiptCase.ProtocolUnspecified0x3000);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateReceiptProcessor(sscd.Object).ProcessAsync(request, response, _queue, _queueItem);

        result.receiptResponse.Should().BeSameAs(response);
        result.receiptResponse.State().Should().Be(TestHelpers.BaseState);
    }

    [Fact]
    public async Task ProtocolUnspecified_WhenTheScuThrows_ReportsTheFailure()
    {
        var sscd = new Mock<IITSSCDProvider>(MockBehavior.Strict);
        sscd.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>())).ThrowsAsync(new TimeoutException("RT printer did not answer"));
        var request = TestHelpers.CreateRequest(ReceiptCase.ProtocolUnspecified0x3000, (ulong) ReceiptCaseFlagsIT.NonFiscalPrint);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await new ProtocolCommandProcessorIT(sscd.Object).ProtocolUnspecified0x3000Async(new ProcessCommandRequest(_queue, request, response));

        result.receiptResponse.State().Should().Be(0x4954_2000_EEEE_EEEE);
        result.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE" && x.Data == "RT printer did not answer");
    }

    [Theory]
    [InlineData(ReceiptCase.ProtocolTechnicalEvent0x3001)]
    [InlineData(ReceiptCase.ProtocolAccountingEvent0x3002)]
    [InlineData(ReceiptCase.InternalUsageMaterialConsumption0x3003)]
    [InlineData(ReceiptCase.Order0x3004)]
    public async Task OtherProtocolReceipts_AreOnlyStored(ReceiptCase receiptCase)
    {
        var sscd = new Mock<IITSSCDProvider>(MockBehavior.Strict);
        var request = TestHelpers.CreateRequest(receiptCase);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateReceiptProcessor(sscd.Object).ProcessAsync(request, response, _queue, _queueItem);

        result.receiptResponse.Should().BeSameAs(response);
        result.receiptResponse.State().Should().Be(TestHelpers.BaseState);
        result.actionJournals.Should().BeEmpty();
    }

    [Fact]
    public async Task Pay_IsNotSupported()
    {
        var sscd = new Mock<IITSSCDProvider>(MockBehavior.Strict);
        var request = TestHelpers.CreateRequest(ReceiptCase.Pay0x3005);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateReceiptProcessor(sscd.Object).ProcessAsync(request, response, _queue, _queueItem);

        result.receiptResponse.State().Should().Be(0x4954_2000_EEEE_EEEE);
        result.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE" && x.Data.Contains("Pay - 0x3005 is not supported"));
    }
}
