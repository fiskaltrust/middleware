using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.Middleware.Localization.QueueIT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.Processors;
using fiskaltrust.Middleware.Localization.QueueIT.SCU;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.Processors;

public class ReceiptCommandProcessorITTests
{
    private static readonly DateTime _documentMoment = new(2024, 10, 14, 8, 55, 45);

    private readonly ftQueue _queue = TestHelpers.CreateQueue();
    private readonly ftQueueItem _queueItem;
    private readonly ftQueueIT _queueIT;
    private readonly Mock<IConfigurationRepository> _configurationRepository;
    private readonly Mock<IMiddlewareJournalITRepository> _journalITRepository = TestHelpers.CreateJournalITRepository();

    public ReceiptCommandProcessorITTests()
    {
        _queueItem = TestHelpers.CreateQueueItem(_queue);
        _queueIT = TestHelpers.CreateQueueIT(_queue);
        _configurationRepository = TestHelpers.CreateConfigurationRepository(_queueIT);
    }

    private ReceiptCommandProcessorIT CreateSut(IITSSCDProvider sscd) => new(sscd, TestHelpers.Lazy(_journalITRepository.Object), TestHelpers.Lazy(_configurationRepository.Object));

    private ReceiptProcessor CreateReceiptProcessor(IITSSCDProvider sscd) => new(Mock.Of<ILogger<ReceiptProcessor>>(), Mock.Of<IMarketValidator>(), null!, CreateSut(sscd), null!, null!, null!);

    private static Action<ProcessRequest> SignsDocument(long zNumber, long documentNumber, string documentType = "POSRECEIPT")
        => request => request.ReceiptResponse.ftSignatures.AddRange(TestHelpers.CreateRTSignatures(zNumber, documentNumber, _documentMoment, documentType));

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    public async Task PointOfSaleReceipt_TakesTheRTNumbering_AddsThePrintSignatures_AndJournalsTheDocument(ReceiptCase receiptCase)
    {
        var sscd = TestHelpers.CreateSscd(SignsDocument(zNumber: 1, documentNumber: 2));
        var request = TestHelpers.CreateRequest(receiptCase);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateReceiptProcessor(sscd.Object).ProcessAsync(request, response, _queue, _queueItem);

        using var scope = new AssertionScope();
        result.receiptResponse.State().Should().Be(TestHelpers.BaseState);
        result.receiptResponse.ftReceiptIdentification.Should().Be("ft1#0001-0002");
        result.receiptResponse.ftSignatures[0].Caption.Should().Be("[www.fiskaltrust.it]");
        result.receiptResponse.ftSignatures[0].Data.Should().Contain("DOCUMENTO N. 0001-0002").And.Contain($"Cassa {TestHelpers.CashBoxIdentification}");
        ((ulong) result.receiptResponse.ftSignatures[0].ftSignatureType).Should().Be(0x4954_2000_0000_0001);
        result.receiptResponse.ftSignatures[1].Caption.Should().Be("DOCUMENTO COMMERCIALE");
        result.receiptResponse.ftSignatures[1].Data.Should().Be("di vendita o prestazione");
        ((ulong) result.receiptResponse.ftSignatures[1].ftSignatureType).Should().Be(0x4954_2000_0000_0002);
        result.receiptResponse.GetSignatureItem(SignatureTypeIT.RTDocumentNumber)!.Data.Should().Be("0002");
        result.actionJournals.Should().BeEmpty();

        _journalITRepository.Verify(x => x.InsertAsync(It.Is<ftJournalIT>(j =>
            j.ftQueueId == _queue.ftQueueId
            && j.ftQueueItemId == _queueItem.ftQueueItemId
            && j.cbReceiptReference == request.cbReceiptReference
            && j.ftSignaturCreationUnitITId == _queueIT.ftSignaturCreationUnitITId!.Value
            && j.JournalType == (long) receiptCase
            && j.ReceiptNumber == 2
            && j.ZRepNumber == 1)), Times.Once);
    }

    [Fact]
    public async Task PointOfSaleReceipt_WhenTheScuNumbersTheDocumentItself_KeepsItsIdentification()
    {
        var sscd = TestHelpers.CreateSscd(request =>
        {
            SignsDocument(zNumber: 3, documentNumber: 4)(request);
            request.ReceiptResponse.ftReceiptIdentification = "RT-0003-0004";
        });
        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateSut(sscd.Object).PointOfSaleReceipt0x0001Async(new ProcessCommandRequest(_queue, request, response));

        result.receiptResponse.ftReceiptIdentification.Should().Be("RT-0003-0004");
    }

    [Fact]
    public async Task PointOfSaleReceipt_WhenTheScuFails_ReturnsTheFailure_WithoutJournal()
    {
        var sscd = TestHelpers.CreateSscd(request => request.ReceiptResponse.SetReceiptResponseError("Printer offline"));
        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateSut(sscd.Object).PointOfSaleReceipt0x0001Async(new ProcessCommandRequest(_queue, request, response));

        result.receiptResponse.State().Should().Be(0x4954_2000_EEEE_EEEE);
        result.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE" && x.Data == "Printer offline");
        _journalITRepository.Verify(x => x.InsertAsync(It.IsAny<ftJournalIT>()), Times.Never);
    }

    [Fact]
    public async Task PointOfSaleReceipt_WithoutRTNumberingInTheScuResponse_Fails()
    {
        var sscd = TestHelpers.CreateSscd();
        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateSut(sscd.Object).PointOfSaleReceipt0x0001Async(new ProcessCommandRequest(_queue, request, response));

        result.receiptResponse.State().Should().Be(0x4954_2000_EEEE_EEEE);
        result.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE" && x.Data.Contains("RT document number"));
        _journalITRepository.Verify(x => x.InsertAsync(It.IsAny<ftJournalIT>()), Times.Never);
    }

    [Theory]
    [InlineData((ulong) ReceiptCaseFlags.Refund, "REFUND", "emesso per RESO MERCE")]
    [InlineData((ulong) ReceiptCaseFlags.Void, "VOID", "emesso per ANNULLAMENTO")]
    public async Task RefundOrVoid_WithReference_HandsTheReferencedRTDocumentToTheScu(ulong flag, string documentType, string headerLine)
    {
        ProcessRequest? scuRequest = null;
        var sscd = TestHelpers.CreateSscd(request =>
        {
            scuRequest = request;
            SignsDocument(zNumber: 2, documentNumber: 1, documentType)(request);
        });
        var referencedRequest = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001);
        var referencedResponse = TestHelpers.CreateResponse(_queue, TestHelpers.CreateQueueItem(_queue), referencedRequest);
        referencedResponse.ftSignatures.AddRange(TestHelpers.CreateRTSignatures(zNumber: 344, documentNumber: 1239, _documentMoment));

        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001, flag, referencedRequest.cbReceiptReference);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);
        TestHelpers.SetPreviousReceipt(response, (referencedRequest, referencedResponse));

        var result = await CreateSut(sscd.Object).PointOfSaleReceipt0x0001Async(new ProcessCommandRequest(_queue, request, response));

        using var scope = new AssertionScope();
        scuRequest.Should().NotBeNull();
        scuRequest!.ReceiptResponse.GetSignatureItem(SignatureTypeIT.RTReferenceZNumber)!.Data.Should().Be("0344");
        scuRequest.ReceiptResponse.GetSignatureItem(SignatureTypeIT.RTReferenceDocumentNumber)!.Data.Should().Be("1239");
        scuRequest.ReceiptResponse.GetSignatureItem(SignatureTypeIT.RTReferenceDocumentMoment)!.Data.Should().Be("2024-10-14 08:55:45");
        result.receiptResponse.State().Should().Be(TestHelpers.BaseState);
        result.receiptResponse.ftReceiptIdentification.Should().Be("ft1#0002-0001");
        result.receiptResponse.ftSignatures[1].Data.Should().Contain(headerLine).And.Contain("N. 0344-1239 del 14-10-2024");
        result.receiptResponse.ftStateData.Should().BeSameAs(response.ftStateData, "the resolved references stay on the response");
    }

    [Fact]
    public async Task Refund_WithAReferenceTheQueueCouldNotResolve_Fails_WithoutCallingTheScu()
    {
        var sscd = new Mock<IITSSCDProvider>(MockBehavior.Strict);
        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001, (ulong) ReceiptCaseFlags.Refund, "unknown");
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateSut(sscd.Object).PointOfSaleReceipt0x0001Async(new ProcessCommandRequest(_queue, request, response));

        result.receiptResponse.State().Should().Be(0x4954_2000_EEEE_EEEE);
        result.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE" && x.Data == "There is no item available with the given cbPreviousReceiptReference 'unknown'.");
    }

    [Fact]
    public async Task Refund_WithoutReference_IsHandedToTheScuUnreferenced()
    {
        ProcessRequest? scuRequest = null;
        var sscd = TestHelpers.CreateSscd(request =>
        {
            scuRequest = request;
            SignsDocument(zNumber: 8, documentNumber: 3, "REFUND")(request);
        });
        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001, (ulong) ReceiptCaseFlags.Refund);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateSut(sscd.Object).PointOfSaleReceipt0x0001Async(new ProcessCommandRequest(_queue, request, response));

        using var scope = new AssertionScope();
        scuRequest!.ReceiptResponse.GetSignatureItem(SignatureTypeIT.RTReferenceDocumentNumber).Should().BeNull();
        result.receiptResponse.State().Should().Be(TestHelpers.BaseState);
        result.receiptResponse.ftSignatures[1].Data.Should().Contain("emesso per RESO MERCE").And.Contain("ND").And.NotContain("del ");
    }

    [Theory]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    [InlineData(ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003)]
    [InlineData(ReceiptCase.ECommerce0x0004)]
    public async Task OtherReceipts_AreStoredWithoutTheScu(ReceiptCase receiptCase)
    {
        var sscd = new Mock<IITSSCDProvider>(MockBehavior.Strict);
        var request = TestHelpers.CreateRequest(receiptCase);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateReceiptProcessor(sscd.Object).ProcessAsync(request, response, _queue, _queueItem);

        result.receiptResponse.Should().BeSameAs(response);
        result.receiptResponse.State().Should().Be(TestHelpers.BaseState);
        result.receiptResponse.ftSignatures.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0x0006)]
    [InlineData(0x0007)]
    public async Task TableCheckAndProForma_AreNotSupported(ulong receiptCase)
    {
        var sscd = new Mock<IITSSCDProvider>(MockBehavior.Strict);
        var request = TestHelpers.CreateRequest((ReceiptCase) receiptCase);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateReceiptProcessor(sscd.Object).ProcessAsync(request, response, _queue, _queueItem);

        result.receiptResponse.State().Should().Be(0x4954_2000_EEEE_EEEE);
        result.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE" && x.Data.Contains("is not supported in the QueueIT implementation"));
    }
}
