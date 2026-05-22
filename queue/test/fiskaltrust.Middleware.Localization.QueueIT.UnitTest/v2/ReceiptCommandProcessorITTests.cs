using System.Text.Json;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.v2;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.v2;

public class ReceiptCommandProcessorITTests
{
    private static readonly ReceiptCase PointOfSaleVoid =
        (ReceiptCase) (0x4954_2000_0000_0000 | (long) ReceiptCases.PointOfSaleReceipt0x0001 | (long) ReceiptCaseFlags.Void);

    private static readonly ReceiptCase PointOfSaleRefund =
        (ReceiptCase) (0x4954_2000_0000_0000 | (long) ReceiptCases.PointOfSaleReceipt0x0001 | (long) ReceiptCaseFlags.Refund);

    private static SignatureItem MakeSig(string caption, string data, SignatureTypesIT type) => new()
    {
        Caption = caption,
        Data = data,
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = (SignatureType) (Cases.BASE_STATE | (long) type),
    };

    [Fact]
    public async Task PointOfSaleReceipt0x0001Async_Void_ShouldReturn_EEEE_Tag_IfReceiptReference_IsNotAvailable()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();

        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null)).Returns(new List<ftQueueItem>().ToAsyncEnumerable());

        var processor = new ReceiptCommandProcessorIT(
            Mock.Of<IITSSCD>(MockBehavior.Strict),
            Mock.Of<IJournalITRepository>(MockBehavior.Strict),
            queueItemRepositoryMock.Object,
            new ftQueueIT());

        var request = new ProcessCommandRequest(new ftQueue(), new ReceiptRequest
        {
            ftReceiptCase = PointOfSaleVoid,
            cbPreviousReceiptReference = cbPreviousReceiptReference,
        }, new ReceiptResponse());

        var result = await processor.PointOfSaleReceipt0x0001Async(request);

        result.receiptResponse.ftState.IsState(State.Error).Should().BeTrue();
    }

    [Fact]
    public async Task PointOfSaleReceipt0x0001Async_Refund_ShouldReturn_EEEE_Tag_IfReceiptReference_IsNotAvailable()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();

        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null)).Returns(new List<ftQueueItem>().ToAsyncEnumerable());

        var processor = new ReceiptCommandProcessorIT(
            Mock.Of<IITSSCD>(MockBehavior.Strict),
            Mock.Of<IJournalITRepository>(MockBehavior.Strict),
            queueItemRepositoryMock.Object,
            new ftQueueIT());

        var request = new ProcessCommandRequest(new ftQueue(), new ReceiptRequest
        {
            ftReceiptCase = PointOfSaleRefund,
            cbPreviousReceiptReference = cbPreviousReceiptReference,
        }, new ReceiptResponse());

        var result = await processor.PointOfSaleReceipt0x0001Async(request);

        result.receiptResponse.ftState.IsState(State.Error).Should().BeTrue();
    }

    [Fact]
    public async Task PointOfSaleReceipt0x0001Async_Refund_ShouldReturn_ReferenceSignatures_IfLoadedReceipt_ContainsThem()
    {
        await AssertReferenceSignaturesAppended(PointOfSaleRefund);
    }

    [Fact]
    public async Task PointOfSaleReceipt0x0001Async_Void_ShouldReturn_ReferenceSignatures_IfLoadedReceipt_ContainsThem()
    {
        await AssertReferenceSignaturesAppended(PointOfSaleVoid);
    }

    private static async Task AssertReferenceSignaturesAppended(ReceiptCase ftReceiptCase)
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();
        var signatures = new List<SignatureItem>
        {
            MakeSig("<doc-number>", "1239", SignatureTypesIT.RTDocumentNumber),
            MakeSig("<z-number>", "344", SignatureTypesIT.RTZNumber),
            MakeSig("<timestamp>", "2024-10-14 08:55:45", SignatureTypesIT.RTDocumentMoment),
            MakeSig("<rt-document-type>", "POSRECEIPT", SignatureTypesIT.RTDocumentType),
        };

        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null)).Returns(new List<ftQueueItem>
        {
            new() { response = JsonSerializer.Serialize(new ReceiptResponse { ftSignatures = signatures }) },
        }.ToAsyncEnumerable());

        var itSSCDMock = new Mock<IITSSCD>(MockBehavior.Strict);
        itSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>())).ReturnsAsync((ProcessRequest r) => new ProcessResponse { ReceiptResponse = r.ReceiptResponse });

        var processor = new ReceiptCommandProcessorIT(
            itSSCDMock.Object,
            Mock.Of<IJournalITRepository>(),
            queueItemRepositoryMock.Object,
            new ftQueueIT { ftSignaturCreationUnitITId = Guid.NewGuid() });

        var request = new ProcessCommandRequest(new ftQueue(), new ReceiptRequest
        {
            ftReceiptCase = ftReceiptCase,
            cbPreviousReceiptReference = cbPreviousReceiptReference,
        }, new ReceiptResponse
        {
            ftSignatures = new List<SignatureItem>(signatures),
            ftReceiptIdentification = "ft1#",
        });

        var result = await processor.PointOfSaleReceipt0x0001Async(request);

        result.receiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        result.receiptResponse.ftSignatures.Should().Contain(x => (long) x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber) && x.Data == "1239");
        result.receiptResponse.ftSignatures.Should().Contain(x => (long) x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber) && x.Data == "344");
        result.receiptResponse.ftSignatures.Should().Contain(x => (long) x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment) && x.Data == "2024-10-14 08:55:45");
    }
}
