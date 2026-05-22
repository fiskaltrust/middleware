using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.v2;

public class MiddlewareStorageHelpersTests
{
    private static SignatureItem MakeSignature(string caption, string data, SignatureTypesIT type) => new()
    {
        Caption = caption,
        Data = data,
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = (SignatureType) (Cases.BASE_STATE | (long) type),
    };

    private static List<SignatureItem> StandardSignatures() => new()
    {
        MakeSignature("<doc-number>", "1239", SignatureTypesIT.RTDocumentNumber),
        MakeSignature("<z-number>", "344", SignatureTypesIT.RTZNumber),
        MakeSignature("<timestamp>", "2024-23-01", SignatureTypesIT.RTDocumentMoment),
    };

    private static ReceiptRequest MakeRequest(string cbPrev, string? cbTerminalId = null) => new()
    {
        cbTerminalID = cbTerminalId ?? string.Empty,
        cbPreviousReceiptReference = cbPrev,
        cbReceiptMoment = new DateTime(2024, 1, 1),
    };

    [Fact]
    public async Task LoadReceiptReferencesToResponse_ShouldReturn_EEEE_Tag_IfReceiptReference_IsNotAvailable()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();
        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null)).Returns(new List<ftQueueItem>().ToAsyncEnumerable());

        var request = MakeRequest(cbPreviousReceiptReference);
        var response = new ReceiptResponse();

        var result = await MiddlewareStorageHelpers.LoadReceiptReferencesToResponse(queueItemRepositoryMock.Object, request, request.cbReceiptMoment, response);

        result.ftState.IsState(State.Error).Should().BeTrue();
    }

    [Fact]
    public async Task LoadReceiptReferencesToResponse_ShouldReturn_ReferenceSignatures_IfLoadedReceipt_ContainsThem()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();
        var signatures = StandardSignatures();

        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null)).Returns(new List<ftQueueItem>
        {
            new() { cbTerminalID = string.Empty, response = JsonSerializer.Serialize(new ReceiptResponse { ftSignatures = signatures }) },
        }.ToAsyncEnumerable());

        var request = MakeRequest(cbPreviousReceiptReference);
        var response = new ReceiptResponse { ftSignatures = new List<SignatureItem>(signatures) };

        var result = await MiddlewareStorageHelpers.LoadReceiptReferencesToResponse(queueItemRepositoryMock.Object, request, request.cbReceiptMoment, response);

        result.ftState.IsState(State.Error).Should().BeFalse();
        AssertContainsReference(result, signatures);
    }

    [Fact]
    public async Task LoadReceiptReferencesToResponse_ShouldReturn_ReferenceSignatures_IfLoadedReceipt_ContainsThem_EvenIfTerminalIdIsSet()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();
        var signatures = StandardSignatures();

        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null)).Returns(new List<ftQueueItem>
        {
            new() { cbTerminalID = string.Empty, response = JsonSerializer.Serialize(new ReceiptResponse { ftSignatures = signatures }) },
        }.ToAsyncEnumerable());

        var request = MakeRequest(cbPreviousReceiptReference, "myterminalid");
        var response = new ReceiptResponse { ftSignatures = new List<SignatureItem>(signatures) };

        var result = await MiddlewareStorageHelpers.LoadReceiptReferencesToResponse(queueItemRepositoryMock.Object, request, request.cbReceiptMoment, response);

        result.ftState.IsState(State.Error).Should().BeFalse();
        AssertContainsReference(result, signatures);
    }

    [Fact]
    public async Task LoadReceiptReferencesToResponse_ShouldUseRightReceipt_IfMatchesWithTerminalId()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();
        var otherSignatures = new List<SignatureItem>
        {
            MakeSignature("<doc-number>", "1239", SignatureTypesIT.RTDocumentNumber),
            MakeSignature("<z-number>", "344", SignatureTypesIT.RTZNumber),
            MakeSignature("<timestamp>", "2024-23-01", SignatureTypesIT.RTDocumentMoment),
        };
        var matchedSignatures = new List<SignatureItem>
        {
            MakeSignature("<doc-number>", "11111", SignatureTypesIT.RTDocumentNumber),
            MakeSignature("<z-number>", "434", SignatureTypesIT.RTZNumber),
            MakeSignature("<timestamp>", "2024-01-01", SignatureTypesIT.RTDocumentMoment),
        };

        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null)).Returns(new List<ftQueueItem>
        {
            new() { cbTerminalID = "asdf", response = JsonSerializer.Serialize(new ReceiptResponse { ftSignatures = otherSignatures }) },
            new() { cbTerminalID = "myterminalid", response = JsonSerializer.Serialize(new ReceiptResponse { ftSignatures = matchedSignatures }) },
        }.ToAsyncEnumerable());

        var request = MakeRequest(cbPreviousReceiptReference, "myterminalid");
        var response = new ReceiptResponse { ftSignatures = new List<SignatureItem>(matchedSignatures) };

        var result = await MiddlewareStorageHelpers.LoadReceiptReferencesToResponse(queueItemRepositoryMock.Object, request, request.cbReceiptMoment, response);

        result.ftState.IsState(State.Error).Should().BeFalse();
        AssertContainsReference(result, matchedSignatures);
    }

    [Fact]
    public async Task LoadReceiptReferencesToResponse_ShouldSkip_FailedReceipts()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();
        var failedSignatures = new List<SignatureItem>
        {
            new() { Caption = "FAILURE", Data = "boom", ftSignatureFormat = SignatureFormat.Text, ftSignatureType = (SignatureType) 5283883447184535552 },
        };
        var goodSignatures = new List<SignatureItem>
        {
            MakeSignature("<doc-number>", "11111", SignatureTypesIT.RTDocumentNumber),
            MakeSignature("<z-number>", "434", SignatureTypesIT.RTZNumber),
            MakeSignature("<timestamp>", "2024-01-01", SignatureTypesIT.RTDocumentMoment),
        };

        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null)).Returns(new List<ftQueueItem>
        {
            new() { response = JsonSerializer.Serialize(new ReceiptResponse { ftSignatures = failedSignatures, ftState = (State) (Cases.BASE_STATE | 0xEEEE_EEEE) }) },
            new() { response = JsonSerializer.Serialize(new ReceiptResponse { ftSignatures = goodSignatures }) },
            new() { response = JsonSerializer.Serialize(new ReceiptResponse { ftSignatures = failedSignatures, ftState = (State) (Cases.BASE_STATE | 0xEEEE_EEEE) }) },
        }.ToAsyncEnumerable());

        var request = MakeRequest(cbPreviousReceiptReference);
        var response = new ReceiptResponse { ftSignatures = new List<SignatureItem>(goodSignatures) };

        var result = await MiddlewareStorageHelpers.LoadReceiptReferencesToResponse(queueItemRepositoryMock.Object, request, request.cbReceiptMoment, response);

        result.ftState.IsState(State.Error).Should().BeFalse();
        AssertContainsReference(result, goodSignatures);
    }

    private static void AssertContainsReference(ReceiptResponse response, List<SignatureItem> expected)
    {
        var docNumber = expected.First(s => ((long) s.ftSignatureType & 0xFF) == (long) SignatureTypesIT.RTDocumentNumber).Data;
        var zNumber = expected.First(s => ((long) s.ftSignatureType & 0xFF) == (long) SignatureTypesIT.RTZNumber).Data;
        var moment = expected.First(s => ((long) s.ftSignatureType & 0xFF) == (long) SignatureTypesIT.RTDocumentMoment).Data;

        response.ftSignatures.Should().Contain(x => (long) x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber) && x.Data == docNumber);
        response.ftSignatures.Should().Contain(x => (long) x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber) && x.Data == zNumber);
        response.ftSignatures.Should().Contain(x => (long) x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment) && x.Data == moment);
    }
}
