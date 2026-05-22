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
using V1 = fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.v2;

public class ProtocolCommandProcessorITTests
{
    [Fact]
    public async Task CopyReceiptPrintExistingReceipt0x3010Async_ShouldReturn_EEEE_Tag_IfReceiptReference_IsNotAvailable()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();

        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null)).Returns(new List<ftQueueItem>().ToAsyncEnumerable());

        var itSSCD = Mock.Of<IITSSCD>(MockBehavior.Strict);
        var request = new ProcessCommandRequest(new ftQueue(), new ReceiptRequest
        {
            cbPreviousReceiptReference = cbPreviousReceiptReference,
        }, new ReceiptResponse());

        var processor = new ProtocolCommandProcessorIT(itSSCD, queueItemRepositoryMock.Object);

        var result = await processor.CopyReceiptPrintExistingReceipt0x3010Async(request);

        result.receiptResponse.ftState.IsState(State.Error).Should().BeTrue();
    }

    [Fact]
    public async Task CopyReceiptPrintExistingReceipt0x3010Async_ShouldReturn_ReferenceSignatures_IfLoadedReceipt_ContainsThem()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();
        var signatures = new List<SignatureItem>
        {
            new() { Caption = "<doc-number>", Data = "1239", ftSignatureFormat = SignatureFormat.Text, ftSignatureType = (SignatureType) (Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber) },
            new() { Caption = "<z-number>",   Data = "344",  ftSignatureFormat = SignatureFormat.Text, ftSignatureType = (SignatureType) (Cases.BASE_STATE | (long) SignatureTypesIT.RTZNumber) },
            new() { Caption = "<timestamp>",  Data = "2024-23-01", ftSignatureFormat = SignatureFormat.Text, ftSignatureType = (SignatureType) (Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment) },
        };

        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null)).Returns(new List<ftQueueItem>
        {
            new() { response = JsonSerializer.Serialize(new ReceiptResponse { ftSignatures = signatures }) },
        }.ToAsyncEnumerable());

        var itSSCDMock = new Mock<IITSSCD>(MockBehavior.Strict);
        itSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>())).ReturnsAsync((ProcessRequest r) => new ProcessResponse { ReceiptResponse = r.ReceiptResponse });

        var request = new ProcessCommandRequest(new ftQueue(), new ReceiptRequest
        {
            cbPreviousReceiptReference = cbPreviousReceiptReference,
        }, new ReceiptResponse { ftSignatures = new List<SignatureItem>(signatures) });

        var processor = new ProtocolCommandProcessorIT(itSSCDMock.Object, queueItemRepositoryMock.Object);

        var result = await processor.CopyReceiptPrintExistingReceipt0x3010Async(request);

        result.receiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        result.receiptResponse.ftSignatures.Should().Contain(x => (long) x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber) && x.Data == "1239");
        result.receiptResponse.ftSignatures.Should().Contain(x => (long) x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber) && x.Data == "344");
        result.receiptResponse.ftSignatures.Should().Contain(x => (long) x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment) && x.Data == "2024-23-01");
    }
}
